# MCP 会话恢复机制

## 📅 实现日期
2025-11-21

## 🎯 问题背景

运行一段时间后，MCP 客户端会返回 404 错误：

```log
Server 019aa573-4337-7225-a7b1-df796306dccd: Ping to MCP client 1 failed: 
Response status code does not indicate success: 404 (Not Found).
```

**根本原因**：MCP 服务端会话过期（重启或超时，默认 2 小时），客户端使用旧 sessionId 导致 404。

## ✅ 解决方案

实现自动会话恢复机制，在检测到 404 错误时自动重建连接。

## 🏗️ 架构设计

### 核心组件

```
┌─────────────────────────────────────────────────────────┐
│                   McpSessionService                      │
│                                                          │
│  ┌────────────────────────────────────────────────┐    │
│  │ _mcpClients: List<McpClient>                   │    │
│  │ (存储活跃的 MCP 客户端实例)                      │    │
│  └────────────────────────────────────────────────┘    │
│                          │                              │
│                          ▼                              │
│  ┌────────────────────────────────────────────────┐    │
│  │ _clientIndexToServiceConfig                    │    │
│  │ Dictionary<int, McpServiceEndpoint>            │    │
│  │ (映射客户端索引到服务配置，用于恢复)              │    │
│  └────────────────────────────────────────────────┘    │
│                          │                              │
│                          ▼                              │
│  ┌────────────────────────────────────────────────┐    │
│  │ HandlePingAsync                                │    │
│  │ • 检测 404 错误                                 │    │
│  │ • 标记需要恢复的客户端                          │    │
│  │ • 触发后台恢复                                  │    │
│  └────────────────────────────────────────────────┘    │
│                          │                              │
│                          ▼                              │
│  ┌────────────────────────────────────────────────┐    │
│  │ RecoverMcpClientAsync                          │    │
│  │ • 释放旧客户端                                  │    │
│  │ • 创建新客户端                                  │    │
│  │ • 应用认证配置                                  │    │
│  │ • 替换客户端实例                                │    │
│  └────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

## 💻 代码实现

### 1. 会话状态跟踪

```csharp
// 为每个客户端记录服务配置，以便恢复
private readonly Dictionary<int, McpServiceEndpoint> _clientIndexToServiceConfig = new();
```

**作用**：建立客户端索引到服务配置的映射，恢复时使用相同配置重建连接。

### 2. 连接时记录配置

```csharp
// 在 ConnectAsync 中
var clientIndex = _mcpClients.Count;
_mcpClients.Add(mcpClient);
_clientIndexToServiceConfig[clientIndex] = service; // 🔧 Track for session recovery
```

**作用**：每次创建客户端时保存配置，确保可以重建。

### 3. Ping 时检测 404

```csharp
// 在 HandlePingAsync 中
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
{
    // 🔧 404 indicates session expired - needs recovery
    _logger.LogWarning(
        "Server {ServerId}: MCP client {ClientIndex} session expired (404), will attempt recovery",
        ServerId, index);
    return (success: false, index, duration: 0.0, needsRecovery: true, error: (Exception?)ex);
}
```

**作用**：专门捕获 404 错误，标记为需要恢复。

### 4. 后台触发恢复

```csharp
// 检查需要恢复的客户端
var clientsNeedingRecovery = results
    .Where(r => r.needsRecovery)
    .Select(r => r.index)
    .ToList();

if (clientsNeedingRecovery.Any())
{
    _logger.LogWarning(
        "Server {ServerId}: {Count} client(s) need session recovery, attempting to reconnect...",
        ServerId, clientsNeedingRecovery.Count);
    
    // 🔧 Trigger session recovery in background (don't block ping response)
    _ = Task.Run(async () =>
    {
        foreach (var clientIndex in clientsNeedingRecovery)
        {
            try
            {
                await RecoverMcpClientAsync(clientIndex, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Server {ServerId}: Failed to recover MCP client {ClientIndex}",
                    ServerId, clientIndex);
            }
        }
    }, cancellationToken);
}
```

**作用**：
- 后台异步恢复，不阻塞 ping 响应
- 批量处理多个失效客户端
- 独立错误处理，一个失败不影响其他

### 5. 恢复方法实现

```csharp
private async Task RecoverMcpClientAsync(int clientIndex, CancellationToken cancellationToken)
{
    if (!_clientIndexToServiceConfig.TryGetValue(clientIndex, out var service))
    {
        _logger.LogWarning(
            "Server {ServerId}: Cannot recover client {ClientIndex} - service config not found",
            ServerId, clientIndex);
        return;
    }

    _logger.LogInformation(
        "Server {ServerId}: Recovering MCP client {ClientIndex} for service {ServiceName}",
        ServerId, clientIndex, service.ServiceName);

    try
    {
        // 1. 释放旧客户端
        var oldClient = _mcpClients[clientIndex];
        try
        {
            await oldClient.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Server {ServerId}: Error disposing old client {ClientIndex}",
                ServerId, clientIndex);
        }

        // 2. 创建新客户端（与初始连接相同的配置）
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(service.NodeAddress),
            Name = $"McpService_{service.ServiceName}",
            OmitContentTypeCharset = true
        };

        // 设置协议
        if (service.Protocol == "sse")
        {
            transportOptions.TransportMode = HttpTransportMode.Sse;
        }
        else if (service.Protocol == "streamable-http" || service.Protocol == "http")
        {
            transportOptions.TransportMode = HttpTransportMode.StreamableHttp;
        }

        // 3. 应用认证配置
        if (McpAuthenticationHelper.IsAuthenticationConfigured(
            service.AuthenticationType,
            service.AuthenticationConfig))
        {
            var authType = service.AuthenticationType!.ToLowerInvariant();

            if (authType == "oauth2")
            {
                transportOptions.OAuth = McpAuthenticationHelper.BuildOAuth2Options(
                    service.AuthenticationConfig!,
                    _logger);
            }
            else
            {
                var authHeaders = McpAuthenticationHelper.BuildAuthenticationHeaders(
                    service.AuthenticationType!,
                    service.AuthenticationConfig!,
                    _logger);

                if (transportOptions.AdditionalHeaders != null)
                {
                    foreach (var header in authHeaders)
                    {
                        transportOptions.AdditionalHeaders[header.Key] = header.Value;
                    }
                }
            }
        }

        // 4. 创建 HttpClient
        var httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var transport = new HttpClientTransport(transportOptions, httpClient, ownsHttpClient: true);
        var newClient = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);

        // 5. 替换旧客户端
        _mcpClients[clientIndex] = newClient;

        _logger.LogInformation(
            "Server {ServerId}: Successfully recovered MCP client {ClientIndex} for service {ServiceName}",
            ServerId, clientIndex, service.ServiceName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Server {ServerId}: Failed to recover MCP client {ClientIndex} for service {ServiceName}",
            ServerId, clientIndex, service.ServiceName);
        throw;
    }
}
```

**步骤**：
1. 验证服务配置存在
2. 安全释放旧客户端（即使失败也继续）
3. 使用相同配置创建新客户端
4. 应用认证（OAuth2、Bearer、Basic、ApiKey）
5. 替换列表中的客户端实例

## 🔄 工作流程

### 正常流程

```
1. 小智发送 ping
   ↓
2. 转发到所有 MCP 客户端
   ↓
3. 所有客户端响应成功
   ↓
4. 返回 ping 响应给小智
```

### 会话过期流程

```
1. 小智发送 ping
   ↓
2. 转发到所有 MCP 客户端
   ↓
3. 某个客户端返回 404
   ↓
4. 标记为 needsRecovery
   ↓
5. 立即返回 ping 响应给小智（部分成功）
   ↓
6. 后台异步恢复：
   ├─ 释放旧客户端
   ├─ 创建新客户端
   ├─ 应用认证配置
   └─ 替换客户端实例
   ↓
7. 下次 ping 自动使用新客户端
   ↓
8. 恢复正常
```

## 📊 预期日志

### 检测到会话过期

```log
dbug: Server 019aa573-4337-7225-a7b1-df796306dccd: Received ping request (id: 8) from Xiaozhi, forwarding to 2 MCP client(s)
warn: Server 019aa573-4337-7225-a7b1-df796306dccd: MCP client 0 session expired (404), will attempt recovery
warn: Server 019aa573-4337-7225-a7b1-df796306dccd: MCP client 1 session expired (404), will attempt recovery
warn: Server 019aa573-4337-7225-a7b1-df796306dccd: Ping completed - 0/2 clients responded successfully, total time: 925ms
warn: Server 019aa573-4337-7225-a7b1-df796306dccd: 2 client(s) need session recovery, attempting to reconnect...
```

### 恢复中

```log
info: Server 019aa573-4337-7225-a7b1-df796306dccd: Recovering MCP client 0 for service calculator
info: Server 019aa573-4337-7225-a7b1-df796306dccd: Recovering MCP client 1 for service weather
```

### 恢复成功

```log
info: Server 019aa573-4337-7225-a7b1-df796306dccd: Successfully recovered MCP client 0 for service calculator
info: Server 019aa573-4337-7225-a7b1-df796306dccd: Successfully recovered MCP client 1 for service weather
```

### 下次 Ping 正常

```log
dbug: Server 019aa573-4337-7225-a7b1-df796306dccd: Received ping request (id: 9) from Xiaozhi, forwarding to 2 MCP client(s)
info: Server 019aa573-4337-7225-a7b1-df796306dccd: Ping completed - 2/2 clients responded successfully, total time: 135ms, avg response: 67.50ms
```

## ✅ 优势

### 1. **自动化**
- 无需人工干预
- 后台自动检测和恢复
- 对小智 ping 响应无影响

### 2. **弹性**
- 单个客户端失败不影响其他
- 恢复失败会记录错误但不会崩溃
- 下次 ping 会再次尝试

### 3. **透明**
- 详细的日志记录
- 明确的状态转换
- 易于调试和监控

### 4. **高效**
- 后台异步恢复，不阻塞响应
- 批量处理多个失效客户端
- 只恢复需要恢复的客户端

## 🔍 故障排查

### 如果恢复持续失败

检查日志中的错误信息：

```log
# 配置问题
error: Server xxx: Failed to recover MCP client 0 for service calculator: 
Connection refused

# 认证问题
error: Server xxx: Failed to recover MCP client 0 for service calculator: 
401 Unauthorized

# 端点问题
error: Server xxx: Failed to recover MCP client 0 for service calculator: 
Name or service not known
```

### 常见问题

| 错误 | 可能原因 | 解决方案 |
|------|---------|---------|
| Connection refused | MCP 服务未运行 | 检查服务状态，确认端口正确 |
| 404 反复出现 | 恢复失败或服务频繁重启 | 查看恢复日志，检查服务稳定性 |
| 401/403 | 认证配置错误 | 验证 AuthenticationConfig 配置 |
| Timeout | 网络问题或服务响应慢 | 检查网络连接，服务性能 |

## 🚀 测试建议

### 1. 模拟会话过期

```bash
# 重启某个 MCP 服务
docker restart <mcp-service-container>

# 观察日志
docker-compose -f docker/docker-compose.single-image.yml logs -f api | grep "session expired"
docker-compose -f docker/docker-compose.single-image.yml logs -f api | grep "Successfully recovered"
```

### 2. 验证恢复效果

```bash
# 第一次 ping（应该看到 404 和恢复）
# 等待几秒让恢复完成
# 第二次 ping（应该全部成功）
docker-compose -f docker/docker-compose.single-image.yml logs -f api | grep "Ping completed"
```

### 3. 压力测试

```bash
# 在多个 MCP 服务同时重启的情况下测试
docker restart mcp-service-1 mcp-service-2 mcp-service-3

# 观察是否能批量恢复
docker-compose -f docker/docker-compose.single-image.yml logs -f api | grep "client(s) need session recovery"
```

## 📈 性能考虑

### 恢复时间

- **检测时间**：ping 周期内（~1秒）
- **恢复启动**：立即（后台异步）
- **恢复完成**：取决于 MCP 服务响应时间（通常 < 5秒）
- **总影响**：对小智 ping 响应无影响

### 资源使用

- **内存**：每个客户端约增加几 KB（服务配置映射）
- **CPU**：恢复时短暂增加（创建新连接）
- **网络**：额外的连接建立请求

### 并发控制

当前实现是串行恢复每个客户端，如果需要加速可以改为并行：

```csharp
// 并行恢复（可选优化）
var recoveryTasks = clientsNeedingRecovery.Select(clientIndex =>
    RecoverMcpClientAsync(clientIndex, cancellationToken)
);
await Task.WhenAll(recoveryTasks);
```

## 🎯 未来增强

### 1. 重试限制

添加恢复重试次数限制，避免无限重试：

```csharp
private readonly Dictionary<int, int> _recoveryAttempts = new();
private const int MaxRecoveryAttempts = 3;
```

### 2. 指数退避

为重复失败的恢复添加退避延迟：

```csharp
var backoffMs = Math.Min(1000 * Math.Pow(2, attemptCount), 30000);
await Task.Delay(backoffMs, cancellationToken);
```

### 3. 健康检查

主动检测会话健康，而不是被动等待 404：

```csharp
// 定期发送健康检查 ping
if (DateTime.UtcNow - lastHealthCheck > TimeSpan.FromMinutes(5))
{
    await HealthCheckAsync();
}
```

### 4. 指标收集

记录恢复成功率、恢复时间等指标：

```csharp
_metrics.RecordSessionRecovery(clientIndex, duration, success);
```
