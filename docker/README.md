# Verdure MCP Platform - 本地开发环境快速启动

本指南帮助你在本地快速启动完整的 Verdure MCP Platform 开发环境，包括：
- PostgreSQL 数据库
- Redis 缓存
- Keycloak 身份认证服务器
- Verdure MCP Platform 应用程序

## 📋 前置要求

- **Docker Desktop** (Windows/Mac) 或 **Docker** + **Docker Compose** (Linux)
- **PowerShell** 5.1+ (Windows) 或 **PowerShell Core** 7+ (跨平台)
- 至少 4GB 可用内存
- 至少 5GB 可用磁盘空间

## 🚀 快速启动

### 1️⃣ 一键启动所有服务

```powershell
# 进入项目根目录
cd c:\github-verdure\verdure-mcp-for-xiaozhi

# 运行启动脚本
.\scripts\start-local.ps1
```

### 2️⃣ 访问应用

脚本运行完成后，你可以访问：

| 服务 | URL | 凭据 |
|------|-----|------|
| **应用主页** | http://localhost:5241 | 使用下面的演示用户 |
| **Keycloak 管理** | http://localhost:8080 | admin / admin |
| **健康检查** | http://localhost:5241/api/health | - |

### 3️⃣ 演示用户账号

系统已预置两个演示用户：

| 用户名 | 密码 | 角色 |
|--------|------|------|
| **admin** | admin123 | 管理员 + 普通用户 |
| **demo** | demo123 | 普通用户 |

## 🏗️ 构建 Docker 镜像

如需本地构建 Docker 镜像，请在项目根目录执行以下命令：

### 构建命令

```powershell
# 进入项目根目录
cd c:\github-verdure\verdure-mcp-for-xiaozhi

# 构建镜像（使用 docker-compose.single-image.yml 中定义的镜像名称）
docker build -t gilzhang/verdure-mcp-platform:alpine-v1.0.9 -f docker/Dockerfile.single-image .
```

### 构建说明

| 项目 | 说明 |
|------|------|
| **基础镜像** | `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (Alpine Linux，镜像更小) |
| **构建镜像** | `mcr.microsoft.com/dotnet/sdk:10.0` |
| **最终镜像大小** | ~250MB |
| **构建时间** | 首次约 3-5 分钟，后续使用缓存更快 |

### 自定义构建

```powershell
# 使用自定义标签
docker build -t my-registry/verdure-mcp:latest -f docker/Dockerfile.single-image .

# 使用 Debug 配置构建
docker build --build-arg BUILD_CONFIGURATION=Debug -t gilzhang/verdure-mcp-platform:debug -f docker/Dockerfile.single-image .

# 不使用缓存（完全重新构建）
docker build --no-cache -t gilzhang/verdure-mcp-platform:alpine-v1.0.9 -f docker/Dockerfile.single-image .
```

### 推送到 Docker Hub

```powershell
# 登录 Docker Hub
docker login

# 推送镜像
docker push gilzhang/verdure-mcp-platform:alpine-v1.0.9
```

## 🛠️ 常用命令

### 停止所有服务

```powershell
.\scripts\stop-local.ps1
```

### 查看日志

```powershell
# 查看所有服务日志
.\scripts\view-logs.ps1

# 查看应用日志
.\scripts\view-logs.ps1 -Service app

# 查看 Keycloak 日志
.\scripts\view-logs.ps1 -Service keycloak

# 只显示最近 50 行
.\scripts\view-logs.ps1 -Service app -Tail 50

# 不跟随日志（只显示一次）
.\scripts\view-logs.ps1 -Service app -Follow:$false
```

### 重启应用

```powershell
cd docker
docker-compose -f docker-compose.single-image.yml restart app
```

### 重新构建应用

```powershell
cd docker
docker-compose -f docker-compose.single-image.yml build --no-cache app
docker-compose -f docker-compose.single-image.yml up -d app
```

### 完全清理（包括数据）

```powershell
cd docker
docker-compose -f docker-compose.single-image.yml down -v
```

⚠️ **警告**：这会删除所有数据库数据和 Keycloak 配置！

## 🔧 配置

### 修改默认配置

1. 编辑 `docker/.env` 文件
2. 重启服务：
   ```powershell
   .\scripts\stop-local.ps1
   .\scripts\start-local.ps1
   ```

### 主要配置项

```bash
# PostgreSQL 密码
POSTGRES_PASSWORD=postgres

# Keycloak 管理员凭据
KEYCLOAK_ADMIN=admin
KEYCLOAK_ADMIN_PASSWORD=admin

# 应用环境
ASPNETCORE_ENVIRONMENT=Production
```

## 🌐 使用 IP 地址访问（非 localhost）

> ⚠️ **重要提示**：如果你需要从其他设备（如手机、其他电脑）访问服务，或者在服务器上部署，则**不能**使用 `localhost`，必须使用服务器/电脑的实际 IP 地址。

### 📝 配置步骤

假设你的服务器/电脑 IP 地址是 `192.168.1.100`（请替换为你的实际 IP）：

#### 步骤 1：准备 .env 文件

首先确保 `.env` 文件存在。如果不存在，从模板复制：

```powershell
# 在项目根目录执行
Copy-Item docker\.env.example docker\.env
```

#### 步骤 2：修改 .env 文件

编辑 `docker/.env` 文件，找到 `Oidc_Authority` 配置项：

```bash
# ❌ 原配置（仅限 localhost 访问）
Oidc_Authority=http://keycloak:8080

# ✅ 修改为你的 IP 地址
Oidc_Authority=http://192.168.1.100:8080
```

> 💡 **说明**：`keycloak` 是 Docker 内部网络的服务名，只有在容器内部才能解析。浏览器无法识别这个地址，所以需要改成实际 IP。

#### 步骤 3：修改 appsettings.json

编辑 `docker/config/appsettings.json` 文件，将 `localhost` 改为你的 IP：

```jsonc
{
  "ApiBaseAddress": "",
  "Oidc": {
    // ❌ 原配置
    // "Authority": "http://localhost:8080/realms/maker-community",
    
    // ✅ 修改为你的 IP 地址
    "Authority": "http://192.168.1.100:8080/realms/maker-community",
    "ClientId": "verdure-mcp",
    "ResponseType": "code",
    "PostLogoutRedirectUri": ""
  }
}
```

#### 步骤 4：启动服务

```powershell
.\scripts\start-local.ps1
```

#### 步骤 5：访问应用

现在可以从任何设备通过 IP 访问：

| 服务 | URL |
|------|-----|
| **应用主页** | http://192.168.1.100:5241 |
| **Keycloak 管理** | http://192.168.1.100:8080 |

### ⚠️ 常见错误

| 错误现象 | 原因 | 解决方法 |
|----------|------|----------|
| 登录后一直转圈/无法跳转 | `.env` 中的 IP 与 `appsettings.json` 中的 IP 不一致 | 确保两个文件使用相同的 IP |
| 页面显示 "无法连接到 keycloak" | `.env` 仍使用 `keycloak` 而非 IP | 修改 `.env` 中的 `Oidc_Authority` |
| 401 未授权错误 | Token 签发地址与验证地址不匹配 | 重启所有服务，确保配置一致 |

### 🔄 切换回 localhost 模式

如果要切换回 localhost 模式，需要同时还原两个配置文件：

#### 步骤 1：停止服务

```powershell
.\scripts\stop-local.ps1
```

#### 步骤 2：删除 .env 文件

```powershell
# 删除 .env 文件，脚本会自动重新生成默认配置
Remove-Item docker\.env
```

#### 步骤 3：还原 appsettings.json

编辑 `docker/config/appsettings.json`，将 IP 地址改回 `localhost`：

```jsonc
{
  "ApiBaseAddress": "",
  "Oidc": {
    // ✅ 改回 localhost
    "Authority": "http://localhost:8080/realms/maker-community",
    "ClientId": "verdure-mcp",
    "ResponseType": "code",
    "PostLogoutRedirectUri": ""
  }
}
```

#### 步骤 4：重新启动

```powershell
.\scripts\start-local.ps1
```

> ⚠️ **注意**：如果只删除 `.env` 而不修改 `appsettings.json`，会导致认证失败（`.env` 使用 `keycloak` 服务名，而 `appsettings.json` 仍使用 IP 地址，两者不匹配）。

## 📊 服务端口

| 服务 | 端口 | 说明 |
|------|------|------|
| 应用 | 5241 | Web UI + API |
| Keycloak | 8080 | 身份认证服务 |
| PostgreSQL | 5432 | 数据库 |
| Redis | 6379 | 缓存 |

## 🗄️ 数据库

PostgreSQL 自动创建了三个数据库：

- `verdure_mcp` - MCP 平台主数据库
- `verdure_identity` - 用户身份数据库
- `verdure_keycloak` - Keycloak 数据库

### 连接数据库

```bash
# 使用 psql
docker exec -it verdure-postgres psql -U postgres -d verdure_mcp

# 或使用 pgAdmin
# 访问 http://localhost:5050 (如果启用了 pgAdmin)
```

## 🔐 Keycloak 配置

### Realm 信息

- **Realm 名称**: `verdure-mcp`
- **Client ID**: `verdure-mcp-api`
- **自动导入**: 首次启动时自动导入配置

### 自定义 Keycloak

如需修改 Keycloak 配置：

1. 编辑 `docker/config/keycloak/verdure-mcp-realm.json`
2. 删除现有容器和数据：
   ```powershell
   cd docker
   docker-compose -f docker-compose.single-image.yml down -v
   ```
3. 重新启动：
   ```powershell
   .\scripts\start-local.ps1
   ```

## 🧪 测试多实例部署

启动多个应用实例（用于测试分布式功能）：

```powershell
cd docker

# 启动第一个实例
docker-compose -f docker-compose.single-image.yml up -d --scale app=3
```

这将启动 3 个应用实例，它们会通过 Redis 协调 WebSocket 连接。

## 🐛 故障排查

### 容器启动失败

```powershell
# 查看容器状态
cd docker
docker-compose -f docker-compose.single-image.yml ps

# 查看错误日志
.\scripts\view-logs.ps1 -Service app
```

### Keycloak 连接失败

1. 确认 Keycloak 已完全启动（可能需要 1-2 分钟）
2. 查看 Keycloak 日志：
   ```powershell
   .\scripts\view-logs.ps1 -Service keycloak
   ```

### 数据库连接失败

```powershell
# 检查 PostgreSQL 健康状态
docker exec verdure-postgres pg_isready -U postgres

# 查看数据库日志
.\scripts\view-logs.ps1 -Service postgres
```

### 端口冲突

如果端口被占用，可以修改 `docker/docker-compose.single-image.yml` 中的端口映射：

```yaml
ports:
  - "8080:8080"  # 改为 "8888:8080"
```

## 📚 更多资源

- [完整文档](../docs/README.md)
- [API 使用示例](../docs/guides/API_EXAMPLES.md)
- [架构设计](../docs/architecture/)
- [部署指南](../docs/guides/DEPLOYMENT.md)

## 💡 提示

- 首次启动需要下载镜像和构建应用，可能需要 5-10 分钟
- 数据持久化在 Docker volumes 中，停止容器不会丢失数据
- 如需完全重置环境，使用 `docker-compose down -v`
- Keycloak 启动较慢（60-90 秒），请耐心等待

## 🆘 获取帮助

如遇到问题：

1. 查看日志：`.\scripts\view-logs.ps1`
2. 检查容器状态：`docker-compose -f docker/docker-compose.single-image.yml ps`
3. 提交 Issue：[GitHub Issues](https://github.com/maker-community/verdure-mcp-for-xiaozhi/issues)
