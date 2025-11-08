# 空绑定列表错误修复

## 问题描述

当小智连接（XiaozhiMcpEndpoint）的配置被启用，但所有关联的服务绑定（ServiceBindings）都被禁用时，会导致以下错误：

```
错误位置: McpSessionService.PipeMcpToWebSocketAsync()
文件: McpSessionService.cs:363
```

## 根本原因

1. **服务绑定过滤**: `McpSessionManager.cs` 在创建会话时会过滤掉所有非活动的绑定：
   ```csharp
   foreach (var binding in server.ServiceBindings.Where(b => b.IsActive))
   ```

2. **空 MCP 客户端列表**: 当所有绑定都被禁用时，`mcpServiceEndpoints` 列表为空，导致不会创建任何 MCP 客户端。

3. **WebSocket 连接成功但无法处理请求**: WebSocket 连接建立成功，但由于没有 MCP 客户端，当收到 `tools/list` 或 `tools/call` 请求时：
   - 之前的实现会静默返回，不发送任何响应
   - 客户端会一直等待，最终超时

## 解决方案

### 1. 预防性检查 (McpSessionManager.cs)

在创建会话前检查是否有活动的服务绑定：

```csharp
// Check if there are any active service bindings
if (mcpServiceEndpoints.Count == 0)
{
    _logger.LogWarning(
        "Server {ServerId} ({ServerName}) has no active service bindings. Cannot create session.",
        serverId, server.Name);
    return false;
}
```

**效果**: 
- 如果没有活动的绑定，直接返回 `false`，不创建会话
- 避免创建无效的 WebSocket 连接
- 记录清晰的警告日志

### 2. 增强错误响应 (McpSessionService.cs)

在 `HandleToolsListAsync` 和 `HandleToolsCallAsync` 方法中，当没有 MCP 客户端时发送明确的错误响应：

```csharp
if (_mcpClients.Count == 0)
{
    _logger.LogWarning("Server {ServerId}: No MCP clients available for tools/list request", ServerId);
    await SendErrorResponseAsync(id, -32603, "No MCP services available", 
        "No active MCP service bindings configured for this endpoint", cancellationToken);
    return;
}
```

**效果**:
- 客户端收到清晰的错误消息，而不是超时
- 日志中记录详细的警告信息
- 符合 JSON-RPC 2.0 错误响应规范

### 3. 移除过度限制 (McpSessionService.cs)

在 `ProcessWebSocketMessageAsync` 方法中移除对 `_mcpClients.Count` 的检查：

```csharp
// 之前: if (_webSocket == null || _mcpClients.Count == 0) return;
// 现在: if (_webSocket == null) return;
```

**原因**:
- `initialize` 和 `ping` 等方法不需要 MCP 客户端
- 让各个具体的处理方法自己决定如何处理空客户端列表

## 预期行为

### 场景 1: 启用连接但禁用所有绑定

**之前**:
- WebSocket 连接建立
- 客户端请求 `tools/list` 时无响应
- 最终超时，错误不明确

**现在**:
- 会话创建失败（返回 `false`）
- 日志记录: "Server {ServerId} has no active service bindings"
- 不会建立 WebSocket 连接
- 避免资源浪费和混乱的错误

### 场景 2: 连接建立后所有绑定被禁用（极端情况）

如果在会话运行期间所有绑定被动态禁用（虽然不常见）：

**现在的行为**:
- 客户端收到 JSON-RPC 错误响应：
  ```json
  {
    "jsonrpc": "2.0",
    "id": 1,
    "error": {
      "code": -32603,
      "message": "No MCP services available",
      "data": "No active MCP service bindings configured for this endpoint"
    }
  }
  ```
- 日志中有明确的警告
- 客户端可以优雅地处理错误

## 测试建议

1. **测试空绑定场景**:
   - 创建一个小智连接
   - 不添加任何服务绑定，或添加后全部禁用
   - 尝试启用连接
   - 验证日志中有 "no active service bindings" 警告
   - 验证连接状态保持为断开

2. **测试错误响应**:
   - 在特殊情况下如果空绑定的会话被创建
   - 发送 `tools/list` 请求
   - 验证收到明确的错误响应（而不是超时）

## 相关文件

- `src/Verdure.McpPlatform.Api/Services/WebSocket/McpSessionManager.cs`
- `src/Verdure.McpPlatform.Api/Services/WebSocket/McpSessionService.cs`

## 日期

2025-11-08
