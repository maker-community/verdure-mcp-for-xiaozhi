# Timeout Optimization & SSE Connection Management

## 🔍 SDK 源码分析结论

**重要发现**：基于对 [modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk) 源码的深入分析：

### SSE 连接生命周期机制

1. **SDK 不配置连接池参数**
   - `SseClientSessionTransport` 和 `HttpClientTransport` 都不设置 `PooledConnectionLifetime` 或 `PooledConnectionIdleTimeout`
   - SDK 完全依赖 HttpClient 的默认连接池行为

2. **SSE 连接通过 Stream 持有**
   ```csharp
   // SseClientSessionTransport.cs
   using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
   await foreach (SseItem<string> sseEvent in SseParser.Create(stream).EnumerateAsync(cancellationToken))
   {
       // 持续读取事件流，连接不会归还到连接池
   }
   ```
   - SSE 使用单个长期 GET 请求建立流连接
   - 连接由 stream 持有，只要 `ReceiveMessagesAsync` 在运行，连接就保持活跃
   - 连接通过 `CancellationToken` 或 `Dispose` 关闭，而非连接池超时

3. **会话管理在服务端**
   - 服务端有 IdleTimeout（默认 2 小时）管理会话
   - 会话过期后，客户端使用旧 sessionId 会收到 404

### 结论

❌ **错误做法**：为 SSE 设置 `Timeout.InfiniteTimeSpan` 
- SSE 连接根本不受连接池超时影响（stream 持续持有）
- PooledConnectionIdleTimeout 无效（连接一直在使用，永不 idle）

✅ **正确做法**：使用 SDK 默认配置
- 不干预连接池设置
- 只设置 `HttpClient.Timeout` 用于工具调用快速失败
- 依赖 SDK 的 stream 管理机制

## 📋 原始问题分析

### 问题描述
小智服务端每 60 秒发送一次 ping 到我们的 MCP 平台，平台需要将 ping 转发给所有绑定的 MCP 工具。如果其中一个工具不可用或响应很慢，会导致整个连接被判断为失败并触发重连。

### 根本原因

问题的核心在于**超时时间的层级冲突**：

```
小智的 ping 周期: 60秒
    ↓
我们的 Ping 超时检测: 90秒（如果90秒未收到ping则断开）
    ↓
HttpClient 单次请求超时: 30秒
    ↓
实际场景: 如果有多个慢速工具，每个耗时接近30秒
         → 总时间可能超过60秒
         → 阻塞下一个ping的接收
         → 最终触发90秒超时
```

**恶性循环流程**：
1. 小智发送 ping (t=0)
2. 平台向 5 个 MCP 工具转发 ping
3. 其中 2 个工具很慢，每个耗时 28 秒才超时
4. `Task.WhenAll` 等待所有 ping 完成，总耗时约 28 秒
5. 响应返回给小智 (t=28)
6. 小智下一个 ping 在 t=60 发出
7. 如果持续遇到慢速工具，累积延迟可能导致某个 ping 超过 90 秒才处理完
8. 触发 `_pingTimeout` 断开连接

## ✅ 优化方案

### 1. 缩短 HttpClient.Timeout：30秒 → 10秒

**位置**: `McpSessionService.cs`, line ~278

**修改前**:
```csharp
Timeout = TimeSpan.FromSeconds(30)  // 30秒太长
```

**修改后**:
```csharp
Timeout = TimeSpan.FromSeconds(10)  // 快速失败慢速工具
```

**原因**:
- 单个工具的超时应该远小于 ping 周期（60秒）
- 10 秒足够大部分正常工具响应
- 快速失败可以避免阻塞整个 ping 流程
- 即使 5 个工具全部超时，总时间也只有约 10 秒

### 2. 添加 HandlePingAsync 总体超时：15秒

**位置**: `McpSessionService.cs`, `HandlePingAsync` 方法

**新增代码**:
```csharp
// 创建一个 15秒总体超时的 CancellationToken，防止 ping 处理阻塞太久
using var pingTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, pingTimeoutCts.Token);
var pingCancellationToken = linkedCts.Token;
```

**原因**:
- 提供**双重保险**：即使所有工具都很慢，15 秒后强制中断
- 15 秒 < 60 秒（小智 ping 周期），留有足够缓冲
- 避免阻塞 WebSocket 消息接收循环（`PipeWebSocketToMcpAsync`）

### 3. 延长 _pingTimeout：90秒 → 120秒

**位置**: `McpSessionService.cs`, line ~35

**修改前**:
```csharp
private readonly TimeSpan _pingTimeout = TimeSpan.FromSeconds(90);
```

**修改后**:
```csharp
private readonly TimeSpan _pingTimeout = TimeSpan.FromSeconds(120);
```

**原因**:
- 小智每 60 秒发一次 ping
- 90 秒只能容忍 1 次 ping 丢失（60+30秒缓冲）
- 120 秒可以容忍 2 次 ping 丢失（60+60秒缓冲）
- 提供更多容错空间，避免因网络抖动误判

## 📊 优化后的超时层级

```
┌─────────────────────────────────────────────────────────────┐
│ Ping 超时检测: 120秒                                          │
│ (如果120秒未收到小智的ping，视为连接断开)                        │
│                                                              │
│  ┌────────────────────────────────────────────────────┐     │
│  │ 小智 Ping 周期: 60秒                                 │     │
│  │                                                     │     │
│  │  ┌──────────────────────────────────────────┐     │     │
│  │  │ HandlePingAsync 总体超时: 15秒            │     │     │
│  │  │                                          │     │     │
│  │  │  ┌────────────────────────────────┐     │     │     │
│  │  │  │ HttpClient.Timeout: 10秒        │     │     │     │
│  │  │  │ (单个工具的 ping 超时)            │     │     │     │
│  │  │  └────────────────────────────────┘     │     │     │
│  │  └──────────────────────────────────────────┘     │     │
│  └────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────┘
```

### 超时关系说明

| 超时类型 | 时间 | 作用 | 触发后果 |
|---------|------|------|----------|
| **HttpClient.Timeout** | 10秒 | 单个 MCP 工具的 ping 请求超时 | 该工具 ping 失败（不影响其他工具） |
| **HandlePingAsync 总超时** | 15秒 | 所有工具 ping 的总时间限制 | 强制中断，返回部分成功结果 |
| **小智 Ping 周期** | 60秒 | 小智定期发送 ping 的间隔 | 触发下一次 ping 处理 |
| **_pingTimeout** | 120秒 | 如果太久未收到小智的 ping | 判定连接死亡，触发重连 |
| **心跳超时** | 90秒 | 如果太久未更新本实例心跳 | 被视为实例崩溃，其他实例接管 |

## 🎯 优化效果

### Before (优化前)
- **单工具超时**: 30秒
- **多工具串行最坏情况**: 30秒 × N (可能很长)
- **总体无限制**: 可能超过 60 秒，阻塞下一个 ping
- **Ping 超时检测**: 90 秒（容错空间小）

**问题**:
```
Ping 1 (t=0)  → 转发 → 工具A(28s) + 工具B(28s) → 总耗时 28s → 返回(t=28)
Ping 2 (t=60) → 转发 → 工具A(28s) + 工具B(28s) → 总耗时 28s → 返回(t=88)
Ping 3 (t=120) → 如果再延迟 → 可能触发 90 秒 timeout → ❌ 连接断开
```

### After (优化后)
- **单工具超时**: 10秒 ✅
- **总体超时限制**: 15秒 ✅
- **Ping 超时检测**: 120 秒（容错空间大）✅

**效果**:
```
Ping 1 (t=0)  → 转发 → 工具A(10s超时) + 工具B(10s超时) → 最多15s → 返回(t=15)
Ping 2 (t=60) → 转发 → 同上 → 最多15s → 返回(t=75)
Ping 3 (t=120) → 转发 → 同上 → 最多15s → 返回(t=135)
...
即使连续多个 ping，也不会触发 120 秒 timeout ✅
```

## 📝 日志改进

优化后的日志会显示更详细的超时信息：

```log
Server xxx: Ping completed - 3/5 clients responded successfully, 
total time: 12500ms, avg response: 250.00ms (HttpClient timeout: 10s, total limit: 15s)
```

包含信息：
- 成功/总数
- 总耗时
- 平均响应时间
- 配置的超时参数（便于调试）

## 🔍 相关配置

### ConnectionMonitorHostedService 配置
```json
{
  "ConnectionMonitor": {
    "CheckIntervalSeconds": 30,       // 每30秒检查一次连接状态
    "HeartbeatTimeoutSeconds": 90,    // 90秒未更新心跳视为实例崩溃
    "ReconnectCooldownSeconds": 60    // 断开后等待60秒才重连
  }
}
```

这些配置**不需要修改**，因为：
- 心跳超时（90秒）与 ping 超时（120秒）独立运行
- 心跳是本实例健康检查，ping 是小智连接检查
- 重连冷却期（60秒）合理，避免频繁重连

## ✅ 验证方法

### 1. 模拟慢速工具
在某个 MCP 工具的 ping 处理中添加延迟：
```csharp
await Task.Delay(TimeSpan.FromSeconds(20)); // 模拟慢速响应
```

### 2. 观察日志
```log
# 期望看到：
Server xxx: Ping to MCP client 2 timed out (exceeded 15s total limit)
Server xxx: Ping completed - 4/5 clients responded successfully, total time: 15000ms
```

### 3. 验证连接稳定性
- 即使有 1-2 个工具持续失败/超时
- 连接应该保持稳定，不会触发重连
- 其他工具仍然正常工作

## 🚀 部署建议

### HTTP 错误诊断

如果遇到 404 或其他 HTTP 错误，现在会看到详细日志：

```log
# 示例错误日志
HTTP request failed to MCP service calculator at http://service:8080/sse: 
StatusCode=404, Protocol=sse
```

可能的原因：
1. **MCP 服务端会话过期**：服务端重启或会话超时（默认2小时）
2. **端点地址错误**：配置的 endpoint 地址不正确
3. **服务端不支持该协议**：检查 protocol 配置（sse/http/stdio）

### 故障恢复机制（待实现）

基于 SDK 源码分析，未来可以添加：

1. **会话恢复**：捕获 404 错误时自动重建会话
   ```csharp
   catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
   {
       // 会话已过期，创建新连接
       await ReconnectServiceAsync(serviceId);
   }
   ```

2. **连接健康检查**：定期验证 SSE stream 是否仍然活跃
   ```csharp
   // 检测 stream 是否已断开
   if (!stream.CanRead)
   {
       await ReconnectAsync();
   }
   ```

### 滚动更新
1. 更新代码
2. 重启一个实例
3. 观察 5-10 分钟
4. 确认 ping 日志正常
5. 继续更新其他实例

### 监控指标
- `Ping completed` 日志中的成功率
- `total time` 是否稳定在 15 秒以内
- 是否仍有 `Ping timeout detected` 错误

## 📚 相关文件

- `src/Verdure.McpPlatform.Api/Services/WebSocket/McpSessionService.cs`
  - `_pingTimeout` (line ~35)
  - `HttpClient.Timeout` (line ~278)
  - `HandlePingAsync` (line ~638)
  
- `src/Verdure.McpPlatform.Api/Services/BackgroundServices/ConnectionMonitorHostedService.cs`
  - 心跳检测逻辑
  - 重连机制

## 🎓 经验总结

### 教训
1. **超时时间必须分层设计**：内层超时 << 外层超时
2. **避免无限等待**：任何网络操作都应该有超时限制
3. **提供容错空间**：不要让超时时间太紧，留有缓冲
4. **日志是关键**：详细的超时日志帮助快速定位问题

### 最佳实践
1. **快速失败原则**：单个请求失败不应影响整体
2. **总体限制保护**：防止累积延迟导致阻塞
3. **合理的容错**：允许部分失败，只要不是全部失败
4. **监控和告警**：关键超时应该有日志和指标

---

**修改日期**: 2025-11-21  
**版本**: 1.0  
**状态**: ✅ 已实施
