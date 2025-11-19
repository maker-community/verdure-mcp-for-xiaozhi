# ModelScope MCP 服务器 C# 测试说明

## 📋 测试文件概览

文件位置: `src/Verdure.McpPlatform.Tests/ModelScopeServerTests.cs`

## ✅ 测试用例列表

### 1. Test_ModelScope_DefaultConfiguration
**目的**: 使用默认配置连接 ModelScope 服务器

**配置**:
- Transport Mode: StreamableHttp
- 无自定义 session ID
- 标准 MCP 客户端配置

**预期结果**: 
- ❌ 失败（400 Bad Request）
- ModelScope 要求在首次请求就提供 session ID（违反 MCP 协议）

**状态**: ✅ 已优化（正确展示标准流程）

---

### 2. Test_ModelScope_CheckUnauthorizedErrorType
**目的**: 检查使用随机 session ID 时的错误类型

**配置**:
- 手动提供随机 GUID 作为 session ID
- 用于验证 401 错误的具体行为

**预期结果**:
- ❌ 失败（401 Unauthorized）
- Session ID 未知或无效

**状态**: ✅ 已优化（用于错误诊断）

---

### 3. ~~Test_ModelScope_WithUrlIdAsSessionId~~ → Test_ModelScope_CaptureSessionIdFromServer
**目的**: 展示标准 MCP 协议的 session ID 获取流程

**原问题**: 
- ❌ 错误地将 URL 中的 `4fbe8c9a28e148` 当作 session ID
- URL 中的 ID 是端点标识符，不是 session ID

**优化后**:
- ✅ 演示正确的 MCP 协议流程
- 使用 SessionCapturingHandler 捕获服务器返回的 session ID
- 记录完整的请求/响应 headers

**预期结果**:
- 标准 MCP 服务器应返回 200 + mcp-session-id header
- ModelScope 返回 400（不符合协议）

**状态**: ✅ 已修复

---

### 4. ~~Test_ModelScope_WithRandomSessionId~~ → Test_ModelScope_LetSdkHandleSessionId
**目的**: 展示 SDK 自动管理 session ID 的正确方式

**原问题**:
- ❌ 手动生成并注入随机 session ID
- 违背 SDK 设计初衷（SDK 应自动处理）

**优化后**:
- ✅ 不手动设置 session ID
- ✅ 使用 SessionCapturingHandler 监控 SDK 行为
- ✅ 验证 SDK 是否正确使用服务器返回的 session ID

**预期结果**:
- SDK 自动从响应头提取并在后续请求使用 session ID
- ModelScope 返回 400（因为首次请求无 session ID）

**状态**: ✅ 已修复

---

### 5. Test_ModelScope_CompleteWorkflow (新增)
**目的**: 演示完整的生产环境工作流程

**流程**:
1. 连接服务器（initialize）
2. 列出可用工具（tools/list）
3. 调用工具（tools/call）

**配置**:
- 使用 SessionCapturingHandler 跟踪 session ID
- 完整的错误处理
- 详细的日志输出

**预期结果**:
- 如果 ModelScope 符合协议：完整流程成功
- 当前实际：在 initialize 阶段失败（400）

**状态**: ✅ 新增（生产就绪示例）

---

### 6. Test_ModelScope_SseMode
**目的**: 测试 SSE (Server-Sent Events) 传输模式

**配置**:
- Transport Mode: SSE
- 使用 Accept: text/event-stream

**预期结果**:
- 测试 SSE 连接是否可用
- ModelScope 可能不支持 SSE

**状态**: ⚠️ 需要优化（简化 session ID 处理）

---

### 7. Test_ModelScope_AutoDetectMode
**目的**: 测试自动检测传输模式功能

**配置**:
- Transport Mode: AutoDetect
- SDK 自动选择最佳模式

**预期结果**:
- SDK 根据服务器响应选择传输方式

**状态**: ⚠️ 需要优化（简化 session ID 处理）

---

### 8. Test_ModelScope_RawHttpRequest
**目的**: 绕过 SDK，直接发送 HTTP 请求

**配置**:
- 使用原始 HttpClient
- 手动构造 JSON-RPC 请求体
- 用于诊断底层 HTTP 问题

**预期结果**:
- 帮助识别是 SDK 问题还是服务器问题

**状态**: ✅ 保留（诊断工具）

---

### 9. Test_ModelScope_WithSessionIdHeader
**目的**: 测试手动提供有效 session ID 的场景

**配置**:
- 从外部获取有效 session ID
- 手动设置在请求头中

**预期结果**:
- 如果有有效 session ID，连接应该成功

**状态**: ⚠️ 需要更新（添加如何获取 session ID 的说明）

---

## 🔧 辅助类

### SessionCapturingHandler
**位置**: ModelScopeServerTests.cs (新增)

**功能**:
- 继承自 `DelegatingHandler`
- 拦截 HTTP 响应
- 从 `mcp-session-id` 响应头提取 session ID
- 记录 session ID 生命周期

**使用场景**:
```csharp
var handler = new SessionCapturingHandler();
var httpClient = new HttpClient(handler);

// ... 使用 httpClient 创建 transport 和 client ...

// 连接后检查捕获的 session ID
Console.WriteLine($"Session ID: {handler.SessionId}");
```

---

### LoggingHttpMessageHandler
**位置**: ModelScopeServerTests.cs (已存在)

**功能**:
- 记录所有 HTTP 请求和响应
- 包括 headers、body、status code
- 用于深度调试

**使用场景**:
```csharp
var handler = new LoggingHttpMessageHandler(_output);
var httpClient = new HttpClient(handler);
```

---

## 📊 测试结果总结

| 测试名称 | 状态 | Session ID 处理 | 预期/实际结果 |
|---------|------|----------------|--------------|
| DefaultConfiguration | ✅ 优化 | SDK 自动管理 | 400（ModelScope 不符合协议） |
| CheckUnauthorizedErrorType | ✅ 优化 | 手动随机 ID | 401（用于错误诊断） |
| CaptureSessionIdFromServer | ✅ 修复 | 等待服务器返回 | 展示标准流程 |
| LetSdkHandleSessionId | ✅ 修复 | SDK 自动管理 | 展示正确用法 |
| CompleteWorkflow | ✅ 新增 | SessionCapturingHandler | 生产环境示例 |
| SseMode | ⚠️ 待优化 | 需简化 | SSE 传输测试 |
| AutoDetectMode | ⚠️ 待优化 | 需简化 | 自动检测测试 |
| RawHttpRequest | ✅ 保留 | 手动构造 | 底层诊断工具 |
| WithSessionIdHeader | ⚠️ 待更新 | 外部提供 | 有效 ID 测试 |

---

## 🎯 主要修复点

### ❌ 修复前的错误

1. **错误 1**: 将 URL 路径当作 session ID
```csharp
// ❌ 错误！
var urlParts = new Uri(ModelScopeEndpoint).Segments;
var sessionId = urlParts[urlParts.Length - 2].TrimEnd('/'); // "4fbe8c9a28e148"
```

2. **错误 2**: 手动生成随机 session ID
```csharp
// ❌ 错误！应该让 SDK 处理
var sessionId = Guid.NewGuid().ToString();
```

3. **错误 3**: 在 initialize 前就设置 session ID
```csharp
// ❌ 违反 MCP 协议
AdditionalHeaders = new Dictionary<string, string>
{
    ["mcp-session-id"] = someId
}
```

### ✅ 修复后的正确做法

1. **正确 1**: 理解 URL 结构
```csharp
// ✅ 正确理解
// https://mcp.api-inference.modelscope.net/4fbe8c9a28e148/mcp
//                                          ^^^^^^^^^^^^^^
//                                          这是端点标识符，不是 session ID
```

2. **正确 2**: 让 SDK 管理 session ID
```csharp
// ✅ 正确！不手动设置 session ID
var transportOptions = new HttpClientTransportOptions
{
    Endpoint = new Uri(ModelScopeEndpoint),
    TransportMode = HttpTransportMode.StreamableHttp
    // SDK 会自动处理 session ID
};
```

3. **正确 3**: 使用 handler 监控 session ID
```csharp
// ✅ 正确！用于调试和验证
var handler = new SessionCapturingHandler();
var httpClient = new HttpClient(handler);
// 连接后，handler.SessionId 包含服务器返回的 session ID
```

---

## 📚 相关文档

- [ModelScope C# SDK 使用指南](./MODELSCOPE_CSHARP_USAGE_GUIDE.md)
- [ModelScope 调查总结](./MODELSCOPE_INVESTIGATION_SUMMARY.md)
- [ModelScope Session 机制分析](./MODELSCOPE_SESSION_MECHANISM.md)
- [TypeScript SDK Session 管理](https://github.com/modelcontextprotocol/typescript-sdk)

---

## 🔍 下一步优化

1. ✅ **已完成**: 修复 session ID 错误理解
2. ✅ **已完成**: 添加 SessionCapturingHandler
3. ✅ **已完成**: 添加完整工作流程示例
4. ⏳ **待完成**: 简化 SSE/AutoDetect 测试
5. ⏳ **待完成**: 添加如何获取有效 session ID 的文档
6. ⏳ **待完成**: 研究 ModelScope 的认证机制（OAuth 2.0?）

---

## 💡 关键要点

1. **Session ID 来自服务器**，不是 URL 的一部分
2. **SDK 应自动管理** session ID，不需要手动干预
3. **首次 initialize 请求**不应该包含 session ID
4. **ModelScope 不符合标准 MCP 协议**，需要额外的认证流程
5. **使用 SessionCapturingHandler** 可以监控 session ID 生命周期
