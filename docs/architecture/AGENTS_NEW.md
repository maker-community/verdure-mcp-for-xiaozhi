# AGENTS.md

A dedicated guide for AI coding agents working on the Verdure MCP Platform project.

---

## 项目概述 (Project Overview)

这是一个基于 .NET 10 的多租户 SaaS 平台，用于管理和提供 MCP (Model Context Protocol) 服务。

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
- **.NET 10**
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
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="10.0.*" />

<!-- Database -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.*" />

<!-- Blazor & UI -->
<PackageReference Include="MudBlazor" Version="8.*" />
<PackageReference Include="Blazored.LocalStorage" Version="4.5.0" />
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

遵循 eShop 的 Clean Architecture 和 DDD 模式，详见 AGENTS.md 完整版。

---

## AI Agent 提示 (Important Reminders)

当使用此文档时：
- **必须严格遵循 eShop 的架构模式和代码风格**
- **所有命名空间使用 `Verdure` 作为根命名空间**
- **使用仓储模式，服务层和数据访问层分离**
- **使用 Minimal API 而非 Controller**
- **使用 Blazor WebAssembly 而非 Blazor Server**
- **前端使用客户端认证，通过 JWT Token 和 LocalStorage**
- **所有 I/O 操作使用异步方法**
- 生成完整的文件，不要省略代码
- 包含必要的注释和 XML 文档
- 遵循 .NET 最佳实践和设计模式

---

_完整文档请参考原 AGENTS.md 文件_
