# Keycloak Refresh Token 故障修复

## 🚨 问题描述

在使用 Keycloak 进行 OIDC 认证时，刷新令牌调用返回错误：

```json
{
  "error": "invalid_grant",
  "error_description": "Token is not active"
}
```

## 🔍 问题原因

### 1. **缺少 `offline_access` scope** ⭐ 最常见原因

**现象**：
- Access Token 过期后无法自动刷新
- Refresh Token 请求返回 `invalid_grant` 错误

**原因**：
- `offline_access` scope 是 OAuth2/OIDC 标准中用于获取 Refresh Token 的特殊 scope
- 没有这个 scope，Keycloak 不会返回 Refresh Token，或返回的 Refresh Token 生命周期很短

**影响**：
- 用户需要频繁重新登录
- 长时间会话无法维持
- API 调用会因 Token 过期而失败

### 2. **Refresh Token 过期**

**原因**：
- Refresh Token 有自己的生命周期（通常比 Access Token 长）
- Keycloak 默认配置：
  - Access Token: 5 分钟
  - Refresh Token: 30 分钟
  - SSO Session Idle: 30 分钟
  - SSO Session Max: 10 小时

**检查方式**：
```bash
# 在 Keycloak Admin Console 中检查
Realm Settings → Tokens → SSO Session Settings
  - SSO Session Idle
  - SSO Session Max
  - Client Session Idle
  - Client Session Max
```

### 3. **Refresh Token Rotation 启用**

**原因**：
- Keycloak 的 Refresh Token Rotation 功能启用后
- 每次刷新都会返回新的 Refresh Token
- 旧的 Refresh Token 立即失效
- 如果客户端使用了旧的 Token，就会报错

**检查方式**：
```bash
# 在 Keycloak Admin Console 中检查
Clients → verdure-mcp → Settings → Advanced Settings
  - "Revoke Refresh Token" (是否撤销刷新令牌)
  - "Refresh Token Max Reuse" (刷新令牌最大重用次数)
```

### 4. **Session 在 Keycloak 中已失效**

**原因**：
- 用户在 Keycloak 中手动退出
- Keycloak 管理员撤销了 Session
- SSO Session 超时

### 5. **时钟偏移问题**

**原因**：
- 客户端和 Keycloak 服务器的时钟不同步
- Token 的 `exp`（过期时间）和 `iat`（签发时间）验证失败

## ✅ 解决方案

### 方案 1: 添加 `offline_access` scope（推荐）⭐

**步骤 1: 修改前端配置**

在 `Program.cs` 中添加 `offline_access` scope：

```csharp
// Add OIDC Authentication with custom role mapping
builder.Services.AddOidcAuthentication(options =>
{
    // Load OIDC settings from configuration
    builder.Configuration.Bind("Oidc", options.ProviderOptions);

    if (string.IsNullOrEmpty(options.ProviderOptions.PostLogoutRedirectUri))
    {
        options.ProviderOptions.PostLogoutRedirectUri = apiBaseAddress;
    }
    
    // Configure for Keycloak
    options.ProviderOptions.ResponseType = "code";
    
    // Only add scopes if not already configured
    if (!options.ProviderOptions.DefaultScopes.Contains("openid"))
    {
        options.ProviderOptions.DefaultScopes.Add("openid");
        options.ProviderOptions.DefaultScopes.Add("profile");
        options.ProviderOptions.DefaultScopes.Add("email");
        // 🔑 添加 offline_access scope 以获取长期有效的 refresh token
        options.ProviderOptions.DefaultScopes.Add("offline_access");
    }
})
.AddAccountClaimsPrincipalFactory<KeycloakRoleClaimsPrincipalFactory>();
```

**步骤 2: 验证 Keycloak 客户端配置**

在 Keycloak Admin Console 中：

1. 进入 `Clients` → `verdure-mcp`
2. 检查 `Settings` 标签：
   - `Access Type`: `public` 或 `confidential`
   - `Standard Flow Enabled`: ✅ ON
   - `Direct Access Grants Enabled`: ✅ ON (可选)
   - `Valid Redirect URIs`: 配置正确的回调 URL

3. 检查 `Client Scopes` 标签：
   - `Assigned Default Client Scopes` 应包含 `offline_access`
   - 如果没有，点击 `Add` → 选择 `offline_access` → 点击 `Add selected`

4. 检查 `Advanced Settings` 标签（可选）：
   - `Access Token Lifespan`: 5 分钟（默认）
   - 根据需要调整

### 方案 2: 调整 Keycloak Token 生命周期

如果不想使用 `offline_access`，可以延长 Token 生命周期：

**Realm 级别配置**：
```bash
Realm Settings → Tokens → Timeout Settings
  - Access Token Lifespan: 15 分钟（默认 5 分钟）
  - Refresh Token Lifespan: 1 小时（默认 30 分钟）
  - SSO Session Idle: 1 小时（默认 30 分钟）
  - SSO Session Max: 12 小时（默认 10 小时）
```

**Client 级别配置**（覆盖 Realm 配置）：
```bash
Clients → verdure-mcp → Advanced Settings
  - Access Token Lifespan: 自定义时间
  - Client Session Idle: 自定义时间
  - Client Session Max: 自定义时间
```

### 方案 3: 禁用 Refresh Token Rotation（开发环境）

⚠️ **仅用于开发环境，生产环境不推荐**

```bash
Clients → verdure-mcp → Advanced Settings
  - Revoke Refresh Token: OFF
  - Refresh Token Max Reuse: 0（无限制）
```

### 方案 4: 改进前端错误处理

确保前端正确处理 Token 刷新失败：

```csharp
// CustomAuthorizationMessageHandler.cs
protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, 
    CancellationToken cancellationToken)
{
    try
    {
        var response = await base.SendAsync(request, cancellationToken);

        // 如果 401，可能是 Token 过期或刷新失败
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("❌ Unauthorized response - redirecting to login");
            // 强制重新登录
            _navigation.NavigateTo("authentication/login", forceLoad: true);
        }

        return response;
    }
    catch (AccessTokenNotAvailableException ex)
    {
        // Token 不可用（过期、刷新失败等）
        _logger.LogError(ex, "❌ Access token not available - redirecting to login");
        // 重定向到登录页
        ex.Redirect();
        throw;
    }
}
```

## 🧪 测试和验证

### 1. 检查 Scope 是否正确发送

在浏览器开发者工具中：

1. 打开 `Network` 标签
2. 过滤 `token` 或 `auth` 请求
3. 查看授权请求中的 `scope` 参数

**正确示例**：
```
scope=openid%20profile%20email%20offline_access
```

**错误示例**（缺少 offline_access）：
```
scope=openid%20profile%20email
```

### 2. 检查 Refresh Token 是否返回

在浏览器开发者工具的 `Application` → `Local Storage` 中：

查找类似以下的键：
```
oidc.user:<authority>:<clientId>
```

值应该包含：
```json
{
  "access_token": "...",
  "refresh_token": "...",  // 👈 应该存在
  "token_type": "Bearer",
  "expires_at": 1700000000
}
```

### 3. 测试 Token 刷新

**手动测试**：

```bash
# 使用 curl 测试 refresh token
curl -X POST https://auth.verdure-hiro.cn/realms/maker-community/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token" \
  -d "client_id=verdure-mcp" \
  -d "refresh_token=<YOUR_REFRESH_TOKEN>"
```

**成功响应**：
```json
{
  "access_token": "new-access-token",
  "refresh_token": "new-refresh-token",
  "token_type": "Bearer",
  "expires_in": 300
}
```

**失败响应**：
```json
{
  "error": "invalid_grant",
  "error_description": "Token is not active"
}
```

### 4. 模拟 Token 过期

在浏览器控制台中运行：

```javascript
// 修改 access_token 的过期时间
const key = Object.keys(localStorage).find(k => k.startsWith('oidc.user:'));
const data = JSON.parse(localStorage.getItem(key));
data.expires_at = Math.floor(Date.now() / 1000) - 100; // 设置为已过期
localStorage.setItem(key, JSON.stringify(data));
console.log('Token expired, refresh on next API call');
```

然后执行一个 API 调用，观察是否自动刷新。

## 📊 排查清单

- [ ] **确认 `offline_access` scope 已添加到前端配置**
- [ ] **检查 Keycloak Client Scopes 配置**
  - [ ] `offline_access` 在 `Assigned Default Client Scopes` 中
- [ ] **检查浏览器 Network 请求中的 scope 参数**
- [ ] **检查 Local Storage 中是否有 `refresh_token`**
- [ ] **测试 Refresh Token API 调用是否成功**
- [ ] **检查 Keycloak Token 生命周期配置**
- [ ] **确认前端正确处理 Token 刷新失败**
- [ ] **检查客户端和服务器时钟同步**

## 🔧 Keycloak 配置参考

### 推荐的 Token 配置（生产环境）

**Realm Settings → Tokens**:
```
Access Token Lifespan: 5 分钟
Refresh Token Lifespan: 30 分钟
SSO Session Idle: 30 分钟
SSO Session Max: 10 小时
```

**Clients → verdure-mcp → Advanced Settings**:
```
Access Token Lifespan: 使用 Realm 默认
Refresh Token Max Reuse: 0（启用 Rotation）
Revoke Refresh Token: ON（启用 Rotation）
```

### 开发环境配置（更宽松）

**Realm Settings → Tokens**:
```
Access Token Lifespan: 15 分钟
Refresh Token Lifespan: 2 小时
SSO Session Idle: 2 小时
SSO Session Max: 24 小时
```

**Clients → verdure-mcp → Advanced Settings**:
```
Revoke Refresh Token: OFF（禁用 Rotation，方便调试）
```

## 🔗 相关文档

- [Keycloak Token Settings](https://www.keycloak.org/docs/latest/server_admin/#_timeouts)
- [OAuth 2.0 Refresh Token](https://oauth.net/2/grant-types/refresh-token/)
- [OIDC offline_access scope](https://openid.net/specs/openid-connect-core-1_0.html#OfflineAccess)
- [Blazor OIDC Authentication](https://learn.microsoft.com/aspnet/core/blazor/security/webassembly/standalone-with-authentication-library)

## 📝 总结

**最常见原因**：缺少 `offline_access` scope

**快速修复**：
1. 在 `Program.cs` 中添加 `offline_access` scope
2. 确认 Keycloak 客户端 Scopes 配置正确
3. 清除浏览器缓存并重新登录
4. 验证 Refresh Token 是否返回

**预防措施**：
- ✅ 始终添加 `offline_access` scope
- ✅ 合理配置 Token 生命周期
- ✅ 正确处理 Token 刷新失败
- ✅ 监控 Token 相关错误日志
