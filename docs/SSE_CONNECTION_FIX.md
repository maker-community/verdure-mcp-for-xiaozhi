# SSE 连接管理修复总结

## 📅 修复日期
2025-11-21

## 🎯 问题背景

程序运行一段时间后出现 404 错误，最初怀疑是连接池超时导致 SSE 连接被关闭。

## 🔍 深入调查

### SDK 源码分析

通过分析 [modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk) 源码发现：

1. **SDK 不配置连接池参数**
   - `SseClientSessionTransport` 和 `HttpClientTransport` 都没有设置 `PooledConnectionLifetime` 或 `PooledConnectionIdleTimeout`
   - SDK 依赖 HttpClient 的默认连接池行为

2. **SSE 连接生命周期**
   ```csharp
   // SDK 源码 - SseClientSessionTransport.cs
   using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
   await foreach (SseItem<string> sseEvent in SseParser.Create(stream).EnumerateAsync(cancellationToken))
   {
       // 持续读取 SSE 事件流
   }
   ```
   - SSE 使用长期 GET 请求建立流连接
   - **连接由 stream 持有，不会归还到连接池**
   - 只要 `ReceiveMessagesAsync` 运行，连接就保持活跃
   - 连接通过 `CancellationToken` 或 `Dispose` 关闭

3. **会话管理**
   - 服务端有 IdleTimeout（默认 2 小时）管理会话
   - 会话过期后，使用旧 sessionId 会收到 404

### 错误诊断

❌ **之前的错误理解**：
```csharp
// 为 SSE 设置 Timeout.InfiniteTimeSpan
PooledConnectionLifetime = service.Protocol == "sse" 
    ? Timeout.InfiniteTimeSpan 
    : TimeSpan.FromMinutes(5)
```

**为什么错误**：
- SSE 连接根本不受连接池超时影响（stream 持续持有连接）
- `PooledConnectionIdleTimeout` 对 SSE 无效（连接一直在使用，永不 idle）
- 这是**过度优化**，试图解决一个不存在的问题

✅ **正确理解**：
- 404 错误来自**服务端会话过期**，而非连接池超时
- SDK 设计就是依赖默认 HttpClient 行为
- 不应该干预连接池配置

## 🛠️ 修复方案

### 1. 移除自定义连接池配置

**文件**: `src/Verdure.McpPlatform.Api/Services/WebSocket/McpSessionService.cs`

**修改前**:
```csharp
var httpClient = new HttpClient(new SocketsHttpHandler
{
    PooledConnectionLifetime = service.Protocol == "sse" 
        ? Timeout.InfiniteTimeSpan 
        : TimeSpan.FromMinutes(5),
    PooledConnectionIdleTimeout = service.Protocol == "sse"
        ? Timeout.InfiniteTimeSpan 
        : TimeSpan.FromSeconds(90)
})
{
    Timeout = TimeSpan.FromSeconds(10)
};
```

**修改后**:
```csharp
// 🔧 Create HttpClient with minimal configuration
// SDK manages SSE connection lifetime via stream, not connection pooling
// We only set request timeout for fast failure on unresponsive tools
var httpClient = new HttpClient()
{
    // Individual request timeout (not connection lifetime)
    // Set to 10 seconds for fast failure on unavailable tools
    // This applies to tool calls, not the SSE stream itself
    Timeout = TimeSpan.FromSeconds(10)
};
```

**原因**:
- 使用 SDK 默认方式，不干预连接池
- 只设置 `Timeout` 用于工具调用的快速失败
- 依赖 SDK 的 stream 管理机制

### 2. 增强 HTTP 错误日志

**修改前**:
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to connect to MCP service {ServiceName} at {NodeAddress}", 
        service.ServiceName, service.NodeAddress);
}
```

**修改后**:
```csharp
catch (HttpRequestException ex)
{
    _logger.LogError(ex, 
        "HTTP request failed to MCP service {ServiceName} at {NodeAddress}: StatusCode={StatusCode}, Protocol={Protocol}",
        service.ServiceName, service.NodeAddress, ex.StatusCode, service.Protocol);
}
catch (Exception ex)
{
    _logger.LogError(ex, 
        "Failed to connect to MCP service {ServiceName} at {NodeAddress}, Protocol={Protocol}", 
        service.ServiceName, service.NodeAddress, service.Protocol);
}
```

**原因**:
- 明确捕获 `HttpRequestException` 以获取 HTTP 状态码
- 记录协议类型以便诊断协议相关问题
- 帮助识别 404（会话过期）vs 其他 HTTP 错误

### 3. 更新文档

更新 `docs/TIMEOUT_OPTIMIZATION.md`：
- 添加 SDK 源码分析结论
- 解释 SSE 连接的真实生命周期机制
- 说明 404 错误的可能原因
- 提供未来改进方向（会话恢复机制）

## 📊 预期效果

### 立即效果
1. ✅ 代码更简洁，符合 SDK 设计意图
2. ✅ 更详细的错误日志，便于诊断 404 等问题
3. ✅ 移除了无效的"优化"

### 404 错误诊断
当遇到 404 时，日志会显示：
```log
HTTP request failed to MCP service calculator at http://service:8080/sse: 
StatusCode=404, Protocol=sse
```

可能原因：
1. **MCP 服务端会话过期**（最可能）
   - 服务端重启
   - 会话超时（默认 2 小时）
   - 需要实现会话恢复机制

2. **端点地址错误**
   - 检查配置的 endpoint 地址

3. **协议不匹配**
   - 检查 protocol 配置

## 🚀 未来改进

### ✅ 已实现：会话恢复机制 (2025-11-21)

基于 SDK 源码分析，已经实现了自动会话恢复机制：

#### 实现细节

1. **会话状态跟踪**
   ```csharp
   // 为每个客户端记录服务配置，以便恢复
   private readonly Dictionary<int, McpServiceEndpoint> _clientIndexToServiceConfig = new();
   ```

2. **404 错误检测**
   ```csharp
   catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
   {
       // 404 表示会话过期 - 需要恢复
       _logger.LogWarning(
           "Server {ServerId}: MCP client {ClientIndex} session expired (404), will attempt recovery",
           ServerId, index);
       return (success: false, index, duration: 0.0, needsRecovery: true, error: (Exception?)ex);
   }
   ```

3. **自动恢复流程**
   ```csharp
   // 在后台异步恢复（不阻塞 ping 响应）
   _ = Task.Run(async () =>
   {
       foreach (var clientIndex in clientsNeedingRecovery)
       {
           await RecoverMcpClientAsync(clientIndex, cancellationToken);
       }
   }, cancellationToken);
   ```

4. **恢复方法实现**
   - 释放旧客户端连接
   - 使用相同配置创建新客户端
   - 应用认证配置（OAuth2/Bearer/Basic/ApiKey）
   - 替换失效的客户端实例

#### 工作原理

1. **检测阶段**：每次 ping 时检测 404 错误
2. **标记阶段**：标记需要恢复的客户端索引
3. **恢复阶段**：后台异步重建连接（不影响 ping 响应）
4. **完成阶段**：新客户端替换旧客户端，下次 ping 自动使用新连接

#### 预期行为

```log
# 检测到会话过期
Server xxx: MCP client 0 session expired (404), will attempt recovery
Server xxx: 1 client(s) need session recovery, attempting to reconnect...

# 恢复中
Server xxx: Recovering MCP client 0 for service calculator

# 恢复成功
Server xxx: Successfully recovered MCP client 0 for service calculator

# 下次 ping 自动使用新会话
Server xxx: Ping to MCP client 0 succeeded in 125ms
```

### 🔜 未来增强

#### 2. 连接健康检查
```csharp
// 定期检测 SSE stream 是否仍然活跃
if (!stream.CanRead)
{
    _logger.LogWarning("SSE stream disconnected, reconnecting...");
    await ReconnectAsync();
}
```

#### 3. 更智能的重连策略
- 区分短暂故障（网络抖动）vs 持久故障（服务下线）
- 为不同故障类型使用不同的退避策略
- 记录重连次数和成功率
- 添加重连次数限制，避免无限重试

## 📝 测试建议

### 测试步骤
1. 重新构建并部署
   ```bash
   docker-compose -f docker/docker-compose.single-image.yml build
   docker-compose -f docker/docker-compose.single-image.yml up -d
   ```

2. 观察启动日志
   - 确认 HttpClient 配置简化
   - 检查是否有初始连接错误

3. 长时间运行测试
   - 运行 2-3 小时以上
   - 观察是否出现 404 错误
   - 检查错误日志的详细信息

4. 模拟故障
   - 重启某个 MCP 服务
   - 观察是否出现 404 及错误日志
   - 验证是否需要实现会话恢复

### 日志检查点
```bash
# 查看启动时的连接日志
docker-compose -f docker/docker-compose.single-image.yml logs api | grep "MCP client connected"

# 查看 HTTP 错误
docker-compose -f docker/docker-compose.single-image.yml logs api | grep "HTTP request failed"

# 查看 ping 处理情况
docker-compose -f docker/docker-compose.single-image.yml logs api | grep "Ping completed"
```

## ✅ 验收标准

- [x] 代码已移除 `PooledConnectionLifetime` 和 `PooledConnectionIdleTimeout` 配置
- [x] 添加了 `HttpRequestException` 专门捕获和详细日志
- [x] 文档已更新，反映正确的理解
- [ ] 构建成功
- [ ] 部署后观察 2-3 小时无异常
- [ ] 如果出现 404，日志提供足够诊断信息

## 📚 参考资料

- [MCP C# SDK 源码](https://github.com/modelcontextprotocol/csharp-sdk)
- [SseClientSessionTransport.cs](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Client/SseClientSessionTransport.cs)
- [HttpClientTransport.cs](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/src/ModelContextProtocol.Core/Client/HttpClientTransport.cs)
- `docs/TIMEOUT_OPTIMIZATION.md` - Timeout 优化详细说明
