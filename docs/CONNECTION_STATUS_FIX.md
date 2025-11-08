# 连接状态判断逻辑修复

## 🎯 修复目标

解决小智连接启用且至少有一个 MCP 服务绑定启用时，连接状态仍然显示为"未连接"的问题。

## 🔍 问题根源

### 1. 时序问题
**之前的流程**：
```
创建会话 → 更新数据库为"已连接" → 更新 Redis 为"Connected" → 启动实际连接
```

**问题**：
- 数据库和 Redis 状态在实际连接建立**之前**就被标记为已连接
- 如果后续连接失败（MCP 服务不可达、认证失败等），状态不会回滚
- 导致数据库显示"已连接"，但实际上 `session.IsConnected == false`

### 2. IsConnected 判断逻辑

```csharp
public bool IsConnected => _webSocket?.State == WebSocketState.Open && _mcpClients.Count > 0;
```

**要求**：
- WebSocket 必须处于 Open 状态
- **必须至少有一个 MCP 客户端成功连接**

如果所有 MCP 服务连接失败，即使 WebSocket 连接成功，`IsConnected` 也为 `false`。

### 3. 心跳更新问题

`ConnectionMonitorHostedService` 只为 `IsConnected == true` 的会话更新心跳：

```csharp
foreach (var session in localSessions)
{
    if (session.Value.IsConnected)  // ← 如果为 false，不更新心跳
    {
        await connectionStateService.UpdateHeartbeatAsync(session.Key, cancellationToken);
    }
}
```

**结果**：
- 心跳不更新 → Redis 中的连接状态过期 → 被认为是过期连接 → 被清理

## ✅ 解决方案

### 1. 添加连接事件回调机制

在 `McpSessionService` 中添加事件：

```csharp
// Connection status events
public event Func<Task>? OnConnected;
public event Func<string, Task>? OnConnectionFailed;

// 新增属性用于详细状态
public int ConnectedClientsCount => _mcpClients.Count;
public int TotalConfiguredClients => _config.McpServices.Count;
```

### 2. 在实际连接建立后触发回调

在 `ConnectAsync()` 方法中，成功创建 MCP 客户端后：

```csharp
// Notify connection success if at least one MCP client connected
if (_mcpClients.Count > 0)
{
    _logger.LogInformation(
        "Server {ServerId}: Connection established with {ConnectedCount}/{TotalCount} MCP services",
        ServerId, _mcpClients.Count, _config.McpServices.Count);
    
    await (OnConnected?.Invoke() ?? Task.CompletedTask);
}
else
{
    var errorMsg = $"No MCP clients connected (0/{_config.McpServices.Count})";
    await (OnConnectionFailed?.Invoke(errorMsg) ?? Task.CompletedTask);
    throw new InvalidOperationException(errorMsg);
}
```

### 3. 在 McpSessionManager 中订阅事件

**OnConnected 回调**：

```csharp
session.OnConnected += async () =>
{
    // 更新数据库状态为已连接
    var serverToUpdate = await repository.GetAsync(serverId);
    if (serverToUpdate != null)
    {
        serverToUpdate.SetConnected();
        await repository.UnitOfWork.SaveEntitiesAsync();
    }

    // 更新 Redis 状态为 Connected
    await _connectionStateService.UpdateConnectionStatusAsync(
        serverId, ConnectionStatus.Connected, CancellationToken.None);

    // 重置重连尝试次数
    await _connectionStateService.ResetReconnectAttemptsAsync(
        serverId, CancellationToken.None);
};
```

**OnConnectionFailed 回调**：

```csharp
session.OnConnectionFailed += async (errorMessage) =>
{
    // 更新数据库状态为未连接
    var serverToUpdate = await repository.GetAsync(serverId);
    if (serverToUpdate != null)
    {
        serverToUpdate.SetDisconnected();
        await repository.UnitOfWork.SaveEntitiesAsync();
    }

    // 更新 Redis 状态为 Failed
    await _connectionStateService.UpdateConnectionStatusAsync(
        serverId, ConnectionStatus.Failed, CancellationToken.None);
};
```

### 4. 修改启动流程

**新的流程**：
```
创建会话 → 订阅事件 → 注册到 Redis (Connecting) → 启动连接 
    → 成功: OnConnected 回调更新状态
    → 失败: OnConnectionFailed 回调更新状态
```

在 `StartSessionAsync` 中：

```csharp
// Start session in background
_ = Task.Run(async () =>
{
    try
    {
        // 只标记为 Connecting，不标记为 Connected
        await _connectionStateService.UpdateConnectionStatusAsync(
            serverId, ConnectionStatus.Connecting, CancellationToken.None);
        
        // 启动会话 - 状态将通过回调更新
        await session.StartAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        // 处理异常...
    }
});
```

## 📊 修复效果对比

### 场景 1: 所有 MCP 服务连接成功

**之前**：
```
1. 数据库：已连接 ✅
2. Redis：Connected ✅
3. session.IsConnected：true ✅
结果：一切正常
```

**现在**：
```
1. 启动时标记为 Connecting
2. MCP 客户端连接成功
3. 触发 OnConnected 回调
4. 数据库：已连接 ✅
5. Redis：Connected ✅
6. session.IsConnected：true ✅
结果：一切正常，状态准确
```

### 场景 2: 部分 MCP 服务连接成功（至少一个）

**之前**：
```
1. 数据库：已连接 ✅
2. Redis：Connected ✅
3. session.IsConnected：true ✅
结果：正常，但不知道有部分失败
```

**现在**：
```
1. 启动时标记为 Connecting
2. 部分 MCP 客户端连接成功 (例如 2/3)
3. 触发 OnConnected 回调
4. 数据库：已连接 ✅
5. Redis：Connected ✅
6. session.IsConnected：true ✅
7. 日志：明确记录 "Connection established with 2/3 MCP services"
结果：正常，且有详细状态
```

### 场景 3: 所有 MCP 服务连接失败

**之前**：
```
1. 数据库：已连接 ✅ (错误！)
2. Redis：Connected ✅ (错误！)
3. session.IsConnected：false ❌
4. 心跳不更新 → 连接被认为过期 → 混乱
结果：状态不一致
```

**现在**：
```
1. 启动时标记为 Connecting
2. 所有 MCP 客户端连接失败 (0/3)
3. 触发 OnConnectionFailed 回调
4. 数据库：未连接 ✅
5. Redis：Failed ✅
6. session.IsConnected：false ✅
7. 日志：明确记录错误原因
结果：状态一致，错误明确
```

### 场景 4: WebSocket 连接失败

**之前**：
```
1. 数据库：已连接 ✅ (错误！)
2. 异常被捕获
3. 清理会话
结果：数据库状态不准确
```

**现在**：
```
1. 启动时标记为 Connecting
2. WebSocket 连接失败，抛出异常
3. catch 块更新状态为 Failed
4. 数据库：未连接 ✅
5. Redis：Failed ✅
结果：状态准确
```

## 🎁 额外改进

### 1. 详细的连接统计

新增属性：
```csharp
public int ConnectedClientsCount => _mcpClients.Count;
public int TotalConfiguredClients => _config.McpServices.Count;
```

可用于：
- 日志记录："Connection established with 2/3 MCP services"
- 监控指标
- 前端显示详细状态

### 2. 更好的日志记录

**连接成功**：
```
Server xxx: Connection established with 2/3 MCP services
```

**连接失败**：
```
Server xxx: No MCP clients connected (0/3)
```

**部分失败**：
```
Failed to connect to MCP service ServiceA at http://...
Server xxx: Connection established with 2/3 MCP services
```

### 3. 保留原有空绑定检查

在 `McpSessionManager` 中的预防性检查（之前添加的）：

```csharp
if (mcpServiceEndpoints.Count == 0)
{
    _logger.LogWarning(
        "Server {ServerId} ({ServerName}) has no active service bindings. Cannot create session.",
        serverId, server.Name);
    return false;
}
```

这个检查仍然有效，作为第一道防线。

## 🚀 使用建议

### 1. 监控连接质量

建议在前端显示：
- "已连接 (2/3 服务)"
- "部分连接"
- "连接失败"

### 2. 日志分析

关注以下日志模式：
```
Connection established with X/Y MCP services
```

如果 `X < Y`，说明有 MCP 服务连接失败，需要检查：
- 服务端点是否可达
- 认证配置是否正确
- 网络是否正常

### 3. 重连策略

现在的重连逻辑会：
- 对于完全失败的连接（0/N），继续重试
- 对于部分成功的连接（X/N, X>0），标记为成功但记录警告

## 📝 测试场景

建议测试以下场景：

1. ✅ **正常连接**：所有 MCP 服务可用
2. ✅ **部分失败**：部分 MCP 服务不可用
3. ✅ **完全失败**：所有 MCP 服务不可用
4. ✅ **WebSocket 失败**：小智端点不可达
5. ✅ **认证失败**：MCP 服务认证配置错误
6. ✅ **延迟连接**：MCP 服务响应缓慢
7. ✅ **动态禁用**：连接建立后禁用绑定

## 🎯 总结

**核心改进**：
- ✅ 状态更新时机从"启动前"改为"连接后"
- ✅ 通过事件回调实现异步状态同步
- ✅ 数据库、Redis 和内存状态保持一致
- ✅ 详细的连接统计和日志
- ✅ 更好的错误处理和报告

**符合期望的行为**：
- 小智连接启用 ✅
- 至少一个 MCP 服务绑定启用且连接成功 ✅
- 连接状态显示为"已连接" ✅
- 工具可以正常加载和使用 ✅
- 日志清晰记录连接详情 ✅

现在的实现确保了状态的准确性和一致性，同时提供了更详细的连接信息！
