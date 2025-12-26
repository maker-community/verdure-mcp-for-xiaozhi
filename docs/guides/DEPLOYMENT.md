# 部署指南

## 本地开发环境部署

### 1. 克隆项目

```bash
git clone https://github.com/maker-community/verdure-mcp-for-xiaozhi.git
cd verdure-mcp-for-xiaozhi
```

### 2. 安装依赖

确保已安装:
- .NET 10 SDK
- Docker Desktop (用于 PostgreSQL)

### 3. 配置数据库

#### 选项 A: 使用 Aspire (推荐)

Aspire 会自动管理 PostgreSQL 容器：

```bash
dotnet run --project src/Verdure.McpPlatform.AppHost
```

#### 选项 B: 手动配置 PostgreSQL

```bash
# 启动 PostgreSQL
docker run -d \
  --name verdure-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:16

# 在 appsettings.Development.json 中配置连接字符串
{
  "ConnectionStrings": {
    "mcpdb": "Host=localhost;Database=verdure_mcp;Username=postgres;Password=postgres",
    "identitydb": "Host=localhost;Database=verdure_identity;Username=postgres;Password=postgres"
  }
}
```

#### 选项 C: 使用 SQLite (最简单)

无需配置，系统会自动创建 SQLite 数据库文件。

### 4. 运行项目

```bash
# 使用 Aspire
dotnet run --project src/Verdure.McpPlatform.AppHost

# 或分别运行
dotnet run --project src/Verdure.McpPlatform.Api
dotnet run --project src/Verdure.McpPlatform.Web
```

## 生产环境部署

### Docker 部署

#### 1. 构建镜像

```bash
# 构建 API 镜像
docker build -t verdure-mcp-api -f src/Verdure.McpPlatform.Api/Dockerfile .

# 构建 Web 镜像
docker build -t verdure-mcp-web -f src/Verdure.McpPlatform.Web/Dockerfile .
```

#### 2. 创建 Dockerfile (如果不存在)

**API Dockerfile**:

```dockerfile
# src/Verdure.McpPlatform.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Verdure.McpPlatform.Api/Verdure.McpPlatform.Api.csproj", "src/Verdure.McpPlatform.Api/"]
COPY ["src/Verdure.McpPlatform.Application/Verdure.McpPlatform.Application.csproj", "src/Verdure.McpPlatform.Application/"]
COPY ["src/Verdure.McpPlatform.Infrastructure/Verdure.McpPlatform.Infrastructure.csproj", "src/Verdure.McpPlatform.Infrastructure/"]
COPY ["src/Verdure.McpPlatform.Domain/Verdure.McpPlatform.Domain.csproj", "src/Verdure.McpPlatform.Domain/"]
COPY ["src/Verdure.McpPlatform.Contracts/Verdure.McpPlatform.Contracts.csproj", "src/Verdure.McpPlatform.Contracts/"]
COPY ["src/Verdure.McpPlatform.ServiceDefaults/Verdure.McpPlatform.ServiceDefaults.csproj", "src/Verdure.McpPlatform.ServiceDefaults/"]

RUN dotnet restore "src/Verdure.McpPlatform.Api/Verdure.McpPlatform.Api.csproj"
COPY . .
WORKDIR "/src/src/Verdure.McpPlatform.Api"
RUN dotnet build "Verdure.McpPlatform.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Verdure.McpPlatform.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Verdure.McpPlatform.Api.dll"]
```

**Web Dockerfile**:

```dockerfile
# src/Verdure.McpPlatform.Web/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Verdure.McpPlatform.Web/Verdure.McpPlatform.Web.csproj", "src/Verdure.McpPlatform.Web/"]
COPY ["src/Verdure.McpPlatform.Contracts/Verdure.McpPlatform.Contracts.csproj", "src/Verdure.McpPlatform.Contracts/"]

RUN dotnet restore "src/Verdure.McpPlatform.Web/Verdure.McpPlatform.Web.csproj"
COPY . .
WORKDIR "/src/src/Verdure.McpPlatform.Web"
RUN dotnet build "Verdure.McpPlatform.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Verdure.McpPlatform.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY src/Verdure.McpPlatform.Web/nginx.conf /etc/nginx/nginx.conf
EXPOSE 8080
```

#### 3. 使用 Docker Compose

创建 `docker-compose.yml`:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16
    container_name: verdure-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-postgres}
      POSTGRES_DB: verdure_mcp
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    image: verdure-mcp-api:latest
    container_name: verdure-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
      - ConnectionStrings__mcpdb=Host=postgres;Database=verdure_mcp;Username=postgres;Password=${POSTGRES_PASSWORD:-postgres}
      - ConnectionStrings__identitydb=Host=postgres;Database=verdure_identity;Username=postgres;Password=${POSTGRES_PASSWORD:-postgres}
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  web:
    image: verdure-mcp-web:latest
    container_name: verdure-web
    environment:
      - ApiBaseAddress=http://api:8080
    ports:
      - "5001:8080"
    depends_on:
      - api

volumes:
  postgres_data:
```

运行:

```bash
docker-compose up -d
```

### Azure App Service 部署

#### 1. 准备应用

```bash
# 发布 API
dotnet publish src/Verdure.McpPlatform.Api -c Release -o ./publish/api

# 发布 Web
dotnet publish src/Verdure.McpPlatform.Web -c Release -o ./publish/web
```

#### 2. 部署到 Azure

```bash
# 创建资源组
az group create --name verdure-mcp-rg --location eastus

# 创建 App Service 计划
az appservice plan create \
  --name verdure-mcp-plan \
  --resource-group verdure-mcp-rg \
  --sku B1 \
  --is-linux

# 创建 Web App for API
az webapp create \
  --resource-group verdure-mcp-rg \
  --plan verdure-mcp-plan \
  --name verdure-mcp-api \
  --runtime "DOTNETCORE:9.0"

# 部署 API
az webapp deploy \
  --resource-group verdure-mcp-rg \
  --name verdure-mcp-api \
  --src-path ./publish/api.zip \
  --type zip

# 配置连接字符串 (Azure Database for PostgreSQL)
az webapp config connection-string set \
  --resource-group verdure-mcp-rg \
  --name verdure-mcp-api \
  --connection-string-type SQLAzure \
  --settings mcpdb="Host=yourserver.postgres.database.azure.com;Database=verdure_mcp;Username=admin@yourserver;Password=yourpassword"
```

### Kubernetes 部署

#### 1. 创建 Kubernetes 清单

**api-deployment.yaml**:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: verdure-mcp-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: verdure-mcp-api
  template:
    metadata:
      labels:
        app: verdure-mcp-api
    spec:
      containers:
      - name: api
        image: verdure-mcp-api:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__mcpdb
          valueFrom:
            secretKeyRef:
              name: verdure-secrets
              key: mcpdb
---
apiVersion: v1
kind: Service
metadata:
  name: verdure-mcp-api-service
spec:
  selector:
    app: verdure-mcp-api
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

#### 2. 部署

```bash
kubectl apply -f api-deployment.yaml
kubectl apply -f web-deployment.yaml
```

## 环境配置

### 生产环境配置示例

**appsettings.Production.json**:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "mcpdb": "Host=${DB_HOST};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}",
    "identitydb": "Host=${DB_HOST};Database=${IDENTITY_DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"
  },
  "AllowedHosts": "*",
  "Identity": {
    "Url": "${KEYCLOAK_URL}",
    "ClientId": "${CLIENT_ID}",
    "ClientSecret": "${CLIENT_SECRET}"
  }
}
```

### 环境变量

| 变量名 | 描述 | 示例 |
|--------|------|------|
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | `Production` |
| `DB_HOST` | 数据库主机 | `postgres.example.com` |
| `DB_NAME` | 数据库名称 | `verdure_mcp` |
| `DB_USER` | 数据库用户 | `postgres` |
| `DB_PASSWORD` | 数据库密码 | `SecurePassword123!` |
| `KEYCLOAK_URL` | Keycloak 地址 | `https://auth.example.com/realms/verdure` |

## 监控和日志

### 健康检查

- API健康检查: `GET /health`
- 存活检查: `GET /alive`

### 日志

使用 Application Insights (Azure) 或其他日志聚合工具：

```json
{
  "ApplicationInsights": {
    "ConnectionString": "${APPINSIGHTS_CONNECTION_STRING}"
  }
}
```

## 故障排除

### 常见问题

1. **数据库连接失败**
   - 检查连接字符串
   - 确保数据库服务正在运行
   - 检查防火墙规则

2. **CORS 错误**
   - 检查 CORS 策略配置
   - 确保前端和 API 域名正确配置

3. **身份验证失败**
   - 验证 Identity 配置
   - 检查 Keycloak 配置(如果使用)

### 日志查看

```bash
# Docker
docker logs verdure-api

# Kubernetes
kubectl logs deployment/verdure-mcp-api

# Azure
az webapp log tail --name verdure-mcp-api --resource-group verdure-mcp-rg
```

## 备份和恢复

### PostgreSQL 备份

```bash
# 备份
docker exec verdure-postgres pg_dump -U postgres verdure_mcp > backup.sql

# 恢复
docker exec -i verdure-postgres psql -U postgres verdure_mcp < backup.sql
```

## 性能优化

1. **启用响应压缩**
2. **使用 Redis 缓存**
3. **配置连接池**
4. **启用静态文件CDN**

## 安全建议

1. 使用 HTTPS
2. 定期更新依赖
3. 使用强密码策略
4. 启用速率限制
5. 实施 IP 白名单(如需要)
