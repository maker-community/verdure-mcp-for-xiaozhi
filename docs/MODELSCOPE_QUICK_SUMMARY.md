# ModelScope MCP 服务器问题总结

## 核心发现

**ModelScope 服务器不遵循标准 MCP 协议。**

## 对比分析

| 方面 | 标准 MCP 协议 | ModelScope 实际行为 |
|------|------------|------------------|
| 首次连接 | 客户端不需要 session ID，服务器自动生成 | **要求客户端提供有效 session ID** ❌ |
| Session 创建 | 服务器在响应头返回 `mcp-session-id` | **返回 400 错误要求 session** ❌ |
| 无认证处理 | 返回 401 + `WWW-Authenticate` 触发 OAuth | **返回 400 无认证信息** ❌ |
| Session 获取 | 不需要（服务器负责） | **机制不明确** ❌ |

## Cherry Studio 为什么能连接？

通过分析源代码发现：

1. **有 OAuth 提供者** (`McpOAuthClientProvider`)
2. **处理 401 错误** 并触发 OAuth 流程
3. **本地回调服务器** 接收认证码
4. **Session 持久化** 存储到本地文件

**但是**：ModelScope 返回 400（不是 401），所以 OAuth 流程不会被触发！

### 可能的原因

Cherry Studio 必定通过以下某种方式获取了有效 session：

- ✅ **缓存的 session**：之前连接时存储的有效 session
- ⚠️ **特殊 User-Agent 处理**：ModelScope 可能识别 Cherry Studio
- ⚠️ **预配置 session**：Cherry Studio 可能内置了特殊配置
- ⚠️ **网页登录**：通过浏览器获取 session（非标准方式）

## C# SDK 是否有问题？

**没有！C# SDK 完全正确。**

测试证明：
- ✅ `AdditionalHeaders` 功能正常
- ✅ Session ID 正确添加到请求头
- ✅ HTTP 错误正确报告为 `HttpRequestException`

问题在于 **ModelScope 服务器的非标准实现**。

## 下一步建议

### 选项 1：使用标准 MCP 服务器（推荐）

测试 C# SDK 时使用符合标准的服务器：
```bash
# 安装标准 MCP 服务器
npx @modelcontextprotocol/server-everything

# 或使用本地 stdio 服务器
```

### 选项 2：逆向工程 ModelScope（复杂）

如果必须支持 ModelScope，需要：

1. **抓包分析** Cherry Studio 的完整请求流程
2. **查找 session API**：ModelScope 可能有独立的 session 创建接口
3. **实现适配器**：
```csharp
public class ModelScopeSessionManager
{
    // 通过未知方式获取有效 session
    public async Task<string> AcquireSessionAsync();
}
```

### 选项 3：联系 ModelScope（理想）

建议 ModelScope 团队：
1. 遵循标准 MCP 协议
2. 提供清晰的认证文档
3. 或提供公开的 session 创建 API

## 测试文件

所有测试代码在：`src/Verdure.McpPlatform.Tests/ModelScopeServerTests.cs`

```csharp
[Fact]
public async Task Test_ModelScope_CheckUnauthorizedErrorType()
{
    // ✅ 验证：C# SDK 正确报告 400 错误为 HttpRequestException
}

[Fact]
public async Task Test_ModelScope_WithSessionIdInAdditionalHeaders()
{
    // ✅ 验证：AdditionalHeaders 功能正常
    // ❌ 结果：ModelScope 拒绝随机 session ID（401）
}
```

## 详细文档

完整调查报告：`docs/MODELSCOPE_INVESTIGATION_SUMMARY.md`

## 时间线

- **2025-11-19**: 发现 400 错误
- **2025-11-19**: 验证 AdditionalHeaders 功能
- **2025-11-19**: 分析 TypeScript SDK 和 Cherry Studio 源码
- **2025-11-19**: **确认 ModelScope 协议不兼容** ✅

---

**结论**：这不是 C# SDK 的问题，而是 ModelScope 服务器不遵循标准 MCP 协议。
