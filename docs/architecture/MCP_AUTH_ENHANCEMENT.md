# MCP 认证配置完善文档

## 更改概述

基于对 MCP Inspector 和 C# SDK 的深入分析,本次更新对 Verdure MCP Platform 的认证配置进行了全面完善,提供了结构化的认证输入界面,消除了繁琐的 JSON 手动配置。

## 核心变更

### 1. **新增结构化认证配置模型** (`Verdure.McpPlatform.Contracts/Models/AuthenticationConfig.cs`)

创建了以下认证配置类:

#### BearerTokenAuthConfig (Bearer Token 认证)
- `Token`: Bearer token 值
- `HeaderName`: 自定义 header 名称 (可选,默认 Authorization)

#### BasicAuthConfig (基本认证)
- `Username`: 用户名
- `Password`: 密码

#### ApiKeyAuthConfig (API Key 认证)
- `ApiKey`: API 密钥
- `HeaderName`: Header 名称 (如 X-API-Key)
- `Prefix`: 可选前缀 (如 "ApiKey ")

#### OAuth2AuthConfig (OAuth 2.0 认证)
完整的 OAuth 2.0 流程配置:
- `ClientId`: OAuth 客户端 ID
- `ClientSecret`: OAuth 客户端密钥 (PKCE 时可选)
- `AuthorizationEndpoint`: 授权端点 URL
- `TokenEndpoint`: Token 端点 URL
- `RedirectUri`: 回调 URL
- `Scope`: OAuth 范围 (空格分隔)
- `GrantType`: 授权类型 (authorization_code, client_credentials 等)
- `UsePkce`: 是否使用 PKCE
- `AccessToken`: 访问令牌 (认证后存储)
- `RefreshToken`: 刷新令牌 (认证后存储)
- `TokenExpiresAt`: Token 过期时间

### 2. **新增认证配置表单组件**

#### BearerTokenConfigForm.razor
- 支持 Token 输入
- 可切换显示/隐藏密码
- 可自定义 Header 名称
- 提供使用示例

#### BasicAuthConfigForm.razor
- 用户名和密码输入
- 密码可见性切换
- 显示 Base64 编码说明

#### ApiKeyConfigForm.razor
- API Key 输入
- Header 名称配置
- 可选前缀配置
- 实时预览 Header 格式

#### OAuth2ConfigForm.razor
完整的 OAuth 2.0 配置界面:
- **客户端配置**: Client ID、Client Secret
- **端点配置**: Authorization Endpoint、Token Endpoint、Redirect URI
- **流程配置**: Grant Type、Scope、PKCE 开关
- **Token 信息显示**: 已认证时显示 Access Token 和过期时间
- 智能过期提醒 (5分钟内为红色,30分钟内为黄色)

### 3. **更新 McpServiceConfigEdit 页面**

#### Protocol 选择简化
- **移除**: stdio、WebSocket
- **保留**: SSE (Server-Sent Events)
- **重命名**: HTTP → StreamableHttp

#### 认证配置界面优化
- 使用 MudSelect 选择认证类型
- 动态加载对应的配置表单组件
- 使用 MudPaper 将配置表单包裹以突出显示
- 自动序列化/反序列化 JSON 配置

### 4. **数据流更新**

#### 保存流程
```csharp
用户填写表单 → 根据认证类型选择对应配置对象 → 
序列化为 JSON → 存储到 AuthenticationConfig 字段
```

#### 加载流程
```csharp
从数据库读取 → 根据 AuthenticationType 判断类型 → 
反序列化 JSON 到对应配置对象 → 填充表单
```

## 技术亮点

### 1. **类型安全的多态序列化**
```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BearerTokenAuthConfig), "bearer")]
[JsonDerivedType(typeof(BasicAuthConfig), "basic")]
[JsonDerivedType(typeof(ApiKeyAuthConfig), "apikey")]
[JsonDerivedType(typeof(OAuth2AuthConfig), "oauth2")]
public abstract class AuthenticationConfig
```

### 2. **智能表单切换**
```razor
@switch (_model.AuthenticationType)
{
    case "bearer":
        <BearerTokenConfigForm @bind-Config="_model.BearerTokenConfig" />
        break;
    // ... 其他认证类型
}
```

### 3. **安全的密码输入**
- 所有敏感字段 (Token、Password、Client Secret) 默认隐藏
- 提供眼睛图标切换可见性
- InputType 动态切换 (Password/Text)

### 4. **用户友好的提示**
- 每个字段都有 HelperText 说明
- MudAlert 显示使用示例
- OAuth Token 过期时间颜色提醒

## 参考实现

### MCP Inspector (TypeScript)
- **OAuth 流程**: 参考了完整的 OAuth 2.0 授权码流程实现
- **自定义 Headers**: 学习了灵活的 Header 配置方式
- **Token 管理**: 借鉴了 Session Storage 的 Token 存储策略

### C# SDK
- **ClientOAuthProvider**: 参考了 OAuth 提供者的配置选项设计
- **ClientOAuthOptions**: 学习了 RedirectUri、Scopes、GrantType 等配置项
- **DynamicClientRegistration**: 了解了动态客户端注册的选项

## 兼容性

### 向后兼容
- 保留 `AuthenticationConfig` 字段存储 JSON
- 旧的 JSON 配置会在加载时尝试反序列化
- 反序列化失败时使用默认值,不会导致错误

### 数据库结构
无需更改现有数据库结构:
- `AuthenticationType` (string): 认证类型标识
- `AuthenticationConfig` (string): JSON 格式的配置数据
- `Protocol` (string): 传输协议 (sse 或 streamable-http)

## 使用指南

### 1. Bearer Token 认证
适用于简单的 Token 认证:
1. 选择 "Bearer Token"
2. 输入 Token
3. 可选:自定义 Header 名称 (默认 Authorization)

### 2. Basic Auth 认证
适用于用户名密码认证:
1. 选择 "Basic Auth"
2. 输入用户名
3. 输入密码

### 3. API Key 认证
适用于 API 密钥认证:
1. 选择 "API Key"
2. 设置 Header 名称 (如 X-API-Key)
3. 输入 API Key
4. 可选:设置前缀 (如 "ApiKey ")

### 4. OAuth 2.0 认证
适用于复杂的 OAuth 场景:
1. 选择 "OAuth 2.0"
2. **客户端配置**: 填写 Client ID (和可选的 Client Secret)
3. **端点配置**: 
   - Authorization Endpoint (授权页面 URL)
   - Token Endpoint (获取 token 的 URL)
   - Redirect URI (OAuth 回调地址)
4. **流程配置**:
   - 选择 Grant Type
   - 输入 Scope (空格分隔)
   - 开启 PKCE (推荐)
5. 认证成功后会自动显示 Token 信息

## 国际化支持

新增所有认证相关的本地化键值:
- BearerToken, BearerTokenHelp, BearerTokenExample
- Username, Password, BasicAuthExample
- ApiKey, ApiKeyHeaderName, ApiKeyPrefix, ApiKeyExample
- OAuth2ClientId, OAuth2ClientSecret, OAuth2AuthorizationEndpoint, etc.
- OAuth2ConfigurationGuide

## 测试建议

### 1. Bearer Token
```bash
curl -H "Authorization: Bearer your-token" https://api.example.com
```

### 2. Basic Auth
```bash
curl -u username:password https://api.example.com
```

### 3. API Key
```bash
curl -H "X-API-Key: your-api-key" https://api.example.com
```

### 4. OAuth 2.0
需要完整的 OAuth 流程测试,建议使用 Postman 或专门的 OAuth 测试工具。

## 下一步

### 短期
1. 添加本地化资源文件 (中文和英文)
2. 完善输入验证 (URL 格式、必填字段等)
3. 添加 OAuth Token 刷新功能

### 长期
1. 实现实际的 OAuth 流程 (浏览器授权)
2. 支持更多认证方式 (JWT、SAML 等)
3. 添加认证配置模板 (常见服务的预设)
4. 提供认证测试功能 (测试连接按钮)

## 总结

本次更新极大地改善了 MCP 认证配置的用户体验:
- ✅ 消除了手动编写 JSON 的复杂性
- ✅ 提供了类型安全的配置管理
- ✅ 支持所有主流认证方式
- ✅ 特别完善了 OAuth 2.0 的配置界面
- ✅ 移除了不需要的协议选项
- ✅ 重命名 HTTP 为 StreamableHttp 更加语义化

用户现在可以通过直观的表单轻松配置各种认证方式,无需理解复杂的 JSON 结构。
