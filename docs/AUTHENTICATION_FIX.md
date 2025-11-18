# 认证配置修复总结

## 修复日期
2025-11-18

## 问题描述

### 问题 1: Scope 重复配置
在 Blazor WebAssembly 前端的认证配置中，OpenID Connect 的 scope（`openid profile email`）被重复配置了多次，导致最终请求中出现类似 `"openid profile email openid profile email"` 的重复内容。

**重复配置的位置：**
1. `appsettings.json` 中的 `Oidc.Scope` 字符串字段
2. `appsettings.json` 中的 `Oidc.DefaultScopes` 数组
3. `Program.cs` 中手动调用 `options.ProviderOptions.DefaultScopes.Add()` 添加的 scope

### 问题 2: 认证过期后未重定向
当用户的认证 token 过期后，应用没有自动重定向到登录页，而是直接报错，用户体验不佳。

**具体表现：**
- API 请求返回 401 Unauthorized 时，前端没有处理
- `AccessTokenNotAvailableException` 异常未被捕获和处理
- 用户看到错误提示而不是被引导重新登录

---

## 修复方案

### 修复 1: 清理 Scope 重复配置

#### 1.1 清理 appsettings.json
**文件**: 
- `src/Verdure.McpPlatform.Web/wwwroot/appsettings.json`
- `docker/config/appsettings.json` (Docker 部署配置)

**修改前：**
```json
{
  "Oidc": {
    "Authority": "https://auth.verdure-hiro.cn/realms/maker-community",
    "ClientId": "verdure-mcp",
    "ResponseType": "code",
    "PostLogoutRedirectUri": "",
    "Scope": "openid profile email",        // ❌ 重复
    "DefaultScopes": [                       // ❌ 重复
      "openid",
      "profile",
      "email"
    ]
  }
}
```

**修改后：**
```json
{
  "Oidc": {
    "Authority": "https://auth.verdure-hiro.cn/realms/maker-community",
    "ClientId": "verdure-mcp",
    "ResponseType": "code",
    "PostLogoutRedirectUri": ""
    // ✅ 移除了 Scope 和 DefaultScopes 字段
  }
}
```

#### 1.2 优化 Program.cs 中的 Scope 配置
**文件**: `src/Verdure.McpPlatform.Web/Program.cs`

**修改前：**
```csharp
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Oidc", options.ProviderOptions);
    
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.DefaultScopes.Add("openid");    // ❌ 无条件添加
    options.ProviderOptions.DefaultScopes.Add("profile");
    options.ProviderOptions.DefaultScopes.Add("email");
})
```

**修改后：**
```csharp
builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Oidc", options.ProviderOptions);
    
    options.ProviderOptions.ResponseType = "code";
    
    // ✅ 只在未配置时才添加，防止重复
    if (!options.ProviderOptions.DefaultScopes.Contains("openid"))
    {
        options.ProviderOptions.DefaultScopes.Add("openid");
        options.ProviderOptions.DefaultScopes.Add("profile");
        options.ProviderOptions.DefaultScopes.Add("email");
    }
})
```

**说明：**
- 通过检查 `DefaultScopes` 中是否已包含 `openid`，避免重复添加
- 保持向后兼容性，如果将来需要在配置文件中配置 scope 也能正常工作

---

### 修复 2: 添加认证过期自动重定向

#### 2.1 增强 CustomAuthorizationMessageHandler
**文件**: `src/Verdure.McpPlatform.Web/Services/CustomAuthorizationMessageHandler.cs`

**修改内容：**
```csharp
public class CustomAuthorizationMessageHandler : AuthorizationMessageHandler
{
    private readonly NavigationManager _navigation;

    public CustomAuthorizationMessageHandler(
        IAccessTokenProvider provider,
        NavigationManager navigation,
        IConfiguration configuration)
        : base(provider, navigation)
    {
        _navigation = navigation;
        // ... 原有初始化代码
    }

    // ✅ 新增：重写 SendAsync 方法处理认证错误
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            // 如果收到 401 Unauthorized，重定向到登录页
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _navigation.NavigateTo("authentication/login", forceLoad: true);
            }

            return response;
        }
        catch (AccessTokenNotAvailableException ex)
        {
            // Token 不可用（过期或缺失）时，自动重定向到登录页
            ex.Redirect();
            throw;
        }
    }
}
```

**功能说明：**
1. **401 错误拦截**: 当 API 返回 401 状态码时，自动重定向到登录页
2. **AccessTokenNotAvailableException 处理**: 捕获 token 不可用异常并重定向
3. **forceLoad**: 使用 `forceLoad: true` 确保完全重新加载页面，清理旧的认证状态

#### 2.2 添加 Authorizing 状态显示
**文件**: `src/Verdure.McpPlatform.Web/App.razor`

**新增内容：**
```razor
<AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(LandingLayout)">
    <NotAuthorized>
        <!-- 原有的未授权提示 -->
    </NotAuthorized>
    
    <!-- ✅ 新增：认证中状态显示 -->
    <Authorizing>
        <MudContainer MaxWidth="MaxWidth.Medium" Style="margin-top: 120px;">
            <div class="d-flex flex-column align-center justify-center">
                <MudProgressCircular Color="Color.Primary" Size="Size.Large" Indeterminate="true" />
                <MudText Typo="Typo.body1" Class="mt-4">Checking authentication...</MudText>
            </div>
        </MudContainer>
    </Authorizing>
</AuthorizeRouteView>
```

**说明：**
- 在认证状态检查期间显示友好的加载提示
- 避免用户看到空白页面或闪烁

---

## 技术原理

### Blazor WebAssembly OIDC 认证流程

```
┌─────────────────────────────────────────────────────────────────┐
│                    用户访问受保护页面                              │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
                    ┌────────────────┐
                    │ 检查认证状态    │
                    └────────┬───────┘
                             │
                ┌────────────┴────────────┐
                │                         │
                ▼                         ▼
         ┌──────────┐              ┌──────────┐
         │ 已认证   │              │ 未认证   │
         └────┬─────┘              └────┬─────┘
              │                         │
              ▼                         ▼
    ┌─────────────────┐       ┌─────────────────┐
    │ 检查 Token 有效性│       │ 重定向到登录页   │
    └────────┬────────┘       └─────────────────┘
             │
    ┌────────┴────────┐
    │                 │
    ▼                 ▼
┌────────┐      ┌────────────┐
│ 有效   │      │ 已过期      │
└───┬────┘      └────┬───────┘
    │                │
    ▼                ▼
┌────────┐    ┌──────────────┐
│ 正常访问│    │ 401 Unauthorized│
└────────┘    └────┬─────────┘
                   │
                   ▼
        ┌──────────────────────┐
        │ CustomAuthorizationMessageHandler │
        │ 拦截 401 并重定向到登录页           │
        └──────────────────────┘
```

### AccessTokenNotAvailableException 机制

`AccessTokenNotAvailableException` 是 Blazor WebAssembly OIDC 库提供的特殊异常，用于处理 token 不可用的情况。

**关键方法：**
- `ex.Redirect()`: 自动重定向到认证端点进行重新认证
- 异常会携带原始请求的 URL，认证成功后可以返回到原页面

**最佳实践：**
```csharp
try
{
    var response = await base.SendAsync(request, cancellationToken);
    return response;
}
catch (AccessTokenNotAvailableException ex)
{
    // 调用 Redirect() 方法处理重定向
    ex.Redirect();
    // 重新抛出异常，让上层知道请求失败
    throw;
}
```

---

## 测试验证

### 测试场景 1: Scope 不再重复

**测试步骤：**
1. 启动应用并打开浏览器开发者工具
2. 进入 Network 标签
3. 触发认证流程
4. 查看 OIDC 认证请求的 `scope` 参数

**预期结果：**
```
scope=openid%20profile%20email
```

**而不是：**
```
scope=openid%20profile%20email%20openid%20profile%20email
```

### 测试场景 2: Token 过期后自动重定向

**测试步骤：**
1. 登录应用
2. 等待 token 过期（或手动清除浏览器中的认证 token）
3. 尝试访问受保护的 API 端点或页面

**预期结果：**
- 应用自动重定向到 `/authentication/login`
- 显示 "Signing In..." 加载提示
- 完成认证后返回到原页面

**而不是：**
- 显示 HTTP 401 错误
- 页面崩溃或白屏
- 需要手动刷新页面

### 测试场景 3: 认证中状态显示

**测试步骤：**
1. 清除浏览器缓存
2. 访问需要认证的页面
3. 观察认证检查期间的 UI 状态

**预期结果：**
- 显示 "Checking authentication..." 加载提示和进度圈
- 过渡流畅，无空白页面或闪烁

---

## 影响范围

### 修改的文件
1. `src/Verdure.McpPlatform.Web/wwwroot/appsettings.json`
2. `src/Verdure.McpPlatform.Web/Program.cs`
3. `src/Verdure.McpPlatform.Web/Services/CustomAuthorizationMessageHandler.cs`
4. `src/Verdure.McpPlatform.Web/App.razor`
5. `docker/config/appsettings.json` (Docker 部署配置)

### 向后兼容性
✅ **完全向后兼容**

- 现有用户不受影响
- 不需要修改 Keycloak 配置
- 不需要数据库迁移
- 不影响其他功能模块

### 依赖变更
无新增依赖包，只使用了现有的 API：
- `Microsoft.AspNetCore.Components.WebAssembly.Authentication`
- `System.Net.HttpStatusCode`

---

## 最佳实践建议

### 1. Scope 配置原则
- **统一配置位置**: 只在一个地方配置 scope（推荐在代码中配置）
- **避免重复**: 添加前检查是否已存在
- **最小权限原则**: 只请求必要的 scope

### 2. 认证错误处理
- **全局拦截**: 在 `AuthorizationMessageHandler` 中统一处理
- **用户友好**: 显示加载状态而不是错误信息
- **自动恢复**: 尽可能自动重定向而不是让用户手动操作

### 3. 认证状态 UI
- **Authorizing 状态**: 总是提供认证中的加载提示
- **NotAuthorized 状态**: 清楚区分"未登录"和"权限不足"
- **一致性**: 所有认证相关的 UI 风格保持一致

---

## 相关文档

- [Blazor WebAssembly OIDC 认证](https://learn.microsoft.com/aspnet/core/blazor/security/webassembly/standalone-with-authentication-library)
- [AuthorizationMessageHandler](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.components.webassembly.authentication.authorizationmessagehandler)
- [AccessTokenNotAvailableException](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.components.webassembly.authentication.accesstokennotavailableexception)

---

## 总结

通过这次修复：

✅ **解决了 Scope 重复问题**：清理了三处重复配置，确保请求中只包含一次 scope

✅ **改善了用户体验**：认证过期时自动重定向到登录页，而不是显示错误

✅ **增强了健壮性**：添加了完整的认证错误处理机制，提高了应用的容错能力

✅ **保持了兼容性**：所有修改都是向后兼容的，不影响现有功能

这些改进使认证流程更加可靠和用户友好，是生产环境中必不可少的优化。
