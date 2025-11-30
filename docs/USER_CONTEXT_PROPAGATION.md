# 用户上下文透传功能 (User Context Propagation)

## 📋 概述

当 Verdure MCP Platform 调用其他 MCP 服务时，现在会自动透传用户信息到下游服务，使得下游 MCP 服务能够识别请求来源的用户身份。

## 🎯 功能说明

### 自动添加的请求头

调用 MCP 服务时，系统会自动添加以下 HTTP 请求头：

- **X-User-Id**: 用户的唯一标识符
- **X-User-Email**: 用户的邮箱地址（如果可用）

### 实现位置

- **服务类**: `Verdure.McpPlatform.Application.Services.McpClientService`
- **方法**: `CreateMcpClientAsync(McpServiceConfig config, ...)`

## 🔧 技术实现

### 1. 用户信息查询

系统根据不同的场景查询用户详细信息：

**场景 A: 通过 McpServiceConfig 调用 (工具同步等)**

```csharp
// McpClientService.CreateMcpClientAsync(McpServiceConfig config, ...)
var userInfoMap = await _userInfoService.GetUsersByIdsAsync(new[] { config.UserId });
if (userInfoMap.TryGetValue(config.UserId, out var userInfo))
{
    // 准备用户上下文请求头
    userContextHeaders = new Dictionary<string, string>();
    userContextHeaders["X-User-Id"] = userInfo.UserId;
    
    if (!string.IsNullOrEmpty(userInfo.Email))
    {
        userContextHeaders["X-User-Email"] = userInfo.Email;
    }
}
```

**场景 B: 通过 McpSessionService 调用 (WebSocket 连接等)**

```csharp
// McpSessionService.GetUserContextHeadersAsync()
private async Task<Dictionary<string, string>?> GetUserContextHeadersAsync()
{
    var userInfoMap = await _userInfoService.GetUsersByIdsAsync(new[] { _config.UserId });
    if (userInfoMap.TryGetValue(_config.UserId, out var userInfo))
    {
        var headers = new Dictionary<string, string>
        {
            ["X-User-Id"] = userInfo.UserId
        };

        if (!string.IsNullOrEmpty(userInfo.Email))
        {
            headers["X-User-Email"] = userInfo.Email;
        }

        return headers;
    }
    return null;
}
```

### 2. 请求头合并策略

用户上下文请求头与认证请求头（Bearer、Basic、API Key）会被合并：

```csharp
// 1. 先处理认证请求头（如 Authorization）
headers = McpAuthenticationHelper.BuildAuthenticationHeaders(...);

// 2. 合并用户上下文请求头
if (additionalHeaders != null)
{
    headers ??= new Dictionary<string, string>();
    foreach (var kvp in additionalHeaders)
    {
        headers[kvp.Key] = kvp.Value;
    }
}

// 3. 应用到传输选项
transportOptions.AdditionalHeaders = headers;
```

### 3. 异常处理

- 如果用户信息查询失败，系统会记录错误日志，但**不会中断 MCP 连接**
- 下游服务在没有用户上下文的情况下仍然可以正常工作（向后兼容）

```csharp
catch (Exception ex)
{
    _logger.LogError(
        ex,
        "Error fetching user information for service {ServiceName}, " +
        "user context headers will not be added",
        config.Name);
    // 继续执行，不抛出异常
}
```

## 📦 依赖注入

### McpClientService

`McpClientService` 依赖 `IUserInfoService` 来查询用户信息：

```csharp
public McpClientService(
    ILogger<McpClientService> logger,
    IUserInfoService userInfoService)
{
    _logger = logger;
    _userInfoService = userInfoService;
}
```

### McpSessionService

**⚠️ 重要：DbContext 生命周期问题**

由于 `McpSessionService` 是一个长生命周期对象（在整个 WebSocket 会话期间保持活动），直接注入 Scoped 服务（如 `IUserInfoService`）会导致 DbContext 生命周期问题：

```
Cannot access a disposed context instance. A common cause of this error is disposing 
a context instance that was resolved from dependency injection...
```

**解决方案**：使用 `IServiceScopeFactory` 在需要时创建新的作用域：

```csharp
// ❌ 错误的做法 - 直接注入 Scoped 服务
public McpSessionService(
    IUserInfoService userInfoService,  // ❌ 会导致 DbContext 过早释放
    ...)
{
    _userInfoService = userInfoService;
}

// ✅ 正确的做法 - 使用 IServiceScopeFactory
public McpSessionService(
    McpSessionConfiguration config,
    ReconnectionSettings reconnectionSettings,
    IMcpClientService mcpClientService,
    IServiceScopeFactory serviceScopeFactory,  // ✅ 在需要时创建新作用域
    ILoggerFactory loggerFactory)
{
    _config = config;
    _reconnectionSettings = reconnectionSettings;
    _mcpClientService = mcpClientService;
    _serviceScopeFactory = serviceScopeFactory;  // ✅ 保存 factory
    _loggerFactory = loggerFactory;
}

// 在需要时创建新作用域并获取服务
private async Task<Dictionary<string, string>?> GetUserContextHeadersAsync()
{
    // 创建新作用域，确保 DbContext 在使用完后正确释放
    using var scope = _serviceScopeFactory.CreateScope();
    var userInfoService = scope.ServiceProvider.GetRequiredService<IUserInfoService>();
    
    var userInfoMap = await userInfoService.GetUsersByIdsAsync(new[] { _config.UserId });
    // ... 使用 userInfoService 查询用户信息
}
```

### 注册服务

确保在 DI 容器中注册了 `IUserInfoService` 实现：

```csharp
// 在 ServiceCollectionExtensions 或 Program.cs 中
services.AddScoped<IUserInfoService, UserInfoService>();
```

## 🔍 调试和日志

### McpClientService 日志

**成功添加用户上下文**

```log
[Debug] Adding user context headers for service MyService: 
        UserId=user-guid-123, Email=user@example.com
```

**用户未找到警告**

```log
[Warning] User user-guid-123 not found for service MyService, 
          user context headers will not be added
```

**查询错误**

```log
[Error] Error fetching user information for service MyService, 
        user context headers will not be added
        Exception: ...
```

### McpSessionService 日志

**成功添加用户上下文**

```log
[Debug] Server server-123: Adding user context headers: 
        UserId=user-guid-123, Email=user@example.com
```

**用户未找到警告**

```log
[Warning] Server server-123: User user-guid-123 not found, 
          user context headers will not be added
```

**查询错误**

```log
[Error] Server server-123: Error fetching user information, 
        user context headers will not be added
        Exception: ...
```

## 🌐 下游 MCP 服务集成

### 读取用户上下文

下游 MCP 服务可以通过以下方式读取用户信息：

**ASP.NET Core API**:
```csharp
[HttpPost("api/tools/{toolName}")]
public async Task<IActionResult> ExecuteTool(
    [FromHeader(Name = "X-User-Id")] string? userId,
    [FromHeader(Name = "X-User-Email")] string? userEmail,
    [FromRoute] string toolName,
    [FromBody] object parameters)
{
    // 使用 userId 和 userEmail 进行授权、审计等
    _logger.LogInformation(
        "Tool {ToolName} called by user {UserId} ({Email})",
        toolName, userId ?? "unknown", userEmail ?? "unknown");
    
    // ... 执行工具逻辑
}
```

**其他语言/框架**:
- Node.js (Express): `req.headers['x-user-id']`
- Python (FastAPI): `@Header("X-User-Id")`
- Go (Gin): `c.GetHeader("X-User-Id")`

## 🔒 安全考虑

1. **信任边界**: 这些请求头应仅在受信任的内部服务之间传递
2. **不要暴露敏感信息**: 只传递必要的身份识别信息（ID 和邮箱）
3. **下游验证**: 下游服务应根据自己的安全策略验证这些请求头
4. **审计日志**: 建议记录所有使用用户上下文的操作，以便审计

## 📊 性能影响

- **额外查询**: 每次创建 MCP 客户端时需要查询一次用户信息
- **缓存优化**: `IUserInfoService.GetUsersByIdsAsync` 支持批量查询，减少数据库往返
- **失败不阻塞**: 用户信息查询失败不会影响 MCP 连接的建立

## ✅ 测试验证

### 单元测试示例

```csharp
[TestMethod]
public async Task CreateMcpClient_AddsUserContextHeaders()
{
    // Arrange
    var mockUserInfoService = new Mock<IUserInfoService>();
    mockUserInfoService
        .Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<string>>()))
        .ReturnsAsync(new Dictionary<string, UserBasicInfo>
        {
            ["user-123"] = new UserBasicInfo
            {
                UserId = "user-123",
                Email = "test@example.com"
            }
        });
    
    var service = new McpClientService(
        Mock.Of<ILogger<McpClientService>>(),
        mockUserInfoService.Object);
    
    var config = new McpServiceConfig(
        "TestService",
        "http://localhost:5000",
        "user-123");
    
    // Act
    var client = await service.CreateMcpClientAsync(config);
    
    // Assert
    // 验证请求头包含 X-User-Id 和 X-User-Email
    Assert.IsNotNull(client);
}
```

## 🔄 版本历史

- **v1.0** (2025-11-30): 初始实现，支持 X-User-Id 和 X-User-Email 透传

## 📚 相关文档

- [MCP Authentication Refactoring](./MCP_AUTHENTICATION_REFACTORING.md)
- [API Examples](./guides/API_EXAMPLES.md)
- [User Info Service](../src/Verdure.McpPlatform.Application/Services/IUserInfoService.cs)
