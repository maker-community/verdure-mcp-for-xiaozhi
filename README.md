# Verdure MCP Platform

> 开源的多租户 MCP 服务管理平台，为小智 AI 助手提供灵活的 Model Context Protocol 服务配置和管理能力

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-Latest-594AE2)](https://mudblazor.com/)
[![License](https://img.shields.io/github/license/maker-community/verdure-mcp-for-xiaozhi)](./LICENSE)

## 📖 项目介绍

Verdure MCP Platform 是一个基于 .NET 9 和 Blazor WebAssembly 构建的企业级多租户 SaaS 平台，专为小智 AI 助手设计，提供完整的 Model Context Protocol (MCP) 服务管理解决方案。

**核心功能**：
- 🔐 多租户身份认证系统（基于 Keycloak OpenID Connect）
- 🌐 每个用户可配置自己的小智 AI 服务器地址
- 🔗 将不同的 MCP 服务绑定到指定节点
- 🚀 通过 WebSocket 连接提供对应的 MCP 服务
- 💾 仓储模式 (Repository Pattern) 实现数据访问层
- 🗄️ 支持 PostgreSQL 和 SQLite 多数据库

## ✨ 核心特性

### 多租户支持
- 基于 Keycloak 的 OpenID Connect 认证
- 每个用户独立配置 MCP 服务和小智连接
- 完整的权限管理和数据隔离

### 灵活的服务绑定
- 支持多种 MCP 服务配置
- 多种认证方式（Bearer Token、Basic Auth、OAuth2、API Key）
- 动态绑定服务到小智节点

### 现代化技术栈
- .NET 9 后端 API
- Blazor WebAssembly 前端（单页应用）
- MudBlazor UI 组件库（Material Design 3）
- .NET Aspire 云原生应用编排
- Entity Framework Core 9.0

### 架构设计
- 领域驱动设计 (DDD)
- 仓储模式 (Repository Pattern)
- 前后端分离架构
- 依赖注入 (Dependency Injection)
- 完整的国际化 (i18n) 支持

## 🏗️ 项目架构

```
verdure-mcp-for-xiaozhi/
├── src/
│   ├── Verdure.McpPlatform.AppHost/           # Aspire 应用宿主
│   ├── Verdure.McpPlatform.ServiceDefaults/   # 共享服务配置
│   ├── Verdure.McpPlatform.Api/               # Web API 项目
│   ├── Verdure.McpPlatform.Web/               # Blazor WebAssembly 前端
│   ├── Verdure.McpPlatform.Domain/            # 领域层
│   ├── Verdure.McpPlatform.Infrastructure/    # 基础设施层
│   ├── Verdure.McpPlatform.Application/       # 应用服务层
│   └── Verdure.McpPlatform.Contracts/         # 共享契约
└── tests/                                      # 测试项目
```

详细架构说明请参考 [AGENTS.md](./AGENTS.md)

## 🚀 快速开始

### 前置要求

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) 或 SQLite
- [Keycloak](https://www.keycloak.org/) (可选，用于 OpenID Connect 认证)

### 安装步骤

1. **克隆仓库**
```bash
git clone https://github.com/maker-community/verdure-mcp-for-xiaozhi.git
cd verdure-mcp-for-xiaozhi
```

2. **恢复依赖**
```bash
dotnet restore
```

3. **配置数据库**

编辑 `src/Verdure.McpPlatform.Api/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "mcpdb": "Host=localhost;Database=verdure_mcp;Username=postgres;Password=your_password"
  }
}
```

4. **运行应用（通过 Aspire）**
```bash
dotnet run --project src/Verdure.McpPlatform.AppHost
```

或单独运行各服务：
```bash
# 运行 API
dotnet run --project src/Verdure.McpPlatform.Api

# 运行 Web 前端
dotnet run --project src/Verdure.McpPlatform.Web
```

5. **访问应用**
- Web UI: https://localhost:5001
- API: https://localhost:5000
- Aspire Dashboard: https://localhost:17181

## 📚 文档

- [架构指南](./AGENTS.md) - 详细的架构设计和开发指南
- [部署指南](./DEPLOYMENT.md) - 生产环境部署说明
- [API 文档](./API_EXAMPLES.md) - API 使用示例
- [前端改进](./FRONTEND_IMPROVEMENTS.md) - 最新的前端更新说明

## 🎯 使用流程

1. **登录系统** - 使用 Keycloak 账号登录平台
2. **配置小智连接** - 添加你的小智 AI 服务器地址
3. **创建 MCP 服务** - 配置 MCP 服务，支持多种认证方式
4. **绑定服务到节点** - 将 MCP 服务绑定到小智节点，开始使用

## 🌐 社区与支持

### GitHub 创客社区
- 组织地址: [https://github.com/maker-community](https://github.com/maker-community)
- 项目仓库: [https://github.com/maker-community/verdure-mcp-for-xiaozhi](https://github.com/maker-community/verdure-mcp-for-xiaozhi)

### B站 UP主
- **绿荫阿广**
- B站主页: [https://space.bilibili.com/25228512](https://space.bilibili.com/25228512)
- 关注我获取更多 AI 和硬件创客教程

### QQ 交流群
- **绿荫DIY硬件交流群**
- 群号: **1023487000**
- 欢迎加入讨论 AI、MCP 和硬件 DIY

## 🤝 贡献

欢迎贡献代码、报告问题或提出建议！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 开源协议

本项目采用 [MIT License](./LICENSE) 开源协议。

## 🙏 致谢

- [Microsoft .NET](https://dotnet.microsoft.com/) - 强大的跨平台框架
- [MudBlazor](https://mudblazor.com/) - 优秀的 Blazor UI 组件库
- [Keycloak](https://www.keycloak.org/) - 开源的身份认证解决方案
- [Model Context Protocol](https://modelcontextprotocol.io/) - AI 上下文协议标准

---

Made with ❤️ by [绿荫阿广](https://space.bilibili.com/25228512) and the [Maker Community](https://github.com/maker-community)
