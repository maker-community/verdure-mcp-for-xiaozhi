# AGENTS.md

A dedicated guide for AI coding agents working on the Verdure MCP Platform project.

---

## 项目概述 (Project Overview)

这是一个基于 .NET 9 的多租户 SaaS 平台，用于管理和提供 MCP (Model Context Protocol) 服务。

**核心功能**：
- 多用户身份认证系统（支持 ASP.NET Core Identity 和 Keycloak OpenID Connect）
- 每个用户可配置自己的小智 AI 服务器地址
- 将不同的 MCP 服务绑定到指定节点
- 通过 WebSocket 连接提供对应的 MCP 服务
- 仓储模式 (Repository Pattern) 实现数据访问层，支持多种数据库

**架构模式**：
- 前后端分离
- 领域驱动设计 (DDD)
- 仓储模式 (Repository Pattern)
- 依赖注入 (Dependency Injection)
- CQRS (可选)

---

## 技术栈 (Tech Stack)

### 后端
- **.NET 9**
- **ASP.NET Core Web API** - RESTful API
- **ASP.NET Core Identity** - 用户认证和授权
- **OpenID Connect** - 与 Keycloak 集成
- **Entity Framework Core 9.0** - ORM 框架
- **仓储模式** - 数据访问抽象层
- **PostgreSQL / SQLite** - 支持多种数据库

### 前端
- **Blazor WebAssembly** - 客户端 Blazor（单页应用，更好的性能和离线支持）
- **MudBlazor** - UI 组件库

### 服务编排
- **.NET Aspire** - 云原生应用编排

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

<!-- Blazor & UI -->
<PackageReference Include="MudBlazor" Version="8.*" />
```

---

## 参考项目 (Reference Projects)

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

## 命名空间约定 (Namespace Convention)

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

## 领域模型 (Domain Models)

### 核心实体

**参考 eShop 的 DDD 模式和聚合根设计**：

```csharp
// Domain/SeedWork/Entity.cs
namespace Verdure.McpPlatform.Domain.SeedWork;

public abstract class Entity
{
    string? _requestedHashCode;
    string _Id;
    
    public virtual string Id 
    {
        get => _Id;
        protected set => _Id = value;
    }
    
    // Domain events, equality comparison, etc.
}

新增的数据库表的实体Id 全部为string类型，请参考现有的实现。

// Domain/AggregatesModel/McpServerAggregate/McpServer.cs
namespace Verdure.McpPlatform.Domain.AggregatesModel.McpServerAggregate;

public class McpServer : Entity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Address { get; private set; }
    public string UserId { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private readonly List<McpBinding> _bindings;
    public IReadOnlyCollection<McpBinding> Bindings => _bindings.AsReadOnly();

    protected McpServer() 
    { 
        _bindings = new List<McpBinding>(); 
    }

    public McpServer(string name, string address, string userId) : this()
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Address = address ?? throw new ArgumentNullException(nameof(address));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateInfo(string name, string address, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Address = address ?? throw new ArgumentNullException(nameof(address));
        Description = description;
    }

    public McpBinding AddBinding(string serviceName, string nodeAddress)
    {
        var binding = new McpBinding(serviceName, nodeAddress, Id);
        _bindings.Add(binding);
        return binding;
    }
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

## 注意事项

### ⚠️ 重要提醒
1. **代码风格**：必须严格遵循 microsoft/agent-framework 的 .NET 代码风格
2. **自动迁移**：EF Core 必须配置为自动迁移，避免手动 SQL 操作
3. **异步优先**：所有 I/O 操作必须使用异步方法
4. **依赖注入**：使用 ASP.NET Core 的内置 DI 容器
5. **日志记录**：关键操作必须记录日志

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
