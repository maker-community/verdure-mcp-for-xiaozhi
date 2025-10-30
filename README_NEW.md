# Verdure MCP Platform - 为小智AI提供的MCP服务平台

一个基于 .NET 9 的多租户 SaaS 平台，用于管理和提供 MCP (Model Context Protocol) 服务。

## 🌟 核心功能

- ✅ **多用户身份认证系统** - 支持 ASP.NET Core Identity 和 Keycloak OpenID Connect
- ✅ **MCP 服务器管理** - 每个用户可配置自己的小智 AI 服务器地址
- ✅ **服务绑定管理** - 将不同的 MCP 服务绑定到指定节点
- ✅ **RESTful API** - 完整的 API 用于管理服务器和绑定
- ✅ **Blazor WebAssembly 前端** - 现代化的单页应用界面
- ✅ **多数据库支持** - PostgreSQL 和 SQLite
- ✅ **.NET Aspire 编排** - 云原生应用编排

## 🏗️ 架构

本项目采用 **Clean Architecture** 和 **领域驱动设计 (DDD)** 模式：

```
Verdure.McpPlatform/
├── src/
│   ├── Verdure.McpPlatform.Domain/              # 领域层
│   │   ├── AggregatesModel/                      # 聚合根
│   │   ├── SeedWork/                             # DDD 基础设施
│   │   └── Exceptions/                           # 领域异常
│   ├── Verdure.McpPlatform.Infrastructure/       # 基础设施层
│   │   ├── Data/                                 # 数据访问
│   │   ├── Repositories/                         # 仓储实现
│   │   └── Identity/                             # 身份认证
│   ├── Verdure.McpPlatform.Application/          # 应用层
│   │   └── Services/                             # 应用服务
│   ├── Verdure.McpPlatform.Contracts/            # 共享契约
│   │   ├── DTOs/                                 # 数据传输对象
│   │   └── Requests/                             # 请求模型
│   ├── Verdure.McpPlatform.Api/                  # Web API
│   │   ├── Apis/                                 # Minimal API 端点
│   │   ├── Extensions/                           # 扩展方法
│   │   └── Services/                             # API 服务
│   ├── Verdure.McpPlatform.Web/                  # Blazor WebAssembly
│   ├── Verdure.McpPlatform.ServiceDefaults/      # 共享服务配置
│   └── Verdure.McpPlatform.AppHost/              # Aspire 应用宿主
└── tests/
```

## 🚀 快速开始

### 前置要求

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (用于 PostgreSQL，可选)
- Visual Studio 2022 或 VS Code

### 运行项目

#### 方式 1: 使用 .NET Aspire (推荐)

这是最简单的方式，会自动启动所有服务和依赖：

```bash
# 克隆仓库
git clone https://github.com/maker-community/verdure-mcp-for-xiaozhi.git
cd verdure-mcp-for-xiaozhi

# 还原依赖
dotnet restore

# 运行 Aspire AppHost
dotnet run --project src/Verdure.McpPlatform.AppHost
```

Aspire 会自动：
- 启动 PostgreSQL 容器
- 启动 API 服务
- 启动 Web 前端
- 配置服务发现和健康检查

访问 Aspire Dashboard (通常在 http://localhost:15000) 查看所有服务状态。

#### 方式 2: 单独运行 API 和 Web

如果不使用 Aspire，可以单独运行各个项目：

```bash
# 运行 API (会使用 SQLite)
dotnet run --project src/Verdure.McpPlatform.Api

# 在另一个终端运行 Web
dotnet run --project src/Verdure.McpPlatform.Web
```

- API: https://localhost:5001
- Swagger UI: https://localhost:5001/swagger
- Web: https://localhost:5002

## 📚 API 文档

### MCP Server 管理 API

| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/mcp-servers` | 获取当前用户的所有服务器 |
| GET | `/api/mcp-servers/{id}` | 获取特定服务器详情 |
| POST | `/api/mcp-servers` | 创建新服务器 |
| PUT | `/api/mcp-servers/{id}` | 更新服务器信息 |
| DELETE | `/api/mcp-servers/{id}` | 删除服务器 |

### MCP Binding 管理 API

| 方法 | 端点 | 描述 |
|------|------|------|
| GET | `/api/mcp-bindings/server/{serverId}` | 获取服务器的所有绑定 |
| GET | `/api/mcp-bindings/active` | 获取所有活跃绑定 |
| POST | `/api/mcp-bindings` | 创建新绑定 |
| PUT | `/api/mcp-bindings/{id}` | 更新绑定信息 |
| PUT | `/api/mcp-bindings/{id}/activate` | 激活绑定 |
| PUT | `/api/mcp-bindings/{id}/deactivate` | 停用绑定 |
| DELETE | `/api/mcp-bindings/{id}` | 删除绑定 |

### 请求示例

#### 创建 MCP Server

```json
POST /api/mcp-servers
Content-Type: application/json

{
  "name": "My XiaoZhi Server",
  "address": "https://xiaozhi.example.com",
  "description": "Production server for XiaoZhi AI"
}
```

#### 创建 MCP Binding

```json
POST /api/mcp-bindings
Content-Type: application/json

{
  "serviceName": "Calculator Service",
  "nodeAddress": "ws://localhost:3000/mcp",
  "serverId": 1,
  "description": "Math calculation service"
}
```

## 🗄️ 数据库

### 使用 PostgreSQL (生产环境推荐)

通过 Aspire 自动配置，或手动配置连接字符串：

```json
{
  "ConnectionStrings": {
    "mcpdb": "Host=localhost;Database=verdure_mcp;Username=postgres;Password=postgres",
    "identitydb": "Host=localhost;Database=verdure_identity;Username=postgres;Password=postgres"
  }
}
```

### 使用 SQLite (开发环境)

如果未配置连接字符串，系统会自动使用 SQLite：
- `mcpplatform.db` - MCP 数据
- `identity.db` - 用户身份数据

数据库会在首次运行时自动创建。

## 🔐 身份认证

### ASP.NET Core Identity

默认配置使用 ASP.NET Core Identity，支持：
- 用户注册和登录
- 密码管理
- 角色和权限

### OpenID Connect / Keycloak (可选)

支持与 Keycloak 集成，配置示例：

```json
{
  "Identity": {
    "Url": "http://localhost:8080/realms/verdure",
    "ClientId": "verdure-mcp-platform",
    "ClientSecret": "your-client-secret"
  }
}
```

## 🛠️ 开发指南

### 添加新功能

1. **定义领域模型** (`Domain/AggregatesModel/`)
2. **创建仓储接口** (`Domain/AggregatesModel/{Aggregate}/`)
3. **实现仓储** (`Infrastructure/Repositories/`)
4. **创建应用服务** (`Application/Services/`)
5. **添加 API 端点** (`Api/Apis/`)
6. **创建前端页面** (`Web/Pages/`)

### 构建和测试

```bash
# 构建解决方案
dotnet build

# 运行测试 (如果有)
dotnet test

# 发布
dotnet publish -c Release
```

## 📦 部署

### Docker 部署

创建 `docker-compose.yml`:

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
  
  api:
    image: verdure-mcp-api:latest
    environment:
      - ConnectionStrings__mcpdb=Host=postgres;Database=verdure_mcp
    depends_on:
      - postgres
    ports:
      - "5000:8080"
  
  web:
    image: verdure-mcp-web:latest
    depends_on:
      - api
    ports:
      - "5001:8080"
```

### Azure 部署

支持部署到：
- Azure App Service
- Azure Container Apps
- Azure Kubernetes Service (AKS)

## 🤝 贡献

欢迎提交 Pull Request！请确保：
1. 遵循现有代码风格
2. 添加必要的测试
3. 更新相关文档

## 📝 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🔗 相关链接

- [.NET 9 文档](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9)
- [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
- [Model Context Protocol](https://github.com/modelcontextprotocol)
- [MudBlazor](https://mudblazor.com/)

## 📧 联系方式

如有问题或建议，请提交 Issue 或联系维护者。

---

**注意**: 这是一个实验性项目，用于为小智AI提供MCP服务支持。
