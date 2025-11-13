# WebSocket 连接状态检测问题分析

## 🔍 问题描述

用户反馈：连接看似正常（小智持续发送 ping 指令，服务正常响应），但怀疑服务端可能会主动断开连接，而这个状态没有被正确记录和检测到。

## 📊 代码分析结果

### ✅ 已实现的断开检测机制

#### 1. **WebSocket 被动关闭检测**（`McpSessionService.PipeWebSocketToMcpAsync`）

```csharp
// 在 PipeWebSocketToMcpAsync 中
if (result.MessageType == WebSocketMessageType.Close)
{
    _logger.LogInformation("Server {ServerId}: WebSocket closed by server", ServerId);
    break;
}
```

**机制**：当小智服务器主动关闭 WebSocket 时，`ReceiveAsync` 会返回 `WebSocketMessageType.Close` 消息类型。

**状态更新**：
- 会触发 `PipeWebSocketToMcpAsync` 任务退出
- 进入 `CleanupConnectionAsync()`
- 但是 **没有触发数据库和 Redis 状态更新**

#### 2. **WebSocket 异常断开检测**

```csharp
// 在 ConnectAsync 的 finally 块中
finally
{
    await CleanupConnectionAsync();
}
```

**机制**：任何异常（网络错误、超时等）都会被捕获并清理连接。

**问题**：同样 **没有在 finally 中更新状态**

#### 3. **后台监控服务检测**（`ConnectionMonitorHostedService`）

```csharp
// 每 30 秒检查一次心跳超时
var staleConnections = await connectionStateService.GetStaleConnectionsAsync(
    _heartbeatTimeout,  // 90秒超时
    cancellationToken);
```

**机制**：
- 每 30 秒运行一次
- 检测 Redis 中 90 秒未更新心跳的连接
- 清理过期状态并尝试重连

**问题**：
- 依赖心跳更新，如果连接已断开但心跳未更新，要等 90 秒才能检测到
- 仅依靠服务端主动更新心跳，不主动检测 WebSocket 状态

## 🚨 发现的关键问题

### 问题 1：**WebSocket 正常关闭时状态未更新** ⚠️ 严重

**位置**：`McpSessionService.ConnectAsync` 方法

**现象**：
```csharp
// 当 WebSocket 正常关闭时
if (result.MessageType == WebSocketMessageType.Close)
{
    _logger.LogInformation("Server {ServerId}: WebSocket closed by server", ServerId);
    break;  // ❌ 仅退出循环，没有更新状态
}
```

**后果**：
1. 本地 `_webSocket` 状态变为关闭
2. 但 `IsConnected` 属性可能还返回 true（因为检查的是 `_webSocket?.State == WebSocketState.Open`）
3. **数据库中的 `IsConnected` 字段未更新为 false**
4. **Redis 中的状态未更新为 `Disconnected`**
5. 后台监控服务需要等 90 秒心跳超时才能检测到

**影响**：
- UI 上显示连接正常
- Redis 状态显示 Connected
- 实际 WebSocket 已断开
- 小智的 ping 请求会失败（因为 `_webSocket?.State != WebSocketState.Open`）

### 问题 2：**心跳更新只在连接正常时执行** ⚠️ 中等

**位置**：`ConnectionMonitorHostedService.UpdateLocalConnectionHeartbeatsAsync`

```csharp
foreach (var session in localSessions)
{
    if (session.Value.IsConnected)  // ❌ 只有 IsConnected 为 true 才更新
    {
        await connectionStateService.UpdateHeartbeatAsync(
            session.Key,
            cancellationToken);
    }
}
```

**问题**：
- 如果 `IsConnected` 返回 false（WebSocket 断开），就不会更新心跳
- 但是 session 对象还在 `_sessions` 字典中
- Redis 状态可能还是 Connected（因为没有主动更新）

### 问题 3：**没有主动检测 WebSocket 状态** ⚠️ 严重

**现状**：
- 系统完全依赖 **被动接收** WebSocket 消息来检测断开
- 没有主动轮询 `_webSocket.State` 来检测状态变化
- 如果网络静默断开（没有 Close 消息），只能等 ping 失败或心跳超时

### 问题 4：**Ping 处理没有检测 WebSocket 状态** ⚠️ 高

**位置**：`McpSessionService.HandlePingAsync`

```csharp
private async Task HandlePingAsync(int? id, CancellationToken cancellationToken)
{
    // ✅ 向 MCP 客户端发送 ping
    var pingTasks = _mcpClients.Select(async (mcpClient, index) => { ... });
    
    // ✅ 响应小智
    await SendWebSocketResponseAsync(response, cancellationToken);
    
    // ❌ 但没有检查 WebSocket 本身的健康状态
}
```

**问题**：
- Ping 只检测了 MCP 服务的健康状态
- 没有检测到小智的 WebSocket 连接状态
- 即使 WebSocket 已断开，`SendWebSocketResponseAsync` 会悄然失败

## 💡 根本原因分析

### 架构设计问题

1. **状态更新不一致**：
   - 连接成功时：✅ 更新数据库 + Redis（通过 OnConnected 回调）
   - 连接失败时：✅ 更新数据库 + Redis（通过 OnConnectionFailed 回调）
   - **正常断开时：❌ 没有更新任何状态**
   - **异常断开时：❌ 没有更新任何状态**

2. **依赖单一检测机制**：
   - 仅依赖后台监控的心跳超时（90秒）
   - 没有在 WebSocket 读取循环中主动检测状态
   - 没有在 Ping 处理中检测 WebSocket 健康状态

3. **IsConnected 属性不可靠**：
   ```csharp
   public bool IsConnected => _webSocket?.State == WebSocketState.Open && _mcpClients.Count > 0;
   ```
   - 这个属性是实时计算的，反映的是当前 WebSocket 状态
   - 但后台监控服务每 30 秒才检查一次
   - 状态变化和检测之间有时间差

## 🔧 建议的修复方案

### 方案 1：在 ConnectAsync 方法中添加状态更新（推荐）

**修改点**：`McpSessionService.ConnectAsync` 的 finally 块

```csharp
finally
{
    // ✅ 添加断开时的状态更新
    LastDisconnectedTime = DateTime.UtcNow;
    
    // 通知断开（如果之前是连接状态）
    if (IsConnected)
    {
        _logger.LogWarning("Server {ServerId} disconnected", ServerId);
        // 这里可以添加一个 OnDisconnected 回调
    }
    
    await CleanupConnectionAsync();
}
```

### 方案 2：在 McpSessionManager 中添加 OnDisconnected 回调

**新增回调**：

```csharp
session.OnDisconnected += async () =>
{
    try
    {
        _logger.LogWarning("Session for server {ServerId} disconnected", serverId);

        // 更新数据库状态
        using var scope = _serviceScopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IXiaozhiMcpEndpointRepository>();
        
        var server = await repository.GetAsync(serverId);
        if (server != null)
        {
            server.SetDisconnected();
            await repository.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
        }

        // 更新 Redis 状态
        await _connectionStateService.UpdateConnectionStatusAsync(
            serverId,
            ConnectionStatus.Disconnected,
            CancellationToken.None);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling OnDisconnected for server {ServerId}", serverId);
    }
};
```

### 方案 3：在 Ping 处理中检测 WebSocket 状态

**修改点**：`McpSessionService.HandlePingAsync`

```csharp
private async Task HandlePingAsync(int? id, CancellationToken cancellationToken)
{
    // ✅ 首先检查 WebSocket 状态
    if (_webSocket?.State != WebSocketState.Open)
    {
        _logger.LogWarning("Server {ServerId}: Received ping but WebSocket is not open (state: {State})", 
            ServerId, _webSocket?.State);
        
        // 可以选择抛出异常来触发重连
        throw new InvalidOperationException($"WebSocket state is {_webSocket?.State}, not Open");
    }
    
    // 现有的 ping 处理逻辑...
}
```

### 方案 4：添加主动 WebSocket 状态监控（最彻底）

**新增方法**：在 `McpSessionService` 中

```csharp
/// <summary>
/// Monitor WebSocket state actively
/// </summary>
private async Task MonitorWebSocketStateAsync(CancellationToken cancellationToken)
{
    try
    {
        while (_webSocket != null && !cancellationToken.IsCancellationRequested)
        {
            // 每 5 秒检查一次 WebSocket 状态
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            
            if (_webSocket.State != WebSocketState.Open)
            {
                _logger.LogWarning(
                    "Server {ServerId}: WebSocket state changed to {State}, triggering disconnect",
                    ServerId, _webSocket.State);
                
                // 触发重连
                break;
            }
        }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        _logger.LogInformation("Server {ServerId}: WebSocket state monitor cancelled", ServerId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Server {ServerId}: Error in WebSocket state monitor", ServerId);
    }
}
```

**在 ConnectAsync 中启动监控**：

```csharp
// Run bidirectional communication + state monitoring
var communicationTasks = new List<Task>
{
    PipeWebSocketToMcpAsync(cancellationToken),
    PipeMcpToWebSocketAsync(cancellationToken),
    MonitorWebSocketStateAsync(cancellationToken)  // ✅ 新增
};
```

### 方案 5：优化后台监控服务

**修改点**：`ConnectionMonitorHostedService.UpdateLocalConnectionHeartbeatsAsync`

```csharp
private async Task UpdateLocalConnectionHeartbeatsAsync(...)
{
    var localSessions = sessionManager.GetAllSessions();

    foreach (var session in localSessions)
    {
        // ✅ 不管是否连接，都检查状态
        if (session.Value.IsConnected)
        {
            // 连接正常，更新心跳
            await connectionStateService.UpdateHeartbeatAsync(
                session.Key,
                cancellationToken);
        }
        else
        {
            // ✅ 连接断开，立即更新 Redis 状态
            _logger.LogWarning(
                "Detected disconnected session for server {ServerId} in local check",
                session.Key);
            
            await connectionStateService.UpdateConnectionStatusAsync(
                session.Key,
                ConnectionStatus.Disconnected,
                cancellationToken);
            
            // 可选：从本地移除
            await sessionManager.StopSessionAsync(session.Key);
        }
    }
}
```

## 🎯 推荐实施顺序

### 第一阶段：紧急修复（立即实施）

1. **方案 2**：添加 OnDisconnected 回调（影响最小，效果明显）
2. **方案 5**：优化后台监控服务（立即检测断开状态）

### 第二阶段：增强检测（后续优化）

3. **方案 3**：在 Ping 处理中检测 WebSocket 状态
4. **方案 4**：添加主动 WebSocket 状态监控（最彻底）

## 📝 测试验证

### 测试场景 1：小智服务器正常关闭连接

**操作**：关闭小智服务器

**预期**：
- 服务端立即检测到 WebSocket Close 消息
- 触发 OnDisconnected 回调
- 更新数据库 IsConnected = false
- 更新 Redis Status = Disconnected
- UI 显示断开状态

### 测试场景 2：网络突然中断

**操作**：断开网络连接

**预期**：
- WebSocket 读取超时或异常
- 进入 ConnectAsync 的 catch 块
- 触发重连机制
- 后台监控服务检测到心跳超时（90秒内）

### 测试场景 3：小智发送 Ping

**操作**：小智正常发送 ping 请求

**预期**：
- 检测到 WebSocket 状态正常
- 向所有 MCP 客户端发送 ping
- 返回健康状态给小智
- 如果 WebSocket 已断开，抛出异常触发重连

## 📊 监控指标建议

添加以下监控指标来追踪问题：

1. **状态不一致检测**：
   - 本地 IsConnected vs Redis Status 不匹配计数
   - 数据库 IsConnected vs Redis Status 不匹配计数

2. **断开检测延迟**：
   - WebSocket 实际断开时间 vs Redis 状态更新时间差

3. **Ping 失败率**：
   - Ping 请求总数 vs 失败数
   - WebSocket 发送失败数

## 🔍 日志增强建议

添加以下日志来帮助排查：

```csharp
// 在 PipeWebSocketToMcpAsync 退出时
_logger.LogWarning(
    "Server {ServerId}: WebSocket receive loop exited, State={State}, IsConnected={IsConnected}",
    ServerId, _webSocket?.State, IsConnected);

// 在 SendWebSocketResponseAsync 失败时
_logger.LogError(
    "Server {ServerId}: Failed to send WebSocket response, State={State}",
    ServerId, _webSocket?.State);
```

## 总结

**核心问题**：WebSocket 断开时没有主动更新状态，导致状态检测延迟。

**推荐方案**：
1. 立即实施：添加 OnDisconnected 回调 + 优化后台监控
2. 后续增强：添加主动状态监控 + Ping 中的状态检测

**预期效果**：
- 断开检测延迟从 90 秒降低到 5-30 秒
- 状态一致性显著提升
- 更快的故障恢复

---

**创建时间**：2025-01-14  
**分析者**：GitHub Copilot  
**状态**：待实施
