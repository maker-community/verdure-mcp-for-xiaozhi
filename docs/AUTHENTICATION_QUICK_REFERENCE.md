# 认证配置快速参考

## 🔐 认证流程概述

```
用户访问 → 检查认证 → Token有效? 
                           ↓ 是
                      访问资源
                           ↓ 否
                      重定向登录
```

## ⚙️ 配置位置

### 1. appsettings.json
```json
{
  "Oidc": {
    "Authority": "https://auth.verdure-hiro.cn/realms/maker-community",
    "ClientId": "verdure-mcp",
    "ResponseType": "code",
    "PostLogoutRedirectUri": ""
  }
}
```

**注意**: 
- ❌ 不要配置 `Scope` 或 `DefaultScopes` 字段（会导致重复）
- ✅ Scope 在 Program.cs 中统一配置

### 2. Program.cs
```csharp
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Oidc", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "code";
    
    // ✅ 防止重复添加 scope
    if (!options.ProviderOptions.DefaultScopes.Contains("openid"))
    {
        options.ProviderOptions.DefaultScopes.Add("openid");
        options.ProviderOptions.DefaultScopes.Add("profile");
        options.ProviderOptions.DefaultScopes.Add("email");
        // 🔑 添加 offline_access 以获取 refresh token
        options.ProviderOptions.DefaultScopes.Add("offline_access");
    }
})
.AddAccountClaimsPrincipalFactory<KeycloakRoleClaimsPrincipalFactory>();
```

### 3. CustomAuthorizationMessageHandler
```csharp
protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request, 
    CancellationToken cancellationToken)
{
    try
    {
        var response = await base.SendAsync(request, cancellationToken);

        // ✅ 处理 401 错误
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _navigation.NavigateTo("authentication/login", forceLoad: true);
        }

        return response;
    }
    catch (AccessTokenNotAvailableException ex)
    {
        // ✅ 处理 Token 不可用
        ex.Redirect();
        throw;
    }
}
```

## 🎯 关键点

### ✅ DO（正确做法）
1. **统一配置 Scope**: 只在 Program.cs 中配置一次
2. **检查重复**: 添加前检查 `DefaultScopes.Contains("openid")`
3. **处理 401**: 在 `CustomAuthorizationMessageHandler` 中拦截
4. **捕获异常**: 处理 `AccessTokenNotAvailableException`
5. **显示状态**: 使用 `<Authorizing>` 显示认证中状态

### ❌ DON'T（错误做法）
1. **多处配置 Scope**: appsettings.json 和 Program.cs 都配置
2. **忽略 401**: 不处理认证失败的响应
3. **不显示状态**: 认证检查时显示空白页面
4. **硬编码重定向**: 使用固定路径而不是 `ex.Redirect()`

## 🔍 常见问题排查

### 问题 1: Scope 重复
**现象**: 网络请求中看到 `scope=openid%20profile%20email%20openid%20profile%20email`

**排查步骤**:
1. 检查 `appsettings.json` 是否有 `Scope` 或 `DefaultScopes` 字段
2. 检查 `Program.cs` 是否无条件添加 scope
3. 使用浏览器开发者工具查看认证请求

**解决方案**: 移除 appsettings.json 中的配置，在 Program.cs 中添加检查

### 问题 2: Token 过期后报错
**现象**: 401 错误直接显示给用户，未重定向到登录页

**排查步骤**:
1. 检查 `CustomAuthorizationMessageHandler` 是否重写了 `SendAsync`
2. 查看浏览器控制台是否有 401 错误
3. 检查是否捕获了 `AccessTokenNotAvailableException`

**解决方案**: 在 `SendAsync` 中添加异常处理和重定向逻辑

### 问题 3: 认证检查时白屏
**现象**: 访问受保护页面时短暂白屏

**排查步骤**:
1. 检查 `App.razor` 是否有 `<Authorizing>` 模板
2. 查看页面加载时的状态

**解决方案**: 添加 `<Authorizing>` 模板显示加载状态

## 📊 测试清单

- [ ] Scope 不重复（查看网络请求）
- [ ] Token 过期后自动重定向
- [ ] 401 错误被正确拦截
- [ ] 认证中显示加载提示
- [ ] 登录成功后返回原页面
- [ ] 未授权显示友好提示

## 🔗 相关文档

- [完整修复文档](AUTHENTICATION_FIX.md)
- [Blazor OIDC 认证](https://learn.microsoft.com/aspnet/core/blazor/security/webassembly/standalone-with-authentication-library)
- [AuthorizationMessageHandler](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.components.webassembly.authentication.authorizationmessagehandler)
