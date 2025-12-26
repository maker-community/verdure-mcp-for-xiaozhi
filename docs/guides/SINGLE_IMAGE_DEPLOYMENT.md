# 单镜像部署指南 (Single Image Deployment Guide)

本指南介绍如何将 Blazor WebAssembly 前端和 ASP.NET Core API 合并到单个 Docker 镜像中部署。

## 📋 架构概述

### 传统架构（双镜像）
```
┌─────────────┐      ┌─────────────┐
│  Web 镜像    │      │  API 镜像    │
│  (Nginx)    │─────▶│  (ASP.NET)  │
│  静态文件    │      │  API 端点    │
└─────────────┘      └─────────────┘
    Port 80             Port 5000
```

### 单镜像架构（推荐）
```
┌─────────────────────────────┐
│      API 镜像                │
│  (ASP.NET Core)             │
│                             │
│  ├─ /api/*  → API 端点      │
│  ├─ /*      → 静态文件       │
│  └─ 回退    → index.html    │
└─────────────────────────────┘
       Port 8080
```

## ✅ 优势

1. **简化部署** - 只需管理一个 Docker 镜像
2. **避免 CORS** - 前后端同域，无需复杂的 CORS 配置
3. **统一认证** - Cookie 和 JWT token 共享更简单
4. **减少资源** - 节省一个容器的资源开销
5. **降低复杂度** - 无需 Nginx 反向代理配置

## 🔧 实现原理

### 1. 项目引用
API 项目引用 Web 项目：
```xml
<!-- Verdure.McpPlatform.Api.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Verdure.McpPlatform.Web\Verdure.McpPlatform.Web.csproj" />
</ItemGroup>
```

构建时，Web 项目会自动编译成静态文件并包含到 API 项目的输出中。

### 2. 静态文件服务
API 项目配置提供静态文件：
```csharp
// Program.cs
app.UseBlazorFrameworkFiles();  // Blazor 框架文件
app.UseStaticFiles();            // 静态资源文件
app.MapFallbackToFile("index.html"); // SPA 路由回退
```

### 3. API 路径配置
所有 API 端点使用 `/api` 前缀：
```csharp
app.MapUserApi();                    // /api/users/*
app.MapXiaozhiMcpEndpointApi();      // /api/xiaozhi-mcp-endpoints/*
app.MapMcpServiceConfigApi();        // /api/mcp-service-configs/*
```

### 4. 前端配置
Web 项目使用相对路径调用 API：
```json
{
  "ApiBaseAddress": ""  // 空字符串表示使用当前域名
}
```

## 📦 构建和部署

### 本地开发

```powershell
# 启动开发环境（使用 Aspire）
dotnet run --project src/Verdure.McpPlatform.AppHost

# 访问应用
# http://localhost:<port>  (查看 Aspire Dashboard 获取实际端口)
```

### Docker 构建

#### 方式 1: 使用 Dockerfile

```dockerfile
# 参见 docker/Dockerfile.single-image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Verdure.McpPlatform.sln", "."]
COPY ["src/Verdure.McpPlatform.Api/Verdure.McpPlatform.Api.csproj", "src/Verdure.McpPlatform.Api/"]
COPY ["src/Verdure.McpPlatform.Web/Verdure.McpPlatform.Web.csproj", "src/Verdure.McpPlatform.Web/"]
COPY ["src/Verdure.McpPlatform.Application/Verdure.McpPlatform.Application.csproj", "src/Verdure.McpPlatform.Application/"]
COPY ["src/Verdure.McpPlatform.Domain/Verdure.McpPlatform.Domain.csproj", "src/Verdure.McpPlatform.Domain/"]
COPY ["src/Verdure.McpPlatform.Infrastructure/Verdure.McpPlatform.Infrastructure.csproj", "src/Verdure.McpPlatform.Infrastructure/"]
COPY ["src/Verdure.McpPlatform.Contracts/Verdure.McpPlatform.Contracts.csproj", "src/Verdure.McpPlatform.Contracts/"]
COPY ["src/Verdure.McpPlatform.ServiceDefaults/Verdure.McpPlatform.ServiceDefaults.csproj", "src/Verdure.McpPlatform.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/Verdure.McpPlatform.Api/Verdure.McpPlatform.Api.csproj"

# Copy all source files
COPY . .

# Build Web project first (to generate static files)
WORKDIR "/src/src/Verdure.McpPlatform.Web"
RUN dotnet build "Verdure.McpPlatform.Web.csproj" -c Release -o /app/build

# Build API project (includes Web static files)
WORKDIR "/src/src/Verdure.McpPlatform.Api"
RUN dotnet build "Verdure.McpPlatform.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Verdure.McpPlatform.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Verdure.McpPlatform.Api.dll"]
```

#### 构建命令

```powershell
# 构建镜像
docker build -f docker/Dockerfile.single-image -t verdure-mcp-platform:latest .

# 运行容器
docker run -d \
  --name verdure-mcp \
  -p 8080:8080 \
  -e ConnectionStrings__mcpdb="Host=postgres;Database=verdure_mcp;Username=postgres;Password=yourpassword" \
  -e ConnectionStrings__identitydb="Host=postgres;Database=verdure_identity;Username=postgres;Password=yourpassword" \
  -e ConnectionStrings__redis="redis:6379" \
  -e Identity__Url="https://auth.verdure-hiro.cn/realms/maker-community" \
  -e Identity__Audience="verdure-mcp-api" \
  verdure-mcp-platform:latest

# 查看日志
docker logs -f verdure-mcp

# 访问应用
# http://localhost:8080
```

### Docker Compose 部署

```yaml
# docker-compose.single-image.yml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    container_name: verdure-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-postgres}
      POSTGRES_MULTIPLE_DATABASES: verdure_mcp,verdure_identity
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: verdure-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  app:
    image: verdure-mcp-platform:latest
    container_name: verdure-mcp-app
    build:
      context: .
      dockerfile: docker/Dockerfile.single-image
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__mcpdb=Host=postgres;Database=verdure_mcp;Username=postgres;Password=${POSTGRES_PASSWORD:-postgres}
      - ConnectionStrings__identitydb=Host=postgres;Database=verdure_identity;Username=postgres;Password=${POSTGRES_PASSWORD:-postgres}
      - ConnectionStrings__redis=redis:6379
      - Identity__Url=https://auth.verdure-hiro.cn/realms/maker-community
      - Identity__ClientId=verdure-mcp
      - Identity__Audience=verdure-mcp-api
      - ConnectionMonitor__CheckIntervalSeconds=30
      - ConnectionMonitor__HeartbeatTimeoutSeconds=90
      - ConnectionMonitor__ReconnectCooldownSeconds=60
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/api/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

volumes:
  postgres_data:
  redis_data:
```

#### 启动服务

```powershell
# 启动所有服务
docker-compose -f docker-compose.single-image.yml up -d

# 查看服务状态
docker-compose -f docker-compose.single-image.yml ps

# 查看日志
docker-compose -f docker-compose.single-image.yml logs -f app

# 停止服务
docker-compose -f docker-compose.single-image.yml down

# 停止并删除数据卷
docker-compose -f docker-compose.single-image.yml down -v
```

## 🌐 路由规则

### API 端点
- `/api/users/*` - 用户管理
- `/api/xiaozhi-mcp-endpoints/*` - 小智连接管理
- `/api/mcp-service-configs/*` - MCP 服务配置
- `/api/mcp-service-bindings/*` - 服务绑定
- `/api/health` - 健康检查
- `/openapi/*` - OpenAPI 文档（开发环境）
- `/scalar/*` - Scalar API 文档（开发环境）

### 静态文件
- `/` - index.html (Blazor 应用入口)
- `/_framework/*` - Blazor 框架文件
- `/_content/*` - 组件静态资源
- `/css/*` - 样式文件
- `/js/*` - JavaScript 文件
- 其他所有非 API 路径 → 回退到 index.html（SPA 路由）

## 🔒 生产环境配置

### 环境变量

```bash
# 数据库连接
ConnectionStrings__mcpdb="Host=postgres;Database=verdure_mcp;Username=postgres;Password=<strong-password>"
ConnectionStrings__identitydb="Host=postgres;Database=verdure_identity;Username=postgres;Password=<strong-password>"
ConnectionStrings__redis="redis:6379"

# 身份认证
Identity__Url="https://auth.yourdomain.com/realms/your-realm"
Identity__ClientId="your-client-id"
Identity__ClientSecret="your-client-secret"
Identity__Audience="your-api-audience"

# CORS (如果需要)
AllowedOrigins__0="https://yourdomain.com"

# 连接监控
ConnectionMonitor__CheckIntervalSeconds=30
ConnectionMonitor__HeartbeatTimeoutSeconds=90
ConnectionMonitor__ReconnectCooldownSeconds=60

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

### 安全建议

1. **使用 HTTPS**: 生产环境必须配置 HTTPS
2. **强密码**: 数据库使用强密码
3. **环境变量**: 敏感信息使用环境变量或 Secret 管理
4. **限制端口暴露**: 只暴露必要的端口
5. **健康检查**: 配置 liveness 和 readiness 探针
6. **资源限制**: 设置 CPU 和内存限制

## 📊 监控和日志

### 健康检查

```bash
# 检查应用健康状态
curl http://localhost:8080/api/health

# 预期响应
{
  "status": "healthy",
  "timestamp": "2024-01-09T10:30:00Z"
}
```

### 日志查看

```powershell
# Docker 日志
docker logs -f verdure-mcp-app

# Docker Compose 日志
docker-compose -f docker-compose.single-image.yml logs -f app

# 只看最近 100 行
docker logs --tail 100 verdure-mcp-app
```

## 🚀 Kubernetes 部署

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: verdure-mcp-platform
spec:
  replicas: 3
  selector:
    matchLabels:
      app: verdure-mcp
  template:
    metadata:
      labels:
        app: verdure-mcp
    spec:
      containers:
      - name: app
        image: verdure-mcp-platform:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__mcpdb
          valueFrom:
            secretKeyRef:
              name: verdure-secrets
              key: mcpdb-connection
        - name: ConnectionStrings__redis
          value: "redis-service:6379"
        livenessProbe:
          httpGet:
            path: /api/health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /api/health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
---
apiVersion: v1
kind: Service
metadata:
  name: verdure-mcp-service
spec:
  selector:
    app: verdure-mcp
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

## 🔄 回滚到双镜像模式

如果需要回滚到独立的前后端部署：

1. 恢复 `AppHost.cs` 中的 Web 项目配置
2. 从 `Verdure.McpPlatform.Api.csproj` 移除 Web 项目引用
3. 从 `Program.cs` 移除 `UseBlazorFrameworkFiles` 和 `MapFallbackToFile`
4. 恢复 `appsettings.json` 中的 `ApiBaseAddress` 为完整 URL

## 📝 注意事项

1. **首次加载**: Blazor WASM 需要下载较大的 .NET 运行时，首次加载可能较慢
2. **缓存策略**: 生产环境建议配置 CDN 和浏览器缓存
3. **压缩**: 启用 Brotli 或 Gzip 压缩减少传输大小
4. **PWA**: 可以配置 PWA 实现离线支持和更快的加载速度

## 🆘 故障排查

### 问题 1: 前端页面显示空白

**原因**: 静态文件未正确包含

**解决**:
```powershell
# 检查构建输出
dotnet build src/Verdure.McpPlatform.Api -c Release
# 检查 bin/Release/net10.0/wwwroot 目录是否包含 _framework 文件夹
```

### 问题 2: API 调用 404

**原因**: API 路径配置错误或缺少 `/api` 前缀

**解决**: 确认所有 API 端点都有 `/api` 前缀

### 问题 3: 认证失败

**原因**: OIDC 配置中的重定向 URI 不正确

**解决**: 更新 Keycloak 客户端配置，添加正确的重定向 URI

## 📚 参考资料

- [ASP.NET Core 托管 Blazor WebAssembly](https://learn.microsoft.com/aspnet/core/blazor/host-and-deploy/webassembly)
- [Docker 最佳实践](https://docs.docker.com/develop/dev-best-practices/)
- [Kubernetes 部署指南](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/)
