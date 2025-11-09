# 自定义配置指南 (Custom Configuration Guide)

本指南说明如何在不重新构建 Docker 镜像的情况下自定义 Blazor WebAssembly 前端配置。

## 📝 问题背景

Verdure MCP Platform 的前端配置文件 `appsettings.json` 位于 `/app/wwwroot/appsettings.json`，用于配置：
- API 基地址
- OpenID Connect (OIDC) 认证设置
- 客户端 ID 和授权服务器地址

构建时，ASP.NET Core 会自动生成预压缩版本：
- `appsettings.json.br` (Brotli 压缩)
- `appsettings.json.gz` (Gzip 压缩)

浏览器请求时，服务器优先返回压缩版本以提高性能。

## ⚠️ 挂载配置文件的问题

如果直接使用 Docker volume 挂载 `appsettings.json`：
```yaml
volumes:
  - ./config/appsettings.json:/app/wwwroot/appsettings.json:ro
```

**问题**：浏览器可能会收到**旧的压缩文件** (`.br` 或 `.gz`)，而不是你挂载的新配置！

## ✅ 解决方案

我们的 Docker 镜像包含一个智能 entrypoint 脚本，会自动处理这个问题：

1. 检测到挂载的 `appsettings.json`
2. 计算文件的 MD5 哈希值判断是否有变化
3. 如果配置文件有变化：
   - 删除旧的压缩文件
   - 使用 **Brotli** 重新压缩生成 `.br` 文件（最高压缩率）
   - 使用 **Gzip** 重新压缩生成 `.gz` 文件（兼容性好）
   - 保存新的哈希值用于下次对比
4. 如果配置未变化，直接使用现有的压缩文件（提高启动速度）

**优势**：
- ✅ 自动压缩，保持最佳性能
- ✅ 智能缓存，避免重复压缩
- ✅ 浏览器自动选择最优压缩格式
- ✅ 无需手动干预

## 🚀 使用方法

### 方法 1：使用 Docker Compose（推荐）

1. **编辑配置文件**
   ```bash
   cd docker/config
   vi appsettings.json
   ```

2. **修改你需要的设置**
   ```json
   {
     "ApiBaseAddress": "",
     "Oidc": {
       "Authority": "https://your-auth-server.com/realms/your-realm",
       "ClientId": "your-client-id",
       "ResponseType": "code",
       "PostLogoutRedirectUri": "",
       "Scope": "openid profile email",
       "DefaultScopes": [
         "openid",
         "profile",
         "email"
       ]
     }
   }
   ```

3. **启动服务**
   ```bash
   docker-compose -f docker-compose.single-image.yml up -d
   ```

配置会自动生效，无需重新构建镜像！

### 方法 2：使用 Docker Run

```bash
docker run -d \
  --name verdure-mcp-app \
  -p 8080:8080 \
  -v $(pwd)/config/appsettings.json:/app/wwwroot/appsettings.json:ro \
  -e ConnectionStrings__mcpdb="Host=postgres;Database=verdure_mcp;..." \
  verdure-mcp-platform:latest
```

### 方法 3：Kubernetes ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: verdure-web-config
data:
  appsettings.json: |
    {
      "ApiBaseAddress": "",
      "Oidc": {
        "Authority": "https://your-auth-server.com/realms/your-realm",
        "ClientId": "your-client-id",
        "ResponseType": "code",
        "PostLogoutRedirectUri": "",
        "Scope": "openid profile email",
        "DefaultScopes": ["openid", "profile", "email"]
      }
    }
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: verdure-mcp-platform
spec:
  template:
    spec:
      containers:
      - name: app
        image: verdure-mcp-platform:latest
        volumeMounts:
        - name: web-config
          mountPath: /app/wwwroot/appsettings.json
          subPath: appsettings.json
          readOnly: true
      volumes:
      - name: web-config
        configMap:
          name: verdure-web-config
```

## 🔍 验证配置

### 1. 检查容器日志

```bash
docker logs verdure-mcp-app
```

你应该看到：
```
Starting Verdure MCP Platform...
Custom appsettings.json detected
Configuration file changed or compressed versions missing
Regenerating compressed files...
  Creating Brotli compressed version: /app/wwwroot/appsettings.json.br
  Creating Gzip compressed version: /app/wwwroot/appsettings.json.gz
✓ Configuration files updated and compressed successfully
Launching Verdure.McpPlatform.Api...
```

或者如果配置未变化：
```
Starting Verdure MCP Platform...
Custom appsettings.json detected
✓ Configuration unchanged, using existing compressed files
Launching Verdure.McpPlatform.Api...
```

### 2. 验证配置文件内容

```bash
# 进入容器
docker exec -it verdure-mcp-app sh

# 查看配置文件
cat /app/wwwroot/appsettings.json

# 检查压缩文件（应该存在且与配置匹配）
ls -la /app/wwwroot/appsettings.json*
# 应该看到: appsettings.json, appsettings.json.br, appsettings.json.gz

# 验证 Brotli 压缩文件内容
brotli -d -c /app/wwwroot/appsettings.json.br | cat

# 验证 Gzip 压缩文件内容
gzip -d -c /app/wwwroot/appsettings.json.gz | cat
```

### 3. 浏览器验证

打开浏览器开发者工具 (F12)，访问应用：
- **Network** 标签
- 查找 `appsettings.json` 请求
- 检查 **Response Headers**：
  - 应该看到 `Content-Encoding: br` (Brotli) 或 `gzip`
  - 这说明浏览器正在接收压缩版本，性能最优
- 查看 **Response** 内容，确认是你的新配置

## 📋 配置文件说明

### ApiBaseAddress
- **空字符串 `""`**: 使用当前域名（推荐用于单镜像部署）
- **完整 URL**: 如 `https://api.example.com`（用于前后端分离部署）

### Oidc.Authority
- 你的 OpenID Connect 授权服务器地址
- 例如：`https://auth.example.com/realms/my-realm`

### Oidc.ClientId
- 在授权服务器注册的客户端 ID
- 例如：`verdure-mcp-web`

### PostLogoutRedirectUri
- 登出后重定向地址
- **空字符串 `""`**: 重定向到当前应用根路径（推荐）
- **完整 URL**: 如 `https://example.com/logout-success`

## 🎯 常见场景

### 场景 1：更改认证服务器

```bash
cd docker/config
cat > appsettings.json << 'EOF'
{
  "ApiBaseAddress": "",
  "Oidc": {
    "Authority": "https://new-auth.example.com/realms/production",
    "ClientId": "prod-client-id",
    "ResponseType": "code",
    "PostLogoutRedirectUri": "",
    "Scope": "openid profile email",
    "DefaultScopes": ["openid", "profile", "email"]
  }
}
EOF

docker-compose -f docker-compose.single-image.yml restart app
```

### 场景 2：多租户部署

为不同的租户创建不同的配置文件：

```bash
# 租户 A
mkdir -p ./tenant-a
cp docker/config/appsettings.json ./tenant-a/
# 编辑 tenant-a/appsettings.json

docker run -d \
  --name verdure-mcp-tenant-a \
  -p 8081:8080 \
  -v $(pwd)/tenant-a/appsettings.json:/app/wwwroot/appsettings.json:ro \
  verdure-mcp-platform:latest

# 租户 B
mkdir -p ./tenant-b
cp docker/config/appsettings.json ./tenant-b/
# 编辑 tenant-b/appsettings.json

docker run -d \
  --name verdure-mcp-tenant-b \
  -p 8082:8080 \
  -v $(pwd)/tenant-b/appsettings.json:/app/wwwroot/appsettings.json:ro \
  verdure-mcp-platform:latest
```

## 🔒 安全注意事项

1. **只读挂载**: 使用 `:ro` 标志防止容器修改配置文件
2. **文件权限**: 确保配置文件权限为 `644` 或更严格
3. **敏感信息**: 
   - ClientSecret 应该在**后端环境变量**中配置，不要放在前端配置
   - PostLogoutRedirectUri 使用相对路径更安全

## 🆘 故障排查

### 问题：修改配置后没有生效

**原因**：浏览器缓存

**解决**：
```bash
# 强制刷新（清除缓存）
Ctrl + Shift + R (Windows/Linux)
Cmd + Shift + R (Mac)

# 或者清除浏览器缓存后重新访问
```

### 问题：容器启动失败

**检查**：
```bash
# 查看日志
docker logs verdure-mcp-app

# 检查配置文件是否存在
ls -la docker/config/appsettings.json

# 验证 JSON 格式
cat docker/config/appsettings.json | jq .
```

### 问题：仍然收到压缩文件

**解决**：
```bash
# 重启容器以重新运行 entrypoint 脚本
docker-compose -f docker-compose.single-image.yml restart app

# 或者完全重新创建容器
docker-compose -f docker-compose.single-image.yml down
docker-compose -f docker-compose.single-image.yml up -d
```

## 📚 相关文档

- **完整部署指南**: `docs/guides/SINGLE_IMAGE_DEPLOYMENT.md`
- **Docker Compose 配置**: `docker/docker-compose.single-image.yml`
- **Entrypoint 脚本**: `docker/entrypoint.sh`
- **Dockerfile**: `docker/Dockerfile.single-image`

## 💡 最佳实践

1. **版本控制**: 将自定义配置文件加入 `.gitignore`，使用示例文件 `.example`
2. **环境分离**: 为 dev/staging/production 准备不同的配置文件
3. **自动化**: 使用 CI/CD 管道自动生成和部署配置
4. **监控**: 记录配置变更日志，便于审计和回滚
