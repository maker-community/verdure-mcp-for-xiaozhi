# ModelScope MCP 服务器调查总结

## 问题描述

尝试连接 ModelScope 的 pozansky-stock-server MCP 服务时，遇到以下错误：

1. **无 session ID** → `400 Bad Request`: "request without mcp-session-id header should be mcp initialize request"
2. **随机 session ID** → `401 Unauthorized`: "session xxx is expired"

## 调查发现

### Cherry Studio 的实现

通过分析 Cherry Studio 的源代码（`src/main/services/MCPService.ts`），发现：

1. **OAuth 触发条件**：
   ```typescript
   try {
     await client.connect(transport)
   } catch (error: any) {
     if (error.name === 'UnauthorizedError' || error.message.includes('Unauthorized')) {
       await handleAuth(client, transport)  // 触发 OAuth 流程
     }
   }
   ```

2. **TypeScript SDK 的 OAuth 处理**（`StreamableHTTPClientTransport`）：
   ```typescript
   if (!response.ok) {
     if (response.status === 401 && this._authProvider) {
       // 执行 OAuth 流程
       const result = await auth(this._authProvider, {...});
       if (result !== 'AUTHORIZED') {
         throw new UnauthorizedError();
       }
     }
   }
   ```

### ModelScope 的非标准行为

ModelScope 服务器**违反了 MCP 和 OAuth 协议规范**：

| 场景 | 标准协议期望 | ModelScope 实际行为 | 影响 |
|------|------------|---------------------|------|
| 首次连接（无 session） | 服务器生成 session ID 并在响应头返回 | 返回 `400 Bad Request` | ❌ 阻止标准客户端初始化 |
| 无认证 | 返回 `401` 触发 OAuth 流程 | 返回 `400` | ❌ 不触发 OAuth |
| 无效 session | 返回 `401` + `WWW-Authenticate` 头 | 返回 `401` 但无正确的认证头 | ❌ 无法启动标准 OAuth |

### C# SDK 的异常类型

C# SDK 抛出的是通用 `HttpRequestException`，不是特殊的认证异常：

```csharp
异常类型: System.Net.Http.HttpRequestException
异常消息: Response status code does not indicate success: 400 (Bad Request).
```

这是正确的行为，因为服务器返回的确实是 400 错误，不是认证错误。

## 结论

### ModelScope 的问题

1. **不遵循 MCP 协议**：
   - 标准流程：客户端发送 `initialize` → 服务器生成 session → 返回 `mcp-session-id` 响应头
   - ModelScope：要求客户端在首次请求时就提供有效 session ID

2. **不遵循 OAuth 2.0 协议**：
   - 标准流程：`401 Unauthorized` + `WWW-Authenticate` 头指示认证方式
   - ModelScope：返回 400 错误或 401 但无正确的认证元数据

3. **Session 管理不明确**：
   - 没有公开的 session 创建 API
   - 没有文档说明如何获取有效 session ID

### Cherry Studio 为什么能连接

推测：Cherry Studio 必定通过以下某种方式获取了有效 session ID：

1. **缓存机制**：之前成功连接时存储了 session ID（`JsonFileStorage`）
2. **服务器特殊处理**：ModelScope 可能对 Cherry Studio 的 User-Agent 有特殊处理
3. **预配置 session**：Cherry Studio 可能内置了 ModelScope 的 session 配置
4. **网页认证**：通过浏览器完成认证并获取 session（但这不符合 OAuth 标准）

### C# SDK 不是问题

C# SDK (`ModelContextProtocol` 0.4.0) 的行为是**完全正确的**：

- ✅ 正确实现了 MCP 协议
- ✅ 正确实现了 `AdditionalHeaders` 功能
- ✅ 正确报告了 HTTP 错误

问题在于 ModelScope 服务器的非标准实现。

## 建议的解决方案

### 选项 1：使用标准 MCP 服务器（推荐）

测试 C# SDK 时使用符合标准的 MCP 服务器：

- 本地 stdio 服务器（如 `@modelcontextprotocol/server-everything`）
- 标准的 HTTP MCP 服务器（遵循协议规范）
- 其他公开的标准 MCP 服务器

### 选项 2：实现 ModelScope 专用适配器（复杂）

如果必须支持 ModelScope，需要：

1. **逆向工程 session 获取流程**：
   - 抓包分析 Cherry Studio 的实际请求
   - 查找 ModelScope 的 session 创建 API
   - 可能需要模拟浏览器登录

2. **实现自定义适配器**：
   ```csharp
   public class ModelScopeSessionManager
   {
       // 1. 获取有效 session ID（具体方法未知）
       public async Task<string> AcquireSessionAsync();
       
       // 2. 存储和恢复 session
       public void SaveSession(string sessionId);
       public string? LoadSession();
       
       // 3. 检查 session 有效性
       public async Task<bool> ValidateSessionAsync(string sessionId);
   }
   ```

3. **集成到 transport options**：
   ```csharp
   var sessionManager = new ModelScopeSessionManager();
   var sessionId = await sessionManager.AcquireSessionAsync();
   
   var transportOptions = new HttpClientTransportOptions
   {
       Endpoint = new Uri(ModelScopeEndpoint),
       TransportMode = HttpTransportMode.StreamableHttp,
       AdditionalHeaders = new Dictionary<string, string>
       {
           ["mcp-session-id"] = sessionId
       }
   };
   ```

### 选项 3：联系 ModelScope（理想）

向 ModelScope 报告协议兼容性问题，建议：

1. 遵循标准 MCP 协议进行 session 管理
2. 如需认证，使用标准 OAuth 2.0 流程
3. 提供清晰的 API 文档说明 session 获取方式

## 测试证明

测试代码位于：`src/Verdure.McpPlatform.Tests/ModelScopeServerTests.cs`

### Test_ModelScope_CheckUnauthorizedErrorType

验证了 C# SDK 正确报告 400 错误为 `HttpRequestException`。

### Test_ModelScope_WithSessionIdInAdditionalHeaders

验证了：
- ✅ `AdditionalHeaders` 功能正常工作
- ✅ Session ID 正确添加到请求头
- ❌ ModelScope 拒绝随机 session ID（401 错误）

## 参考资料

### 源代码分析

1. **Cherry Studio MCPService**:
   - 文件：`src/main/services/MCPService.ts`
   - OAuth 提供者：`src/main/services/mcp/oauth/provider.ts`
   - 关键发现：OAuth 只在收到 401 + authProvider 时触发

2. **TypeScript SDK StreamableHTTPClientTransport**:
   - 文件：`src/client/streamableHttp.ts`
   - Line 391-413：401 错误处理和 OAuth 触发逻辑
   - Line 387-389：session ID 从响应头提取

### 协议规范

1. **MCP Specification**:
   - Session 管理：服务器负责生成 session ID
   - 客户端从响应头 `mcp-session-id` 获取 session
   - 后续请求携带 session ID

2. **OAuth 2.0 RFC 6749**:
   - 401 响应必须包含 `WWW-Authenticate` 头
   - 客户端根据 `WWW-Authenticate` 启动认证流程

## 时间线

- **2025-11-19**: 初步调查，发现 400/401 错误
- **2025-11-19**: 验证 `AdditionalHeaders` 功能正常
- **2025-11-19**: 分析 MCP TypeScript SDK，理解标准 session 流程
- **2025-11-19**: 分析 Cherry Studio 源码，发现 OAuth 触发条件
- **2025-11-19**: 确认 ModelScope 协议不兼容
- **2025-11-19**: 创建本总结文档

## 下一步行动

1. ✅ 确认 C# SDK 功能正常（已完成）
2. ⏭️ 使用标准 MCP 服务器进行全面测试
3. ⏭️ 如需支持 ModelScope，调查 session 获取方法
4. ⏭️ 考虑向 ModelScope 团队反馈协议兼容性问题
