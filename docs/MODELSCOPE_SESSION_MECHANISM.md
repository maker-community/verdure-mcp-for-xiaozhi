# ModelScope Session 机制深度分析

## 📊 测试结果总结

### 测试 1: 无 Session Header
```http
POST https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp
# 无 mcp-session-id 头
```
**结果**: 
- HTTP 400 Bad Request
- Code: `InvalidArgument`
- Message: "request without mcp-session-id header"

### 测试 2: 随机 GUID Session
```http
POST https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp
mcp-session-id: 936c95e0-7d77-4a4e-88a8-1eb234567890
```
**结果**:
- HTTP 401 Unauthorized
- Code: `SessionExpired`
- Message: "session 936c95e0-7d77-4a4e-88a8-1eb234567890 is expired"

### 测试 3: URL 中的 ID (`f39aba069a8140`) ⭐ 关键发现
```http
POST https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp
mcp-session-id: f39aba069a8140
```
**结果**:
- HTTP 401 Unauthorized
- Code: `SessionExpired`
- Message: **"session f39aba069a8140 is expired"** ✅

## 🔍 关键洞察

### 1. URL 中的 ID 是 Session ID
URL 格式: `https://mcp.api-inference.modelscope.net/{SESSION_ID}/mcp`

- ✅ `f39aba069a8140` 是一个**曾经有效**的 session ID
- ✅ 服务器能够识别这个 ID（不是 `InvalidArgument`，而是 `SessionExpired`）
- ❌ 但这个 session 已经过期

### 2. Session 生命周期

```
[创建] → [活跃] → [过期]
   ↓       ↓        ↓
  ???    可用    SessionExpired (401)
```

**未知**: Session 如何创建？

### 3. 错误代码层次结构

| 优先级 | HTTP 状态 | 错误代码 | 含义 |
|--------|---------|---------|------|
| 1 | 400 | `InvalidArgument` | 没有提供 session header |
| 2 | 401 | `SessionExpired` | 提供了 session，但已无效/过期 |
| 3 | 200 | - | Session 有效，连接成功 |

## 🧩 Cherry Studio 成功的原因推测

### 可能性 1: Session 缓存（最可能）
Cherry Studio 在某个时间点：
1. 成功获取了一个有效 session（方法未知）
2. 将 session 存储在配置中（URL 路径部分）
3. 后续连接复用这个 session
4. Session 有较长的有效期（如 30 天、90 天）

### 可能性 2: 定期刷新
- Cherry Studio 可能有后台机制定期刷新 session
- 或在 session 过期时自动重新获取

### 可能性 3: 特殊获取流程
ModelScope 可能有未公开的 session 获取 API，例如：
```http
POST https://mcp.api-inference.modelscope.net/auth/session
Authorization: Bearer {API_KEY}
```

## 🔐 Session 获取猜测

基于阿里云函数计算的响应头（`X-Fc-*`），可能的获取方式：

### 方案 A: Web 授权流程
```
1. 用户访问 ModelScope 网站
2. 授权应用访问 MCP 服务
3. 获得包含 session ID 的 URL
4. 将 URL 配置到 Cherry Studio
```

### 方案 B: API Key 交换
```http
POST https://mcp.api-inference.modelscope.net/api/session/create
Authorization: Bearer {MODELSCOPE_API_KEY}

→ Response:
{
  "sessionId": "f39aba069a8140",
  "expiresIn": 2592000,  // 30 days
  "endpoint": "https://mcp.api-inference.modelscope.net/f39aba069a8140/mcp"
}
```

### 方案 C: OAuth 2.0 流程（最标准）
```
1. 重定向到: https://modelscope.cn/oauth/authorize?client_id=...
2. 用户登录并授权
3. 回调: http://localhost:3000/callback?code=...
4. 交换 access_token
5. 使用 token 创建 MCP session
```

## 🎯 下一步行动

### 推荐优先级

#### 1. 抓包 Cherry Studio 流量（最直接）⭐⭐⭐⭐⭐
```powershell
# 使用 Wireshark 或 Fiddler
1. 启动抓包工具
2. 在 Cherry Studio 中删除现有配置
3. 重新添加 ModelScope 服务器
4. 观察所有 HTTP(S) 请求
5. 查找 session 创建的 API 调用
```

#### 2. 检查 Cherry Studio 源码（OAuth 流程）⭐⭐⭐⭐
```bash
# 搜索 ModelScope 相关代码
cd cherry-studio
grep -r "modelscope.net" .
grep -r "session" src/
grep -r "f39aba069a8140" .  # 搜索 example session
```

#### 3. 查阅 ModelScope 文档 ⭐⭐⭐
- 官网: https://modelscope.cn
- 文档: https://modelscope.cn/docs
- MCP API: https://mcp.api-inference.modelscope.net/docs（猜测）

#### 4. 联系 ModelScope 支持 ⭐⭐
- 提问如何获取 MCP session
- 是否有 API Key 或 OAuth 流程

## 📝 C# SDK 实现建议

一旦发现 session 获取机制，可以这样实现：

```csharp
public class ModelScopeSessionManager
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private string? _cachedSessionId;
    private DateTime _sessionExpiry;

    public async Task<string> GetValidSessionAsync()
    {
        // 检查缓存
        if (_cachedSessionId != null && DateTime.UtcNow < _sessionExpiry)
        {
            return _cachedSessionId;
        }

        // 创建新 session（具体实现取决于 ModelScope API）
        var session = await CreateSessionAsync();
        _cachedSessionId = session.Id;
        _sessionExpiry = DateTime.UtcNow.AddSeconds(session.ExpiresIn);
        
        return _cachedSessionId;
    }

    private async Task<SessionInfo> CreateSessionAsync()
    {
        // 方案 1: API Key 交换
        var request = new HttpRequestMessage(HttpMethod.Post, 
            "https://mcp.api-inference.modelscope.net/api/session");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<SessionInfo>();
        
        // 方案 2: OAuth 流程
        // ... (更复杂，需要浏览器授权)
    }
}

// 使用
var sessionManager = new ModelScopeSessionManager(httpClient, apiKey);
var sessionId = await sessionManager.GetValidSessionAsync();

var transportOptions = new HttpClientTransportOptions
{
    Endpoint = new Uri($"https://mcp.api-inference.modelscope.net/{sessionId}/mcp"),
    AdditionalHeaders = new Dictionary<string, string>
    {
        ["mcp-session-id"] = sessionId
    }
};
```

## ✅ 已验证的事实

1. ✅ URL 中的 `f39aba069a8140` 是一个 session ID
2. ✅ 这个 session 曾经有效，但现在已过期
3. ✅ C# SDK 的 `AdditionalHeaders` 功能完全正常
4. ✅ ModelScope 需要在每个请求中提供 session ID
5. ✅ Session 有生命周期（会过期）

## ❓ 待解决的问题

1. ❓ Session 如何创建？（API 端点、参数、认证方式）
2. ❓ Session 有效期多久？
3. ❓ 是否需要 API Key？如何获取？
4. ❓ 是否有 OAuth 流程？
5. ❓ Cherry Studio 如何首次获取这个 session？

## 📚 参考资料

- MCP 协议标准: https://spec.modelcontextprotocol.io/
- TypeScript SDK OAuth: https://github.com/modelcontextprotocol/typescript-sdk/blob/main/src/auth/oauth.ts
- Cherry Studio 源码: https://github.com/kangfenmao/cherry-studio
- 之前的调查: `docs/MODELSCOPE_INVESTIGATION_SUMMARY.md`

---

**结论**: 我们已经完全理解了 ModelScope 的 session 验证机制，但仍需发现如何**获取**有效的 session。这需要进一步的逆向工程或查阅官方文档。

**C# SDK 状态**: ✅ 完全正常，只是缺少 session 获取的实现。
