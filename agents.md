# AGENTS.md

## 项目概述

这是一个基于 .NET 9 的多用户 MCP (Model Context Protocol) 服务管理平台。

**核心功能**：
- 多用户登录系统
- 每个用户可配置自己的小智 AI 服务器地址
- 将不同的 MCP 服务绑定到指定节点
- 通过 Socket 连接提供对应的 MCP 服务

**架构模式**：前后端分离

---

## 技术栈

### 后端
- .NET 9
- ASP.NET Core Web API
- Entity Framework Core (自动迁移)
- SQLite (默认数据库，可扩展到其他数据库)

### 前端
- Blazor WebAssembly
- MudBlazor UI 组件库

### 服务编排
- .NET Aspire

### 核心 NuGet 包
```xml
<PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.3" />
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.3.0-preview.3" />
```

---

## 参考项目

在生成代码时，请严格参考以下项目的设计模式和代码风格：

### 1. 核心 MCP 实现参考
**仓库**: https://github.com/maker-community/mcp-calculator/tree/dev_csharp/csharp
- 这是项目的核心依据
- 参考其 MCP 协议的实现方式
- 学习其 Socket 连接和服务绑定逻辑

### 2. 项目结构参考
**仓库**: https://github.com/GreenShadeZhang/agent-framework-tutorial-code/tree/main/agent-groupchat
- 参考其前后端分离的项目结构
- 学习 Blazor WebAssembly 与 ASP.NET Core API 的集成方式
- 参考其 Aspire 服务编排配置

### 3. 代码风格参考（最高优先级）
**仓库**: https://github.com/microsoft/agent-framework/tree/main/dotnet
- **必须严格遵循此仓库的 .NET 代码风格**
- 命名约定、文件组织、注释风格等都要保持一致
- 使用相同的设计模式和最佳实践

---

## 项目结构要求

请按照以下结构组织代码：

```
/
├── src/
│   ├── McpService.AppHost/          # Aspire 应用宿主
│   ├── McpService.Api/              # 后端 API 项目
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Models/
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── Migrations/
│   │   └── Program.cs
│   ├── McpService.Web/              # Blazor WebAssembly 前端
│   │   ├── Pages/
│   │   ├── Components/
│   │   ├── Services/
│   │   └── Program.cs
│   ├── McpService.Shared/           # 共享模型和 DTO
│   │   ├── Models/
│   │   └── DTOs/
│   └── McpService.Core/             # 核心业务逻辑
│       ├── Interfaces/
│       └── Services/
├── tests/
│   ├── McpService.Api.Tests/
│   └── McpService.Core.Tests/
├── McpService.sln
└── AGENTS.md
```

---

## 开发指南

### 数据库自动迁移

**重要**：由于开发者不擅长 SQL，Entity Framework Core 必须配置为自动迁移。

在 `Program.cs` 中添加：
```csharp
// 应用启动时自动执行迁移
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}
```

### 数据模型要求

需要至少以下实体：

1. **User** (用户)
   - Id, Username, PasswordHash, Email, CreatedAt

2. **McpServerConfig** (MCP 服务器配置)
   - Id, UserId, ServerAddress, ServerName, Description, CreatedAt

3. **McpServiceBinding** (MCP 服务绑定)
   - Id, UserId, McpServerConfigId, ServiceName, NodeAddress, IsActive

### API 端点设计

**用户管理**：
- POST `/api/auth/register` - 用户注册
- POST `/api/auth/login` - 用户登录
- GET `/api/auth/me` - 获取当前用户信息

**MCP 服务器配置**：
- GET `/api/mcp-servers` - 获取当前用户的所有服务器配置
- POST `/api/mcp-servers` - 创建新的服务器配置
- PUT `/api/mcp-servers/{id}` - 更新服务器配置
- DELETE `/api/mcp-servers/{id}` - 删除服务器配置

**MCP 服务绑定**：
- GET `/api/mcp-bindings` - 获取当前用户的所有服务绑定
- POST `/api/mcp-bindings` - 创建新的服务绑定
- PUT `/api/mcp-bindings/{id}` - 更新服务绑定
- DELETE `/api/mcp-bindings/{id}` - 删除服务绑定

**Socket 连接**：
- WebSocket `/ws/mcp/{bindingId}` - 建立 MCP Socket 连接

---

## 代码风格规范

### 命名约定
- 类名：PascalCase (`UserService`, `McpServerConfig`)
- 接口：以 `I` 开头 (`IUserService`, `IMcpClient`)
- 私有字段：以 `_` 开头的 camelCase (`_userService`, `_dbContext`)
- 公共属性：PascalCase (`UserId`, `ServerAddress`)
- 方法：PascalCase (`GetUserByIdAsync`, `CreateServerConfigAsync`)

### 异步编程
- 所有 I/O 操作必须使用异步方法
- 异步方法以 `Async` 结尾
- 使用 `async/await` 而不是 `.Result` 或 `.Wait()`

### 依赖注入
- 使用构造函数注入
- 通过接口而不是具体类进行依赖

```csharp
public class UserService : IUserService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UserService> _logger;

    public UserService(ApplicationDbContext dbContext, ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
}
```

### 错误处理
- 使用全局异常处理中间件
- 返回标准化的错误响应
- 记录详细的错误日志

### 日志记录
- 使用 `ILogger<T>`
- 关键操作必须记录日志（登录、配置变更、错误等）

---

## Blazor 前端开发指南

### UI 组件库
使用 MudBlazor，遵循其最佳实践：
- 使用 `MudThemeProvider` 进行主题配置
- 使用 `MudDialog` 进行模态对话框
- 使用 `MudTable` 显示数据列表
- 使用 `MudForm` 进行表单验证

### 状态管理
- 使用服务进行跨组件状态共享
- 使用 `HttpClient` 与后端 API 通信
- 实现基于 JWT 的身份验证

### 页面结构
```
Pages/
├── Index.razor              # 首页/仪表板
├── Login.razor              # 登录页面
├── Register.razor           # 注册页面
├── McpServers.razor         # MCP 服务器配置管理
└── McpBindings.razor        # MCP 服务绑定管理
```

---

## Aspire 配置

在 `AppHost` 项目中配置服务编排：

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.McpService_Api>("api");

builder.AddProject<Projects.McpService_Web>("web")
    .WithReference(api);

builder.Build().Run();
```

---

## 测试要求

### 单元测试
- 为所有服务类编写单元测试
- 使用 xUnit
- 使用 Moq 进行模拟
- 测试覆盖率目标：>80%

### 集成测试
- 测试 API 端点
- 使用 WebApplicationFactory
- 使用内存数据库（SQLite）

---

## 安全要求

1. **身份验证**：使用 JWT Token
2. **密码存储**：使用 BCrypt 或 ASP.NET Core Identity 的密码哈希
3. **CORS**：仅允许前端应用域名
4. **输入验证**：所有用户输入必须验证
5. **SQL 注入防护**：使用 EF Core 参数化查询（自动处理）

---

## 构建和运行命令

### 开发环境
```bash
# 恢复依赖
dotnet restore

# 运行所有服务（通过 Aspire）
dotnet run --project src/McpService.AppHost

# 单独运行后端 API
dotnet run --project src/McpService.Api

# 单独运行前端
dotnet run --project src/McpService.Web

# 运行测试
dotnet test
```

### 数据库迁移（如果需要手动操作）
```bash
# 添加迁移
dotnet ef migrations add InitialCreate --project src/McpService.Api

# 应用迁移
dotnet ef database update --project src/McpService.Api
```

### 生产构建
```bash
# 发布 API
dotnet publish src/McpService.Api -c Release -o publish/api

# 发布前端
dotnet publish src/McpService.Web -c Release -o publish/web
```

---

## 常见任务示例

### 添加新的 API 端点
1. 在 `Models/` 中定义实体
2. 在 `Data/ApplicationDbContext.cs` 中添加 DbSet
3. 创建迁移（或依赖自动迁移）
4. 在 `Services/` 中实现业务逻辑
5. 在 `Controllers/` 中创建控制器
6. 编写单元测试

### 添加新的前端页面
1. 在 `Pages/` 中创建 `.razor` 文件
2. 在 `Services/` 中创建 API 客户端服务
3. 使用 MudBlazor 组件构建 UI
4. 在导航菜单中添加链接

---

## 注意事项

### ⚠️ 重要提醒
1. **代码风格**：必须严格遵循 microsoft/agent-framework 的 .NET 代码风格
2. **自动迁移**：EF Core 必须配置为自动迁移，避免手动 SQL 操作
3. **异步优先**：所有 I/O 操作必须使用异步方法
4. **依赖注入**：使用 ASP.NET Core 的内置 DI 容器
5. **日志记录**：关键操作必须记录日志

### 数据库扩展性
虽然默认使用 SQLite，但代码应该设计为可以轻松切换到其他数据库（PostgreSQL、SQL Server 等），通过连接字符串和 EF Core Provider 配置。

---

## 版本信息

- **.NET**: 9.0
- **ModelContextProtocol**: 0.3.0-preview.3
- **Entity Framework Core**: 9.0.x
- **MudBlazor**: 最新稳定版
- **.NET Aspire**: 9.0.x

---

## 资源链接

- [.NET 9 文档](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9)
- [MudBlazor 文档](https://mudblazor.com/)
- [.NET Aspire 文档](https://learn.microsoft.com/dotnet/aspire/)
- [EF Core 文档](https://learn.microsoft.com/ef/core/)
- [ModelContextProtocol GitHub](https://github.com/modelcontextprotocol)

---

## AI Agent 提示

当使用此文档时：
- 生成的代码必须符合上述所有规范
- 优先参考 microsoft/agent-framework 的代码风格
- 实现功能时考虑可扩展性和可维护性
- 生成完整的文件，不要省略代码
- 包含必要的注释和 XML 文档
- 遵循 .NET 最佳实践和设计模式
