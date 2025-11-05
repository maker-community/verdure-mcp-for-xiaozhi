# AGENTS.md

A dedicated guide for AI coding agents working on the Verdure MCP Platform project.

**This file follows the [agents.md](https://agents.md/) specification - a standard format for providing AI coding agents with project-specific context, conventions, and instructions.**

---

## 🎯 Quick Start for Agents

### Essential Commands
```powershell
# Restore dependencies
dotnet restore

# Build entire solution
dotnet build

# Run with Aspire orchestration (recommended)
dotnet run --project src/Verdure.McpPlatform.AppHost

# Run API only
dotnet run --project src/Verdure.McpPlatform.Api

# Run Web frontend only
dotnet run --project src/Verdure.McpPlatform.Web

# Run all tests
dotnet test

# Database migrations (EF Core)
dotnet ef migrations add <MigrationName> --project src/Verdure.McpPlatform.Infrastructure --startup-project src/Verdure.McpPlatform.Api
dotnet ef database update --project src/Verdure.McpPlatform.Infrastructure --startup-project src/Verdure.McpPlatform.Api
```

### Testing Commands
```powershell
# Unit tests only
dotnet test tests/Verdure.McpPlatform.UnitTests

# Functional tests only
dotnet test tests/Verdure.McpPlatform.FunctionalTests

# Run with coverage
dotnet test /p:CollectCoverage=true

# Watch mode during development
dotnet watch test --project tests/Verdure.McpPlatform.UnitTests
```

### Verification Script
```powershell
# Verify complete setup
.\scripts\verify-setup.ps1

# Start development environment
.\scripts\start-dev.ps1
```

---

## 📖 项目概述 (Project Overview)

**Verdure MCP Platform** 是一个基于 .NET 9 的企业级多租户 SaaS 平台，为小智 AI 助手提供完整的 Model Context Protocol (MCP) 服务管理解决方案。

### 核心功能 (Core Features)

1. **多租户身份认证** - 基于 Keycloak OpenID Connect
2. **小智连接管理** - 配置小智 AI 服务器的 WebSocket 端点地址
3. **MCP 服务配置** - 管理各类 MCP 服务节点及其工具
4. **服务绑定** - 将 MCP 服务动态绑定到小智连接节点
5. **分布式 WebSocket 管理** - 支持多实例部署的 WebSocket 连接协调
6. **自动重连机制** - 后台监控和自动恢复断开的连接

### 架构模式 (Architecture Patterns)

- **前后端分离** - Blazor WebAssembly SPA + ASP.NET Core Web API
- **领域驱动设计 (DDD)** - 清晰的领域模型和聚合根
- **仓储模式 (Repository Pattern)** - 数据访问抽象层
- **依赖注入 (Dependency Injection)** - 完全基于 ASP.NET Core DI
- **分布式协调** - 使用 Redis 实现跨实例状态管理和分布式锁

---

## 🛠️ 技术栈 (Tech Stack)

### 后端 (Backend)
- **.NET 9** - 最新的 .NET 平台
- **ASP.NET Core Web API** - RESTful API with Minimal APIs pattern
- **ASP.NET Core Identity** - 用户认证和授权
- **OpenID Connect** - Keycloak 集成
- **Entity Framework Core 9.0** - ORM 框架，支持自动迁移
- **Redis** - 分布式缓存、状态管理和分布式锁
- **PostgreSQL / SQLite** - 支持多数据库切换

### 前端 (Frontend)
- **Blazor WebAssembly** - 客户端 SPA，离线支持
- **MudBlazor** - Material Design 3 UI 组件库
- **国际化 (i18n)** - 完整的多语言支持（中文/英文）

### 服务编排 (Orchestration)
- **.NET Aspire** - 云原生应用编排和开发仪表板

### 分布式系统 (Distributed System)
- **StackExchange.Redis 2.9.32** - Redis 客户端
- **RedLock.net 2.3.2** - 分布式锁（RedLock 算法）
- **WebSocket** - 实时双向通信
- **Background Services** - 连接监控和自动重连

### 核心 NuGet 包
```xml
<!-- MCP Protocol -->
<PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.3" />
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.3.0-preview.3" />

<!-- Identity & Authentication -->
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="9.0.*" />

<!-- Database -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.*" />

<!-- Redis & Distributed Coordination -->
<PackageReference Include="StackExchange.Redis" Version="2.9.32" />
<PackageReference Include="RedLock.net" Version="2.3.2" />

<!-- Blazor & UI -->
<PackageReference Include="MudBlazor" Version="8.*" />
```

---

## 📚 参考项目 (Reference Projects)

生成代码时，必须严格参考以下项目的架构和代码风格：

### 1. 架构和代码风格参考（最高优先级）
**仓库**: https://github.com/dotnet/eShop
- **必须严格遵循 eShop 的架构模式和代码风格**
- 仓储模式 (Repository Pattern) 实现
- 服务层 (Service Layer) 设计
- 依赖注入最佳实践
- 身份认证和授权实现（ASP.NET Core Identity + OpenID Connect）
- 领域驱动设计 (DDD) 模式
- Entity Framework Core 最佳实践

**关键参考点**：
- `src/Ordering.Infrastructure/Repositories/` - 仓储实现
- `src/Ordering.Domain/SeedWork/IRepository.cs` - 仓储接口
- `src/Identity.API/` - Identity 服务器实现
- `src/WebApp/Extensions/Extensions.cs` - OpenID Connect 配置
- `src/eShop.ServiceDefaults/AuthenticationExtensions.cs` - 认证扩展
- `src/Basket.API/Extensions/Extensions.cs` - 依赖注入模式
- `src/Ordering.API/Extensions/Extensions.cs` - 应用服务注册

### 2. MCP 协议实现参考
**仓库**: https://github.com/maker-community/mcp-calculator/tree/dev_csharp/csharp
- MCP 协议的 C# 实现方式
- WebSocket 连接和服务绑定逻辑

### 3. 前后端集成参考
**仓库**: https://github.com/GreenShadeZhang/agent-framework-tutorial-code/tree/main/agent-groupchat
- Blazor 与 ASP.NET Core API 的集成方式
- .NET Aspire 服务编排配置

---

## 📂 项目文档结构 (Documentation Structure)

项目采用结构化文档组织，所有文档位于 `docs/` 目录：

### 架构文档 (docs/architecture/)
- `DISTRIBUTED_WEBSOCKET_GUIDE.md` - 分布式 WebSocket 连接管理详细指南
- `FAILURE_RECOVERY_EXPLAINED.md` - 故障恢复机制说明
- `IMPLEMENTATION_SUMMARY.md` - 分布式实现总结
- `WEBSOCKET_FEATURES.md` - WebSocket 功能特性
- `MCP_AUTH_ENHANCEMENT.md` - MCP 认证增强方案
- `AGENTS.md` / `AGENTS_NEW.md` - AI 编程助手指南

### 开发指南 (docs/guides/)
- `QUICK_START_DISTRIBUTED.md` - 分布式部署快速开始
- `API_EXAMPLES.md` - API 使用示例
- `DEPLOYMENT.md` - 部署指南
- `TESTING_GUIDE.md` - 测试指南
- `UI_GUIDE.md` - UI 开发指南
- `FRONTEND_IMPROVEMENTS.md` - 前端改进日志

### 其他文档 (docs/)
- `CHANGELOG.md` - 变更日志
- `SUMMARY.md` - 项目总结

### 脚本目录 (scripts/)
- `verify-setup.ps1` - 环境验证脚本
- `start-dev.ps1` - 启动开发环境
- `fix-git-author.ps1` - Git 作者修复
- `update-api-names.ps1` - API 命名更新
- 其他维护脚本

**重要**: 新增功能时应同步更新相关文档，保持文档与代码一致性。

---

## 🌐 分布式 WebSocket 管理 (Distributed WebSocket Management)

### 核心问题

在多实例部署场景下，需要解决：
1. **避免重复连接** - 多个 API 实例不应同时创建到同一小智服务器的连接
2. **连接状态共享** - 所有实例需要知道哪些连接已被持有
3. **自动重连** - 服务重启后自动检测并重建断开的连接
4. **故障恢复** - 实例崩溃时其他实例接管连接（2-3分钟恢复时间）

### 解决方案架构

```
┌─────────────────────────────────────────────────────────────┐
│                     API 实例集群                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │  实例 1      │  │  实例 2      │  │  实例 3      │         │
│  │ Session Mgr │  │ Session Mgr │  │ Session Mgr │         │
│  │ Monitor Svc │  │ Monitor Svc │  │ Monitor Svc │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
└─────────┼─────────────────┼─────────────────┼─────────────────┘
          └─────────────────┼─────────────────┘
                           │
                    ┌──────▼──────┐
                    │    Redis     │
                    │ • 连接状态    │
                    │ • 分布式锁    │
                    │ • 心跳数据    │
                    └─────────────┘
```

### 关键组件

#### 1. 分布式锁服务 (IDistributedLockService)
- **位置**: `Services/DistributedLock/RedisDistributedLockService.cs`
- **功能**: 使用 RedLock 算法确保互斥访问
- **用途**: 防止多实例同时创建相同连接

#### 2. 连接状态服务 (IConnectionStateService)
- **位置**: `Services/ConnectionState/RedisConnectionStateService.cs`
- **功能**: Redis 存储所有连接状态和心跳
- **数据**: InstanceId (MachineName:ProcessId:Guid)、Status、Heartbeat

#### 3. 会话管理器 (McpSessionManager)
- **位置**: `Services/WebSocket/McpSessionManager.cs`
- **改进**: 双重检查锁定模式，启动前后检查 Redis 状态
- **流程**: 检查本地 → 检查 Redis → 获取锁 → 再次检查 → 创建连接

#### 4. 连接监控服务 (ConnectionMonitorHostedService)
- **位置**: `Services/BackgroundServices/ConnectionMonitorHostedService.cs`
- **功能**: 
  - 启动时自动连接所有已启用服务器
  - 每30秒检查连接状态并更新心跳
  - 检测过期连接（90秒超时）并自动重连
  - 60秒冷却期防止频繁重连

### 配置参数

```json
{
  "ConnectionStrings": {
    "redis": "localhost:6379"
  },
  "ConnectionMonitor": {
    "CheckIntervalSeconds": 30,
    "HeartbeatTimeoutSeconds": 90,
    "ReconnectCooldownSeconds": 60
  }
}
```

### 故障恢复时间线

- **实例崩溃**: 0秒
- **心跳超时检测**: 最多90秒（HeartbeatTimeoutSeconds）
- **其他实例发现**: 30秒内（CheckIntervalSeconds）
- **冷却期**: 60秒（ReconnectCooldownSeconds）
- **总恢复时间**: 约2-3分钟

**详细说明**: 参考 `docs/architecture/DISTRIBUTED_WEBSOCKET_GUIDE.md` 和 `docs/architecture/FAILURE_RECOVERY_EXPLAINED.md`

---

## 📋 命名空间约定 (Namespace Convention)

**所有项目必须使用 `Verdure` 作为根命名空间**

```
Verdure.McpPlatform.AppHost              # Aspire 宿主
Verdure.McpPlatform.Api                  # Web API
Verdure.McpPlatform.Web                  # Blazor 前端
Verdure.McpPlatform.Domain               # 领域模型
Verdure.McpPlatform.Infrastructure       # 基础设施层（仓储实现）
Verdure.McpPlatform.Application          # 应用服务层
Verdure.McpPlatform.Contracts            # 共享契约和 DTO
```

---

## 项目结构要求 (Project Structure)

遵循 eShop 的 Clean Architecture 和 DDD 模式：

```
/
├── src/
│   ├── Verdure.McpPlatform.AppHost/           # Aspire 应用宿主
│   │   └── Program.cs
│   │
│   ├── Verdure.McpPlatform.ServiceDefaults/   # 共享服务配置
│   │   ├── Extensions.cs
│   │   └── AuthenticationExtensions.cs
│   │
│   ├── Verdure.McpPlatform.Api/               # Web API 项目
│   │   ├── Apis/                              # Minimal API 端点
│   │   ├── Extensions/                        # 扩展方法
│   │   ├── Infrastructure/
│   │   │   └── Services/                      # 基础设施服务
│   │   ├── GlobalUsings.cs
│   │   └── Program.cs
│   │
│   ├── Verdure.McpPlatform.Web/               # Blazor WebAssembly 前端
│   │   ├── Pages/                             # 页面组件
│   │   ├── Components/                        # 共享组件
│   │   ├── Services/                          # 前端服务（HTTP客户端）
│   │   ├── wwwroot/                           # 静态资源
│   │   ├── Program.cs
│   │   └── _Imports.razor
│   │
│   ├── Verdure.McpPlatform.Domain/            # 领域层
│   │   ├── AggregatesModel/
│   │   │   ├── McpServerAggregate/
│   │   │   └── McpBindingAggregate/
│   │   ├── Events/                            # 领域事件
│   │   ├── Exceptions/                        # 领域异常
│   │   └── SeedWork/                          # DDD 基础设施
│   │       ├── Entity.cs
│   │       ├── IAggregateRoot.cs
│   │       ├── IRepository.cs
│   │       └── IUnitOfWork.cs
│   │
│   ├── Verdure.McpPlatform.Infrastructure/    # 基础设施层
│   │   ├── Data/
│   │   │   ├── McpPlatformContext.cs
│   │   │   ├── EntityConfigurations/
│   │   │   └── Migrations/
│   │   ├── Repositories/                      # 仓储实现
│   │   │   ├── McpServerRepository.cs
│   │   │   └── McpBindingRepository.cs
│   │   └── Identity/                          # Identity 配置
│   │       └── ApplicationDbContext.cs
│   │
│   ├── Verdure.McpPlatform.Application/       # 应用服务层
│   │   ├── Services/
│   │   │   ├── IMcpServerService.cs
│   │   │   └── McpServerService.cs
│   │   ├── DTOs/
│   │   └── Queries/                           # CQRS 查询
│   │
│   └── Verdure.McpPlatform.Contracts/         # 共享契约
│       ├── DTOs/
│       └── Requests/
│
├── tests/
│   ├── Verdure.McpPlatform.UnitTests/
│   └── Verdure.McpPlatform.FunctionalTests/
│
├── Verdure.McpPlatform.sln
└── AGENTS.md
```

---

## 架构设计原则 (Architecture Principles)

### 1. 仓储模式 (Repository Pattern)

**参考 eShop 的仓储实现**：

```csharp
// Domain Layer - IRepository interface
namespace Verdure.McpPlatform.Domain.SeedWork;

public interface IRepository<T> where T : IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
}

// Domain Layer - Specific repository
namespace Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;

public interface IMcpServerRepository : IRepository<McpServer>
{
    McpServer Add(McpServer server);
    void Update(McpServer server);
    Task<McpServer> GetAsync(int serverId);
    Task<IEnumerable<McpServer>> GetByUserIdAsync(string userId);
}

// Infrastructure Layer - Repository implementation
namespace Verdure.McpPlatform.Infrastructure.Repositories;

public class McpServerRepository : IMcpServerRepository
{
    private readonly McpPlatformContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public McpServerRepository(McpPlatformContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public McpServer Add(McpServer server)
    {
        return _context.McpServers.Add(server).Entity;
    }

    public void Update(McpServer server)
    {
        _context.Entry(server).State = EntityState.Modified;
    }

    public async Task<McpServer> GetAsync(int serverId)
    {
        var server = await _context.McpServers.FindAsync(serverId);
        if (server != null)
        {
            await _context.Entry(server)
                .Collection(s => s.Bindings).LoadAsync();
        }
        return server;
    }
}
```

### 2. 服务层 (Service Layer)

**应用服务在 Application 层，基础设施服务在 Infrastructure 层**：

```csharp
// Application Layer
namespace Verdure.McpPlatform.Application.Services;

public interface IMcpServerService
{
    Task<McpServerDto> CreateAsync(CreateMcpServerRequest request, string userId);
    Task<IEnumerable<McpServerDto>> GetByUserAsync(string userId);
}

public class McpServerService : IMcpServerService
{
    private readonly IMcpServerRepository _repository;
    private readonly ILogger<McpServerService> _logger;

    public McpServerService(
        IMcpServerRepository repository,
        ILogger<McpServerService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<McpServerDto> CreateAsync(CreateMcpServerRequest request, string userId)
    {
        var server = new McpServer(request.Name, request.Address, userId);
        _repository.Add(server);
        await _repository.UnitOfWork.SaveChangesAsync();
        return MapToDto(server);
    }
}
```

### 3. 依赖注入配置

**参考 eShop 的扩展方法模式**：

```csharp
// Extensions/Extensions.cs
namespace Verdure.McpPlatform.Api.Extensions;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        // Add authentication
        builder.AddDefaultAuthentication();

        // Add database context with repository pattern
        services.AddDbContext<McpPlatformContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("mcpdb"));
        });

        // Register repositories
        services.AddScoped<IMcpServerRepository, McpServerRepository>();
        services.AddScoped<IMcpBindingRepository, McpBindingRepository>();

        // Register application services
        services.AddScoped<IMcpServerService, McpServerService>();
        services.AddScoped<IMcpBindingService, McpBindingService>();

        // Add HTTP context accessor
        services.AddHttpContextAccessor();
        services.AddTransient<IIdentityService, IdentityService>();
    }
}
```

---

## 身份认证架构 (Authentication Architecture)

### 1. ASP.NET Core Identity 集成

**参考 eShop 的 Identity.API 实现**：

```csharp
// Infrastructure/Identity/ApplicationUser.cs
namespace Verdure.McpPlatform.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    // 扩展属性
    public string DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Infrastructure/Identity/ApplicationDbContext.cs
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // 自定义配置
    }
}

// Program.cs
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
```

### 2. Keycloak OpenID Connect 集成

**参考 eShop 的 OpenID Connect 配置**：

```csharp
// ServiceDefaults/AuthenticationExtensions.cs
namespace Verdure.McpPlatform.ServiceDefaults;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddDefaultAuthentication(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // Keycloak configuration
        var identitySection = configuration.GetSection("Identity");
        
        if (!identitySection.Exists())
        {
            return services;
        }

        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie(options => 
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        })
        .AddOpenIdConnect(options =>
        {
            var identityUrl = identitySection.GetRequiredValue("Url");
            var clientId = identitySection.GetRequiredValue("ClientId");
            
            options.Authority = identityUrl;
            options.RequireHttpsMetadata = false;
            options.ClientId = clientId;
            options.ClientSecret = identitySection.GetValue<string>("ClientSecret");
            options.ResponseType = "code";
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;
            
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
        });

        services.AddAuthorization();

        return services;
    }
}
```

### 3. 配置文件示例

```json
// appsettings.json
{
  "Identity": {
    "Url": "http://localhost:8080/realms/verdure",
    "ClientId": "verdure-mcp-platform",
    "ClientSecret": "your-client-secret",
    "Audience": "verdure-api"
  },
  "ConnectionStrings": {
    "mcpdb": "Host=localhost;Database=verdure_mcp;Username=postgres;Password=postgres",
    "identitydb": "Host=localhost;Database=verdure_identity;Username=postgres;Password=postgres"
  }
}
```

---

## 🏛️ 领域模型 (Domain Models)

### 核心聚合根

**重要**: 所有实体 ID 使用 `string` 类型（Guid Version 7），请参考现有实现。

#### 1. XiaozhiConnection (小智连接聚合根)

**位置**: `Domain/AggregatesModel/XiaozhiConnectionAggregate/XiaozhiConnection.cs`

**职责**: 代表到小智 AI 的 WebSocket 端点连接

**核心属性**:
- `Name`: 连接名称
- `Address`: WebSocket 端点地址
- `UserId`: 所属用户
- `IsEnabled`: 是否启用（用户可启用/禁用）
- `IsConnected`: 当前连接状态
- `ServiceBindings`: 绑定的 MCP 服务集合

**关键方法**:
```csharp
public class XiaozhiConnection : Entity, IAggregateRoot
{
    public void Enable() // 启用连接
    public void Disable() // 禁用连接并断开
    public void MarkConnected() // 标记为已连接
    public void MarkDisconnected() // 标记为已断开
    public McpServiceBinding AddServiceBinding(...) // 添加服务绑定
}
```

#### 2. McpServiceConfig (MCP服务配置聚合根)

**位置**: `Domain/AggregatesModel/McpServiceConfigAggregate/McpServiceConfig.cs`

**职责**: 代表一个 MCP 服务节点及其工具配置

**核心属性**:
- `Name`: 服务名称
- `Endpoint`: 服务端点
- `UserId`: 所属用户
- `IsPublic`: 是否公开（可被其他用户使用）
- `AuthenticationType`: 认证类型（Bearer/Basic/OAuth2/ApiKey）
- `AuthenticationConfig`: 认证配置（JSON）
- `Protocol`: 协议类型（stdio/http/sse）
- `Tools`: 工具集合

**关键方法**:
```csharp
public class McpServiceConfig : Entity, IAggregateRoot
{
    public void UpdateInfo(...) // 更新服务信息
    public void UpdateTools(IEnumerable<McpTool> tools) // 更新工具列表
}
```

#### 3. McpServiceBinding (值对象/实体)

**位置**: `Domain/AggregatesModel/XiaozhiConnectionAggregate/McpServiceBinding.cs`

**职责**: 绑定关系，连接 XiaozhiConnection 和 McpServiceConfig

**核心属性**:
- `XiaozhiConnectionId`: 小智连接ID
- `McpServiceConfigId`: MCP服务配置ID
- `SelectedToolNames`: 选中的工具名称列表（JSON）
- `IsEnabled`: 绑定是否启用

### DDD 基础设施

**参考 eShop 的 DDD 模式**：

```csharp
// Domain/SeedWork/Entity.cs
namespace Verdure.McpPlatform.Domain.SeedWork;

public abstract class Entity
{
    private string _id = string.Empty;
    
    public virtual string Id 
    {
        get => _id;
        protected set => _id = value;
    }
    
    protected void GenerateId()
    {
        _id = Guid.CreateVersion7().ToString();
    }
    
    // Domain events, equality comparison, etc.
}

// Domain/SeedWork/IAggregateRoot.cs
public interface IAggregateRoot { }

// Domain/SeedWork/IRepository.cs
public interface IRepository<T> where T : IAggregateRoot
{
    IUnitOfWork UnitOfWork { get; }
}
```

---

## 数据库配置 (Database Configuration)

### 多数据库支持策略

**参考 eShop 的数据库配置模式**：

```csharp
// Infrastructure/Data/McpPlatformContext.cs
namespace Verdure.McpPlatform.Infrastructure.Data;

public class McpPlatformContext : DbContext, IUnitOfWork
{
    public DbSet<McpServer> McpServers { get; set; }
    public DbSet<McpBinding> McpBindings { get; set; }

    public McpPlatformContext(DbContextOptions<McpPlatformContext> options) 
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(McpPlatformContext).Assembly);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await SaveChangesAsync(cancellationToken);
        return true;
    }
}

// Extensions/Extensions.cs - 数据库提供程序配置
public static void AddApplicationServices(this IHostApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("mcpdb");
    var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "PostgreSQL";

    switch (dbProvider)
    {
        case "PostgreSQL":
            builder.Services.AddDbContext<McpPlatformContext>(options =>
                options.UseNpgsql(connectionString));
            builder.EnrichNpgsqlDbContext<McpPlatformContext>();
            break;
            
        case "Sqlite":
            builder.Services.AddDbContext<McpPlatformContext>(options =>
                options.UseSqlite(connectionString));
            break;
            
        default:
            throw new InvalidOperationException($"Unsupported database provider: {dbProvider}");
    }

    // 自动迁移
    builder.Services.AddMigration<McpPlatformContext>();
}
```

### 实体配置

```csharp
// Infrastructure/Data/EntityConfigurations/McpServerEntityTypeConfiguration.cs
namespace Verdure.McpPlatform.Infrastructure.Data.EntityConfigurations;

public class McpServerEntityTypeConfiguration : IEntityTypeConfiguration<McpServer>
{
    public void Configure(EntityTypeBuilder<McpServer> builder)
    {
        builder.ToTable("mcp_servers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.HasIndex(s => s.UserId);

        builder.HasMany(s => s.Bindings)
            .WithOne()
            .HasForeignKey(b => b.McpServerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## API 端点设计 (API Endpoints)

**使用 Minimal API 而非 Controller（参考 eShop）**：

```csharp
// Apis/McpServerApi.cs
namespace Verdure.McpPlatform.Api.Apis;

public static class McpServerApi
{
    public static RouteGroupBuilder MapMcpServerApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/mcp-servers")
            .RequireAuthorization()
            .WithTags("MCP Servers");

        api.MapGet("/", GetMcpServersAsync)
            .WithName("GetMcpServers")
            .Produces<IEnumerable<McpServerDto>>();

        api.MapGet("/{id:int}", GetMcpServerAsync)
            .WithName("GetMcpServer")
            .Produces<McpServerDto>()
            .Produces(StatusCodes.Status404NotFound);

        api.MapPost("/", CreateMcpServerAsync)
            .WithName("CreateMcpServer")
            .Produces<McpServerDto>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        api.MapPut("/{id:int}", UpdateMcpServerAsync)
            .WithName("UpdateMcpServer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        api.MapDelete("/{id:int}", DeleteMcpServerAsync)
            .WithName("DeleteMcpServer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Results<Ok<IEnumerable<McpServerDto>>, NotFound>> GetMcpServersAsync(
        [AsParameters] McpPlatformServices services,
        ClaimsPrincipal user)
    {
        var userId = await services.IdentityService.GetUserIdentityAsync();
        var servers = await services.McpServerService.GetByUserAsync(userId);
        return TypedResults.Ok(servers);
    }

    private static async Task<Results<Created<McpServerDto>, ValidationProblem>> CreateMcpServerAsync(
        [AsParameters] McpPlatformServices services,
        CreateMcpServerRequest request,
        ClaimsPrincipal user)
    {
        var userId = await services.IdentityService.GetUserIdentityAsync();
        var server = await services.McpServerService.CreateAsync(request, userId);
        return TypedResults.Created($"/api/mcp-servers/{server.Id}", server);
    }
}

// Helper for dependency injection
public record McpPlatformServices(
    IMcpServerService McpServerService,
    IIdentityService IdentityService,
    ILogger<McpPlatformServices> Logger);
```

---

## 代码风格规范 (Coding Standards)

**严格遵循 eShop 的代码风格**：

### 命名约定
- **类名**: PascalCase (`McpServerService`, `McpServerRepository`)
- **接口**: `I` 前缀 + PascalCase (`IMcpServerService`, `IRepository<T>`)
- **私有字段**: `_` 前缀 + camelCase (`_repository`, `_logger`)
- **参数和局部变量**: camelCase (`userId`, `serverName`)
- **常量**: PascalCase 或 UPPER_CASE
- **异步方法**: `Async` 后缀 (`GetByUserAsync`, `CreateAsync`)

### 文件组织
```csharp
// GlobalUsings.cs - 全局 using 声明
global using System;
global using System.Collections.Generic;
global using System.Threading.Tasks;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;
global using Verdure.McpPlatform.Domain.SeedWork;
global using Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;
```

### 依赖注入模式
```csharp
// Primary constructor pattern (C# 12)
public class McpServerService(
    IMcpServerRepository repository,
    ILogger<McpServerService> logger) : IMcpServerService
{
    public async Task<McpServerDto> GetByIdAsync(int id)
    {
        var server = await repository.GetAsync(id);
        if (server is null)
        {
            logger.LogWarning("MCP server {ServerId} not found", id);
            throw new NotFoundException($"Server {id} not found");
        }
        return MapToDto(server);
    }
}
```

### 异步编程规范
- 所有 I/O 操作使用 `async/await`
- 永远不使用 `.Result` 或 `.Wait()`
- 使用 `ConfigureAwait(false)` 在库代码中
- 异步方法返回 `Task` 或 `Task<T>`

### 错误处理
```csharp
// Domain exceptions
namespace Verdure.McpPlatform.Domain.Exceptions;

public class McpPlatformDomainException : Exception
{
    public McpPlatformDomainException() { }

    public McpPlatformDomainException(string message)
        : base(message) { }

    public McpPlatformDomainException(string message, Exception innerException)
        : base(message, innerException) { }
}

// Global exception handler
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature =
            context.Features.Get<IExceptionHandlerPathFeature>();
        
        var exception = exceptionHandlerPathFeature?.Error;
        
        logger.LogError(exception, "An unhandled exception occurred");
        
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An error occurred",
            Detail = exception?.Message
        });
    });
});
```

---

## Blazor 前端开发指南 (Blazor Frontend)

**使用 Blazor WebAssembly（单页应用架构）**：

### Blazor WebAssembly 优势

- **客户端执行**: 应用在浏览器中运行，减少服务器负载
- **离线支持**: 可以通过 PWA 实现离线功能
- **更好的响应性**: 无需服务器往返，UI 更新更快
- **独立部署**: 可以部署到任何静态文件托管服务（CDN、Azure Static Web Apps 等）
- **更小的服务器成本**: 服务器只需提供 API，无需维护 SignalR 连接

### Program.cs 配置

```csharp
// Verdure.McpPlatform.Web/Program.cs
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Verdure.McpPlatform.Web;
using Verdure.McpPlatform.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base address
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] 
    ?? builder.HostEnvironment.BaseAddress;

// Add MudBlazor services
builder.Services.AddMudServices();

// Add authentication
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure HTTP clients for API
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiBaseAddress) 
});

// Register API client services
builder.Services.AddScoped<IMcpServerClientService, McpServerClientService>();
builder.Services.AddScoped<IMcpBindingClientService, McpBindingClientService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

await builder.Build().RunAsync();
```

### 组件结构

```razor
@* Pages/McpServers.razor *@
@page "/mcp-servers"
@using Verdure.McpPlatform.Web.Services
@inject IMcpServerClientService ServerService
@inject ISnackbar Snackbar
@inject NavigationManager Navigation
@attribute [Authorize]

<PageTitle>MCP Servers</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">MCP Server Management</MudText>
    
    <MudButton Variant="Variant.Filled" 
               Color="Color.Primary" 
               OnClick="OpenCreateDialog"
               StartIcon="@Icons.Material.Filled.Add">
        Add Server
    </MudButton>

    <MudTable Items="@_servers" 
              Loading="@_loading" 
              Hover="true" 
              Breakpoint="Breakpoint.Sm">
        <HeaderContent>
            <MudTh>Name</MudTh>
            <MudTh>Address</MudTh>
            <MudTh>Created</MudTh>
            <MudTh>Actions</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Name">@context.Name</MudTd>
            <MudTd DataLabel="Address">@context.Address</MudTd>
            <MudTd DataLabel="Created">@context.CreatedAt.ToShortDateString()</MudTd>
            <MudTd DataLabel="Actions">
                <MudIconButton Icon="@Icons.Material.Filled.Edit" 
                               Size="Size.Small" 
                               OnClick="@(() => EditServer(context))" />
                <MudIconButton Icon="@Icons.Material.Filled.Delete" 
                               Size="Size.Small" 
                               OnClick="@(() => DeleteServer(context))" />
            </MudTd>
        </RowTemplate>
    </MudTable>
</MudContainer>

@code {
    private List<McpServerDto> _servers = new();
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadServers();
    }

    private async Task LoadServers()
    {
        try
        {
            _loading = true;
            _servers = (await ServerService.GetServersAsync()).ToList();
        }
        catch (HttpRequestException ex)
        {
            Snackbar.Add($"Error loading servers: {ex.Message}", Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Unexpected error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void OpenCreateDialog()
    {
        Navigation.NavigateTo("/mcp-servers/create");
    }

    private void EditServer(McpServerDto server)
    {
        Navigation.NavigateTo($"/mcp-servers/edit/{server.Id}");
    }

    private async Task DeleteServer(McpServerDto server)
    {
        var confirmed = await Snackbar.Add(
            $"Delete server '{server.Name}'?", 
            Severity.Warning, 
            config => config.Action = "Delete");

        if (confirmed)
        {
            try
            {
                await ServerService.DeleteServerAsync(server.Id);
                _servers.Remove(server);
                Snackbar.Add("Server deleted successfully", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
        }
    }
}
```

### 服务客户端（带认证支持）

```csharp
// Services/McpServerClientService.cs
namespace Verdure.McpPlatform.Web.Services;

public interface IMcpServerClientService
{
    Task<IEnumerable<McpServerDto>> GetServersAsync();
    Task<McpServerDto> GetServerAsync(int id);
    Task<McpServerDto> CreateServerAsync(CreateMcpServerRequest request);
    Task UpdateServerAsync(int id, UpdateMcpServerRequest request);
    Task DeleteServerAsync(int id);
}

public class McpServerClientService : IMcpServerClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpServerClientService> _logger;

    public McpServerClientService(
        HttpClient httpClient, 
        ILogger<McpServerClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IEnumerable<McpServerDto>> GetServersAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<McpServerDto>>(
                "api/mcp-servers");
            return response ?? Enumerable.Empty<McpServerDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to get MCP servers");
            throw;
        }
    }

    public async Task<McpServerDto> GetServerAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<McpServerDto>(
            $"api/mcp-servers/{id}");
    }

    public async Task<McpServerDto> CreateServerAsync(CreateMcpServerRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/mcp-servers", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<McpServerDto>();
    }

    public async Task UpdateServerAsync(int id, UpdateMcpServerRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/mcp-servers/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteServerAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/mcp-servers/{id}");
        response.EnsureSuccessStatusCode();
    }
}
```

### 认证状态管理

```csharp
// Services/CustomAuthenticationStateProvider.cs
namespace Verdure.McpPlatform.Web.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _httpClient;

    public CustomAuthenticationStateProvider(
        ILocalStorageService localStorage,
        HttpClient httpClient)
    {
        _localStorage = localStorage;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync<string>("authToken");

        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        // Set authorization header
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        // Parse JWT token to get claims
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public async Task LoginAsync(string token)
    {
        await _localStorage.SetItemAsync("authToken", token);
        _httpClient.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(user)));
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        _httpClient.DefaultRequestHeaders.Authorization = null;

        NotifyAuthenticationStateChanged(
            Task.FromResult(new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity()))));
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        return keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()));
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
```

### 本地存储服务（用于 Token 管理）

需要添加 `Blazored.LocalStorage` NuGet 包：

```xml
<PackageReference Include="Blazored.LocalStorage" Version="4.5.0" />
```

在 `Program.cs` 中注册：

```csharp
builder.Services.AddBlazoredLocalStorage();
```

### wwwroot/appsettings.json 配置

```json
{
  "ApiBaseAddress": "https://localhost:5000",
  "Authentication": {
    "Authority": "http://localhost:8080/realms/verdure",
    "ClientId": "verdure-mcp-platform-web"
  }
}
```

---

## .NET Aspire 配置 (Aspire Orchestration)

**参考 eShop 的 AppHost 配置**：

```csharp
// Verdure.McpPlatform.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// 数据库资源
var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent);

var mcpDb = postgres.AddDatabase("mcpdb");
var identityDb = postgres.AddDatabase("identitydb");

// Redis for caching/session
var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent);

// Keycloak (optional)
var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithLifetime(ContainerLifetime.Persistent);

// API 服务
var api = builder.AddProject<Projects.Verdure_McpPlatform_Api>("api")
    .WithReference(mcpDb)
    .WithReference(identityDb)
    .WithReference(redis)
    .WithEnvironment("Identity__Url", keycloak.GetEndpoint("http"));

// Blazor WebAssembly 前端
// 注意：Blazor WASM 是静态文件，通常通过 API 项目托管或独立部署
builder.AddProject<Projects.Verdure_McpPlatform_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

---

## 构建和运行命令 (Build & Run Commands)

### 开发环境

```powershell
# 恢复所有依赖
dotnet restore

# 运行所有服务（通过 Aspire - 推荐）
dotnet run --project src/Verdure.McpPlatform.AppHost

# 单独运行 API
dotnet run --project src/Verdure.McpPlatform.Api

# 单独运行 Web 前端
dotnet run --project src/Verdure.McpPlatform.Web

# 运行单元测试
dotnet test tests/Verdure.McpPlatform.UnitTests

# 运行功能测试
dotnet test tests/Verdure.McpPlatform.FunctionalTests

# 运行所有测试
dotnet test
```

### 数据库管理

```powershell
# 添加新迁移
dotnet ef migrations add <MigrationName> `
  --project src/Verdure.McpPlatform.Infrastructure `
  --startup-project src/Verdure.McpPlatform.Api

# 应用迁移到数据库
dotnet ef database update `
  --project src/Verdure.McpPlatform.Infrastructure `
  --startup-project src/Verdure.McpPlatform.Api

# 删除最后一个迁移
dotnet ef migrations remove `
  --project src/Verdure.McpPlatform.Infrastructure `
  --startup-project src/Verdure.McpPlatform.Api

# 查看迁移列表
dotnet ef migrations list `
  --project src/Verdure.McpPlatform.Infrastructure `
  --startup-project src/Verdure.McpPlatform.Api

# 生成 SQL 脚本
dotnet ef migrations script `
  --project src/Verdure.McpPlatform.Infrastructure `
  --startup-project src/Verdure.McpPlatform.Api `
  --output scripts/migrations.sql
```

### Docker 部署

```powershell
# 构建 Docker 镜像
docker build -t verdure-mcp-api -f src/Verdure.McpPlatform.Api/Dockerfile .
docker build -t verdure-mcp-web -f src/Verdure.McpPlatform.Web/Dockerfile .

# 运行 Docker Compose
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止服务
docker-compose down
```

### 生产构建

```powershell
# 发布 API
dotnet publish src/Verdure.McpPlatform.Api `
  -c Release `
  -o publish/api `
  --self-contained false

# 发布 Web 前端
dotnet publish src/Verdure.McpPlatform.Web `
  -c Release `
  -o publish/web `
  --self-contained false

# 发布 AppHost（用于生产环境的 Aspire）
dotnet publish src/Verdure.McpPlatform.AppHost `
  -c Release `
  -o publish/apphost
```

---

## 常见开发任务 (Common Development Tasks)

### 添加新的聚合根和仓储

**参考 eShop 的 Ordering 领域模型**：

1. **定义聚合根** (`Domain/AggregatesModel/<Aggregate>/`)

```csharp
// Domain/AggregatesModel/McpBindingAggregate/McpBinding.cs
namespace Verdure.McpPlatform.Domain.AggregatesModel.McpBindingAggregate;

public class McpBinding : Entity, IAggregateRoot
{
    public string ServiceName { get; private set; }
    public string NodeAddress { get; private set; }
    public int McpServerId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected McpBinding() { }

    public McpBinding(string serviceName, string nodeAddress, int mcpServerId)
    {
        ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        NodeAddress = nodeAddress ?? throw new ArgumentNullException(nameof(nodeAddress));
        McpServerId = mcpServerId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
```

2. **定义仓储接口** (`Domain/AggregatesModel/<Aggregate>/`)

```csharp
// Domain/AggregatesModel/McpBindingAggregate/IMcpBindingRepository.cs
public interface IMcpBindingRepository : IRepository<McpBinding>
{
    McpBinding Add(McpBinding binding);
    void Update(McpBinding binding);
    Task<McpBinding> GetAsync(int bindingId);
    Task<IEnumerable<McpBinding>> GetByServerIdAsync(int serverId);
    Task<IEnumerable<McpBinding>> GetActiveBindingsAsync();
}
```

3. **实现仓储** (`Infrastructure/Repositories/`)

```csharp
// Infrastructure/Repositories/McpBindingRepository.cs
namespace Verdure.McpPlatform.Infrastructure.Repositories;

public class McpBindingRepository : IMcpBindingRepository
{
    private readonly McpPlatformContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public McpBindingRepository(McpPlatformContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public McpBinding Add(McpBinding binding)
    {
        return _context.McpBindings.Add(binding).Entity;
    }

    public async Task<McpBinding> GetAsync(int bindingId)
    {
        return await _context.McpBindings.FindAsync(bindingId);
    }

    public async Task<IEnumerable<McpBinding>> GetByServerIdAsync(int serverId)
    {
        return await _context.McpBindings
            .Where(b => b.McpServerId == serverId)
            .ToListAsync();
    }
}
```

4. **配置实体映射** (`Infrastructure/Data/EntityConfigurations/`)

```csharp
// Infrastructure/Data/EntityConfigurations/McpBindingEntityTypeConfiguration.cs
public class McpBindingEntityTypeConfiguration : IEntityTypeConfiguration<McpBinding>
{
    public void Configure(EntityTypeBuilder<McpBinding> builder)
    {
        builder.ToTable("mcp_bindings");
        builder.HasKey(b => b.Id);
        
        builder.Property(b => b.ServiceName)
            .HasMaxLength(200)
            .IsRequired();
            
        builder.HasIndex(b => b.McpServerId);
        builder.HasIndex(b => b.IsActive);
    }
}
```

5. **注册仓储** (`Api/Extensions/Extensions.cs`)

```csharp
services.AddScoped<IMcpBindingRepository, McpBindingRepository>();
```

### 添加新的应用服务

1. **定义服务接口** (`Application/Services/`)

```csharp
// Application/Services/IMcpBindingService.cs
namespace Verdure.McpPlatform.Application.Services;

public interface IMcpBindingService
{
    Task<McpBindingDto> CreateAsync(CreateMcpBindingRequest request, string userId);
    Task<IEnumerable<McpBindingDto>> GetByServerAsync(int serverId);
    Task ActivateAsync(int bindingId, string userId);
    Task DeactivateAsync(int bindingId, string userId);
}
```

2. **实现服务** (`Application/Services/`)

```csharp
// Application/Services/McpBindingService.cs
public class McpBindingService : IMcpBindingService
{
    private readonly IMcpBindingRepository _bindingRepository;
    private readonly IMcpServerRepository _serverRepository;
    private readonly ILogger<McpBindingService> _logger;

    public McpBindingService(
        IMcpBindingRepository bindingRepository,
        IMcpServerRepository serverRepository,
        ILogger<McpBindingService> logger)
    {
        _bindingRepository = bindingRepository;
        _serverRepository = serverRepository;
        _logger = logger;
    }

    public async Task<McpBindingDto> CreateAsync(CreateMcpBindingRequest request, string userId)
    {
        var server = await _serverRepository.GetAsync(request.ServerId);
        
        if (server == null || server.UserId != userId)
        {
            throw new UnauthorizedAccessException("Server not found or access denied");
        }

        var binding = new McpBinding(
            request.ServiceName, 
            request.NodeAddress, 
            request.ServerId);

        _bindingRepository.Add(binding);
        await _bindingRepository.UnitOfWork.SaveEntitiesAsync();

        _logger.LogInformation(
            "Created MCP binding {BindingId} for server {ServerId}", 
            binding.Id, 
            request.ServerId);

        return MapToDto(binding);
    }
}
```

3. **注册服务** (`Api/Extensions/Extensions.cs`)

```csharp
services.AddScoped<IMcpBindingService, McpBindingService>();
```

### 添加新的 API 端点

**使用 Minimal API 模式（参考 eShop）**：

```csharp
// Apis/McpBindingApi.cs
namespace Verdure.McpPlatform.Api.Apis;

public static class McpBindingApi
{
    public static RouteGroupBuilder MapMcpBindingApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/mcp-bindings")
            .RequireAuthorization()
            .WithTags("MCP Bindings");

        api.MapGet("/", GetBindingsAsync);
        api.MapGet("/server/{serverId:int}", GetBindingsByServerAsync);
        api.MapPost("/", CreateBindingAsync);
        api.MapPut("/{id:int}/activate", ActivateBindingAsync);
        api.MapPut("/{id:int}/deactivate", DeactivateBindingAsync);
        api.MapDelete("/{id:int}", DeleteBindingAsync);

        return api;
    }

    private static async Task<Ok<IEnumerable<McpBindingDto>>> GetBindingsByServerAsync(
        int serverId,
        [AsParameters] McpPlatformServices services)
    {
        var bindings = await services.McpBindingService.GetByServerAsync(serverId);
        return TypedResults.Ok(bindings);
    }

    private static async Task<Created<McpBindingDto>> CreateBindingAsync(
        CreateMcpBindingRequest request,
        [AsParameters] McpPlatformServices services,
        ClaimsPrincipal user)
    {
        var userId = await services.IdentityService.GetUserIdentityAsync();
        var binding = await services.McpBindingService.CreateAsync(request, userId);
        return TypedResults.Created($"/api/mcp-bindings/{binding.Id}", binding);
    }
}
```

### 添加新的 Blazor 页面

1. **创建页面组件** (`Web/Components/Pages/`)

```razor
@* Components/Pages/McpBindings.razor *@
@page "/mcp-bindings"
@using Verdure.McpPlatform.Web.Services
@inject IMcpBindingClientService BindingService
@inject ISnackbar Snackbar
@attribute [Authorize]

<PageTitle>MCP Bindings</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">MCP Service Bindings</MudText>
    
    @* Component implementation *@
</MudContainer>

@code {
    private List<McpBindingDto> _bindings = new();
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadBindings();
    }

    private async Task LoadBindings()
    {
        try
        {
            _loading = true;
            _bindings = (await BindingService.GetBindingsAsync()).ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }
}
```

2. **创建服务客户端** (`Web/Services/`)

```csharp
// Services/McpBindingClientService.cs
namespace Verdure.McpPlatform.Web.Services;

public class McpBindingClientService : IMcpBindingClientService
{
    private readonly HttpClient _httpClient;

    public McpBindingClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<McpBindingDto>> GetBindingsAsync()
    {
        return await _httpClient.GetFromJsonAsync<IEnumerable<McpBindingDto>>(
            "api/mcp-bindings") ?? Enumerable.Empty<McpBindingDto>();
    }
}
```

3. **注册服务** (`Web/Extensions/Extensions.cs`)

```csharp
builder.Services.AddHttpClient<IMcpBindingClientService, McpBindingClientService>(
    client => client.BaseAddress = new Uri("https+http://api"))
    .AddAuthToken();
```

4. **添加导航链接** (`Web/Components/Layout/NavMenu.razor`)

```razor
<MudNavLink Href="/mcp-bindings" Icon="@Icons.Material.Filled.Link">
    Bindings
</MudNavLink>
```

---

## 测试指南 (Testing Guidelines)

**参考 eShop 的测试模式**：

### 单元测试

```csharp
// tests/Verdure.McpPlatform.UnitTests/Application/McpServerServiceTests.cs
namespace Verdure.McpPlatform.UnitTests.Application;

[TestClass]
public class McpServerServiceTests
{
    private readonly Mock<IMcpServerRepository> _mockRepository;
    private readonly Mock<ILogger<McpServerService>> _mockLogger;
    private readonly McpServerService _service;

    public McpServerServiceTests()
    {
        _mockRepository = new Mock<IMcpServerRepository>();
        _mockLogger = new Mock<ILogger<McpServerService>>();
        _service = new McpServerService(_mockRepository.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task CreateAsync_ValidRequest_ReturnsServerDto()
    {
        // Arrange
        var request = new CreateMcpServerRequest
        {
            Name = "Test Server",
            Address = "http://localhost:5000"
        };
        var userId = "user-123";

        var expectedServer = new McpServer("Test Server", "http://localhost:5000", userId);
        _mockRepository.Setup(r => r.Add(It.IsAny<McpServer>()))
            .Returns(expectedServer);

        // Act
        var result = await _service.CreateAsync(request, userId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Server", result.Name);
        _mockRepository.Verify(r => r.Add(It.IsAny<McpServer>()), Times.Once);
    }
}
```

### 功能测试

```csharp
// tests/Verdure.McpPlatform.FunctionalTests/Api/McpServerApiTests.cs
namespace Verdure.McpPlatform.FunctionalTests.Api;

[TestClass]
public class McpServerApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public McpServerApiTests()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TestMethod]
    public async Task GetMcpServers_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/mcp-servers");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
```

---

## 安全性考虑 (Security Considerations)

### 认证和授权

- **始终验证用户身份**: 使用 `IIdentityService` 获取当前用户 ID
- **资源所有权检查**: 确保用户只能访问自己的资源
- **使用 HTTPS**: 生产环境必须使用 HTTPS
- **Token 过期**: 配置合理的 token 过期时间

### 数据验证

```csharp
// Application/DTOs/CreateMcpServerRequest.cs
public record CreateMcpServerRequest
{
    [Required(ErrorMessage = "Server name is required")]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    public string Name { get; init; }

    [Required(ErrorMessage = "Server address is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; init; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; init; }
}
```

### SQL 注入防护

- **使用 EF Core 参数化查询**: 永远不要拼接 SQL 字符串
- **使用 LINQ**: 优先使用 LINQ 而非原始 SQL

### 日志安全

```csharp
// 不要记录敏感信息
_logger.LogInformation("User {UserId} created server {ServerId}", userId, serverId);

// 避免这样做
_logger.LogInformation("User password: {Password}", password); // ❌ 错误
```

---

## 性能优化 (Performance Optimization)

### 数据库查询优化

```csharp
// 使用 AsNoTracking 进行只读查询
public async Task<IEnumerable<McpServerDto>> GetByUserAsync(string userId)
{
    var servers = await _context.McpServers
        .AsNoTracking()
        .Where(s => s.UserId == userId)
        .Include(s => s.Bindings)
        .ToListAsync();
        
    return servers.Select(MapToDto);
}

// 分页查询
public async Task<PagedResult<McpServerDto>> GetPagedAsync(int page, int pageSize)
{
    var query = _context.McpServers.AsNoTracking();
    
    var total = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
        
    return new PagedResult<McpServerDto>
    {
        Items = items.Select(MapToDto),
        TotalCount = total,
        Page = page,
        PageSize = pageSize
    };
}
```

### 缓存策略

```csharp
// 使用分布式缓存（Redis）
public class CachedMcpServerService : IMcpServerService
{
    private readonly IMcpServerService _inner;
    private readonly IDistributedCache _cache;

    public async Task<McpServerDto> GetAsync(int serverId)
    {
        var cacheKey = $"server:{serverId}";
        var cached = await _cache.GetStringAsync(cacheKey);
        
        if (cached != null)
        {
            return JsonSerializer.Deserialize<McpServerDto>(cached);
        }

        var server = await _inner.GetAsync(serverId);
        
        await _cache.SetStringAsync(
            cacheKey, 
            JsonSerializer.Serialize(server),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });

        return server;
    }
}
```

---

## 监控和日志 (Monitoring & Logging)

### 结构化日志

```csharp
_logger.LogInformation(
    "MCP server {ServerId} created by user {UserId} at {CreatedAt}",
    server.Id,
    userId,
    DateTime.UtcNow);

_logger.LogWarning(
    "Failed to activate binding {BindingId} - server {ServerId} not found",
    bindingId,
    serverId);

_logger.LogError(
    exception,
    "Error processing MCP request for binding {BindingId}",
    bindingId);
```

### Application Insights (可选)

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});
```

---

## 部署清单 (Deployment Checklist)

### 生产环境配置

- [ ] 配置生产数据库连接字符串
- [ ] 启用 HTTPS
- [ ] 配置 CORS 策略
- [ ] 设置环境变量
- [ ] 配置日志级别
- [ ] 启用健康检查
- [ ] 配置 Redis 连接（如果使用缓存）
- [ ] 配置 Keycloak/Identity 服务器
- [ ] 设置备份策略
- [ ] 配置监控和告警

### Docker Compose 示例

```yaml
# docker-compose.yml
version: '3.8'

services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  api:
    image: verdure-mcp-api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__mcpdb=Host=postgres;Database=verdure_mcp;Username=postgres;Password=${POSTGRES_PASSWORD}
      - Identity__Url=http://keycloak:8080/realms/verdure
    depends_on:
      - postgres
      - redis
    ports:
      - "5000:8080"

  web:
    image: verdure-mcp-web:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - api
    ports:
      - "5001:8080"

volumes:
  postgres_data:
```

---

## 🔧 常见开发流程 (Common Development Workflows)

### 添加新功能

1. **确定需求** → 检查是否需要新的聚合根或实体
2. **设计领域模型** → 在 `Domain/AggregatesModel/` 中创建
3. **创建仓储** → 在 `Infrastructure/Repositories/` 中实现
4. **创建应用服务** → 在 `Application/Services/` 中实现业务逻辑
5. **创建 API 端点** → 在 `Api/Apis/` 中使用 Minimal API
6. **创建前端页面** → 在 `Web/Pages/` 或 `Web/Components/` 中实现
7. **编写测试** → 单元测试和功能测试
8. **更新文档** → 同步更新相关文档

### 修改数据库结构

```powershell
# 1. 修改 Domain 实体
# 2. 修改或添加 EntityConfiguration
# 3. 创建迁移
dotnet ef migrations add <MigrationName> `
  --project src/Verdure.McpPlatform.Infrastructure `
  --startup-project src/Verdure.McpPlatform.Api

# 4. 检查生成的迁移文件
# 5. 应用迁移
dotnet ef database update `
  --project src/Verdure.McpPlatform.Infrastructure `
  --startup-project src/Verdure.McpPlatform.Api
```

### 添加新的分布式服务

对于需要跨实例协调的功能：

1. 考虑使用 `IDistributedLockService` 避免竞态条件
2. 使用 `IConnectionStateService` 共享状态
3. 实现 `BackgroundService` 进行定期检查
4. 配置合适的超时和重试策略
5. 更新 `appsettings.json` 配置

### 测试分布式功能

```powershell
# 启动 Redis
docker run -d -p 6379:6379 redis:7-alpine

# 启动多个 API 实例（不同端口）
$env:ASPNETCORE_URLS="http://localhost:5000"; dotnet run --project src/Verdure.McpPlatform.Api
$env:ASPNETCORE_URLS="http://localhost:5001"; dotnet run --project src/Verdure.McpPlatform.Api
$env:ASPNETCORE_URLS="http://localhost:5002"; dotnet run --project src/Verdure.McpPlatform.Api

# 观察 Redis 中的连接状态
redis-cli
> KEYS mcp:*
> GET mcp:connection:state:<serverId>
```

---

## 📝 注意事项 (Important Notes)

### ⚠️ 关键提醒
1. **代码风格**：必须严格遵循 eShop 的 .NET 代码风格
2. **实体 ID**：所有实体 ID 使用 `string` 类型（Guid Version 7）
3. **自动迁移**：EF Core 必须配置为自动迁移，避免手动 SQL 操作
4. **异步优先**：所有 I/O 操作必须使用异步方法
5. **依赖注入**：使用 ASP.NET Core 的内置 DI 容器
6. **日志记录**：关键操作必须记录日志
7. **文档同步**：新增功能必须同步更新文档

### 数据库扩展性
虽然默认使用 PostgreSQL，但代码应该设计为可以轻松切换到 SQLite，通过连接字符串和 EF Core Provider 配置。

### 分布式系统注意事项
- 总是考虑网络分区和时钟偏移
- 使用适当的超时和重试策略
- 避免长时间持有分布式锁
- 心跳间隔要合理（不要太频繁）
- 日志中记录 InstanceId 便于排查问题

### 数据库扩展性
虽然默认使用 PostgreSQL，但代码应该设计为可以轻松切换到 SQLite，通过连接字符串和 EF Core Provider 配置。

---

## 版本信息

- **.NET**: 9.0
- **ModelContextProtocol**: 0.3.0-preview.3
- **Entity Framework Core**: 9.0.x
- **MudBlazor**: 最新稳定版
- **.NET Aspire**: 9.0.x

---

## 📚 资源链接 (Resources)

### 官方文档
- [.NET 9 文档](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9)
- [MudBlazor 文档](https://mudblazor.com/)
- [.NET Aspire 文档](https://learn.microsoft.com/dotnet/aspire/)
- [EF Core 文档](https://learn.microsoft.com/ef/core/)
- [ModelContextProtocol GitHub](https://github.com/modelcontextprotocol)

### 参考项目
- [eShop](https://github.com/dotnet/eShop) - 架构参考
- [MCP Calculator](https://github.com/maker-community/mcp-calculator/tree/dev_csharp/csharp) - MCP 实现参考
- [Agent Framework Tutorial](https://github.com/GreenShadeZhang/agent-framework-tutorial-code) - 集成参考

### 项目文档
- `docs/architecture/` - 架构设计文档
- `docs/guides/` - 开发指南
- `scripts/` - 常用脚本

---

## 🤖 AI Agent 使用指南 (AI Agent Instructions)

### 代码生成准则

**必须遵循的原则**:
1. ✅ **严格遵循 eShop 架构** - 所有代码必须符合 eShop 的 DDD 和仓储模式
2. ✅ **使用 Guid Version 7** - 所有实体 ID 必须是 `string` 类型，使用 `Guid.CreateVersion7().ToString()`
3. ✅ **Minimal API 模式** - 使用 Minimal API 而非 Controller
4. ✅ **异步优先** - 所有 I/O 操作必须是异步的
5. ✅ **完整实现** - 不要省略代码，不要使用 `// ... existing code ...` 占位符

### 命名约定

```csharp
// ✅ 正确
public class McpServerService  // PascalCase for classes
private readonly ILogger<McpServerService> _logger;  // _camelCase for private fields
public async Task<McpServerDto> GetAsync(string id)  // Async suffix for async methods

// ❌ 错误
public class mcpServerService  // Wrong casing
private ILogger logger;  // Missing underscore
public Task<McpServerDto> Get(string id)  // Missing Async suffix
```

### 创建新功能的步骤

```
1. Domain Layer (领域层)
   - 创建聚合根: Domain/AggregatesModel/<Aggregate>/<Entity>.cs
   - 定义仓储接口: Domain/AggregatesModel/<Aggregate>/I<Entity>Repository.cs
   
2. Infrastructure Layer (基础设施层)
   - 实现仓储: Infrastructure/Repositories/<Entity>Repository.cs
   - 配置实体: Infrastructure/Data/EntityConfigurations/<Entity>Configuration.cs
   
3. Application Layer (应用层)
   - 创建服务接口: Application/Services/I<Entity>Service.cs
   - 实现服务: Application/Services/<Entity>Service.cs
   
4. API Layer (API层)
   - 创建端点: Api/Apis/<Entity>Api.cs
   - 注册服务: Api/Extensions/Extensions.cs
   
5. Web Layer (前端层)
   - 创建客户端服务: Web/Services/I<Entity>ClientService.cs
   - 创建页面: Web/Pages/<Entity>.razor
   
6. Database (数据库)
   - 生成迁移: dotnet ef migrations add <Name>
   - 应用迁移: dotnet ef database update
   
7. Testing (测试)
   - 单元测试: tests/UnitTests/<Entity>Tests.cs
   - 功能测试: tests/FunctionalTests/<Entity>ApiTests.cs
   
8. Documentation (文档)
   - 更新相关文档: docs/
```

### 分布式功能开发注意

当开发需要跨实例协调的功能时：

```csharp
// ✅ 正确使用分布式锁
await using var lockHandle = await _lockService.AcquireLockAsync(
    $"mcp:lock:{resourceKey}",
    expiryTime: TimeSpan.FromMinutes(5),
    waitTime: TimeSpan.FromSeconds(10),
    retryTime: TimeSpan.FromSeconds(1));

if (lockHandle != null && lockHandle.IsAcquired)
{
    // 1. 再次检查状态（双重检查）
    var state = await _stateService.GetConnectionStateAsync(serverId);
    if (state != null && state.Status == ConnectionStatus.Connected)
    {
        return; // 已经有其他实例处理了
    }
    
    // 2. 执行操作
    // 3. 更新状态
    // 4. 锁会自动释放（await using）
}
```

### 常见错误及解决

| 错误 | 正确做法 |
|------|---------|
| ❌ 使用 `int` 作为实体 ID | ✅ 使用 `string` 类型的 Guid Version 7 |
| ❌ 直接修改数据库不创建迁移 | ✅ 修改实体后创建 EF Core 迁移 |
| ❌ 使用 Controller | ✅ 使用 Minimal API |
| ❌ 同步方法调用数据库 | ✅ 使用异步方法（async/await） |
| ❌ 在多实例环境不加锁 | ✅ 使用 IDistributedLockService |
| ❌ 硬编码连接字符串 | ✅ 使用配置文件和环境变量 |

### 测试要求

```powershell
# 在提交代码前必须运行
dotnet restore
dotnet build
dotnet test

# 验证数据库迁移
dotnet ef migrations add Test --project src/Verdure.McpPlatform.Infrastructure --startup-project src/Verdure.McpPlatform.Api
dotnet ef migrations remove --project src/Verdure.McpPlatform.Infrastructure --startup-project src/Verdure.McpPlatform.Api
```

### 文档更新要求

新增或修改功能时，必须同步更新：
- [ ] API 端点文档 (`docs/guides/API_EXAMPLES.md`)
- [ ] 相关架构文档 (`docs/architecture/`)
- [ ] CHANGELOG.md
- [ ] 如果涉及前端，更新 UI_GUIDE.md

### 代码审查清单

提交代码前确认：
- [ ] 遵循 eShop 代码风格
- [ ] 使用正确的命名空间 (`Verdure.McpPlatform.*`)
- [ ] 所有实体 ID 为 `string` 类型
- [ ] 使用异步方法处理 I/O
- [ ] 添加了适当的日志记录
- [ ] 实现了仓储模式
- [ ] 注册了依赖注入
- [ ] 创建了数据库迁移
- [ ] 编写了单元测试
- [ ] 更新了相关文档

---

## 🎓 学习路径 (Learning Path)

### 新手上手

1. **阅读项目概述** → 理解项目目标和核心功能
2. **查看领域模型** → 了解 XiaozhiConnection 和 McpServiceConfig
3. **研究分布式架构** → 阅读 `docs/architecture/DISTRIBUTED_WEBSOCKET_GUIDE.md`
4. **运行项目** → 使用 `scripts/verify-setup.ps1` 和 `scripts/start-dev.ps1`
5. **查看示例** → 阅读 `docs/guides/API_EXAMPLES.md`

### 深入理解

1. **DDD 模式** → 研究 eShop 的领域驱动设计
2. **仓储模式** → 查看 `Infrastructure/Repositories/` 实现
3. **分布式锁** → 理解 RedLock 算法和使用场景
4. **WebSocket 管理** → 研究 `McpSessionManager` 实现
5. **前端架构** → 学习 Blazor WebAssembly 和 MudBlazor

### 进阶主题

1. **多实例部署** → 测试分布式 WebSocket 协调
2. **故障恢复** → 理解自动重连机制
3. **性能优化** → Redis 缓存和查询优化
4. **安全加固** → OpenID Connect 和多租户隔离
5. **监控告警** → 日志聚合和健康检查

---

## 💡 最佳实践提示 (Best Practices)

### Do's ✅

- **参考 eShop 实现** - 遇到不确定的架构问题，先查看 eShop
- **使用分布式锁** - 多实例环境下的关键操作必须加锁
- **记录详细日志** - 包含足够的上下文信息（ServerId, UserId, InstanceId等）
- **编写测试** - 每个新功能都应该有单元测试和功能测试
- **保持文档更新** - 代码变更时同步更新文档

### Don'ts ❌

- **不要跳过迁移** - 必须使用 EF Core 迁移管理数据库变更
- **不要硬编码** - 配置信息必须来自 appsettings.json 或环境变量
- **不要忽略异常** - 所有异常必须适当处理和记录
- **不要在生产环境直接修改数据库** - 使用迁移脚本
- **不要长时间持有锁** - 分布式锁应该尽快释放

---

## 🆘 故障排查 (Troubleshooting)

### 常见问题

**问题**: WebSocket 连接在多实例环境下重复创建
**解决**: 检查 Redis 连接，确认分布式锁服务正常工作

**问题**: 实例崩溃后连接无法恢复
**解决**: 检查 ConnectionMonitorHostedService 是否启动，查看心跳超时配置

**问题**: 数据库迁移失败
**解决**: 确认数据库连接字符串正确，检查迁移文件冲突

**问题**: Blazor 前端无法连接 API
**解决**: 检查 CORS 配置，确认 API 基址配置正确

### 调试技巧

```powershell
# 查看 Redis 中的连接状态
redis-cli
> KEYS mcp:connection:state:*
> GET mcp:connection:state:<serverId>

# 查看分布式锁
> KEYS mcp:lock:*

# 启用详细日志
# 修改 appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Verdure.McpPlatform": "Debug",
      "Verdure.McpPlatform.Api.Services": "Trace"
    }
  }
}
```
