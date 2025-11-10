# 本地开发环境优化 - 更新日志

**日期**: 2025-11-10  
**版本**: v1.0

## 📋 概述

优化了本地开发环境的部署流程，实现一键启动完整的开发环境，包括：
- PostgreSQL 数据库（自动创建 3 个数据库）
- Redis 缓存
- Keycloak 身份认证服务器（自动导入配置）
- Verdure MCP Platform 应用

## 🎯 主要改进

### 1. 集成 Keycloak 服务

#### 新增配置
- **docker-compose.single-image.yml**
  - 添加了 Keycloak 服务定义
  - 配置为使用 PostgreSQL 作为数据库
  - 自动导入 realm 配置
  - 健康检查和依赖管理

#### Keycloak 配置
- **创建了 `docker/config/keycloak/verdure-mcp-realm.json`**
  - 预配置 `verdure-mcp` realm
  - 预配置 `verdure-mcp-api` client
  - 预置两个演示用户：
    - `admin / admin123` (管理员)
    - `demo / demo123` (普通用户)
  - 国际化支持（中文/英文）
  - OIDC 协议配置

### 2. 数据库初始化优化

#### 更新 init-db.sh
- 自动创建 3 个数据库：
  - `verdure_mcp` - MCP 平台主数据库
  - `verdure_identity` - 用户身份数据库
  - `verdure_keycloak` - Keycloak 数据库

### 3. 环境配置简化

#### 更新 .env.example
- 简化默认配置
- 添加 Keycloak 管理员凭据
- 默认使用本地 Keycloak (http://localhost:8180)
- 移除生产环境的外部 URL

### 4. 新增便捷脚本

#### start-local.ps1 （主启动脚本）
**功能**：
- ✅ Docker 环境检查
- ✅ 自动创建 .env 文件
- ✅ 清理现有容器
- ✅ 构建应用镜像
- ✅ 启动所有服务
- ✅ 等待服务就绪（带进度显示）
- ✅ 显示完整的连接信息
- ✅ 自动跟随日志输出

**使用**：
```powershell
.\scripts\start-local.ps1
```

#### stop-local.ps1
**功能**：
- 优雅停止所有服务
- 保留数据卷
- 提示如何完全清理

**使用**：
```powershell
.\scripts\stop-local.ps1
```

#### view-logs.ps1
**功能**：
- 查看服务日志
- 支持筛选特定服务
- 可配置日志行数
- 支持实时跟随或一次性显示

**使用**：
```powershell
# 查看所有日志
.\scripts\view-logs.ps1

# 查看应用日志
.\scripts\view-logs.ps1 -Service app

# 只显示最近 50 行
.\scripts\view-logs.ps1 -Tail 50
```

#### health-check.ps1
**功能**：
- 检查所有服务健康状态
- 显示服务 URL
- 提供故障排查建议

**使用**：
```powershell
.\scripts\health-check.ps1
```

### 5. 完整文档

#### docker/README.md
**内容**：
- 📋 前置要求
- 🚀 快速启动指南
- 🛠️ 常用命令参考
- 🔧 配置说明
- 📊 服务端口清单
- 🗄️ 数据库信息
- 🔐 Keycloak 配置
- 🧪 测试指南
- 🐛 故障排查
- 💡 使用提示

## 🏗️ 架构变更

### 服务依赖关系

```
┌─────────────┐
│ Application │
└──────┬──────┘
       │
       ├──────────┐
       │          │
       ▼          ▼
┌──────────┐  ┌─────┐
│ Keycloak │  │Redis│
└────┬─────┘  └─────┘
     │
     ▼
┌──────────────┐
│  PostgreSQL  │
│ (3 databases)│
└──────────────┘
```

### 端口映射

| 服务 | 容器端口 | 主机端口 |
|------|----------|----------|
| Application | 8080 | 8080 |
| Keycloak | 8080 | 8180 |
| PostgreSQL | 5432 | 5432 |
| Redis | 6379 | 6379 |

## 📝 配置文件变更

### 修改的文件

1. **docker/docker-compose.single-image.yml**
   - 添加 Keycloak 服务
   - 更新应用依赖
   - 更新默认环境变量
   - 添加 Keycloak 数据卷

2. **docker/init-db.sh**
   - 添加 Keycloak 数据库创建

3. **docker/.env.example**
   - 简化默认配置
   - 添加 Keycloak 配置
   - 使用本地服务 URL

### 新增的文件

1. **docker/config/keycloak/verdure-mcp-realm.json**
   - Keycloak realm 完整配置
   - Client 配置
   - 用户预置

2. **scripts/start-local.ps1**
   - 主启动脚本

3. **scripts/stop-local.ps1**
   - 停止服务脚本

4. **scripts/view-logs.ps1**
   - 日志查看脚本

5. **scripts/health-check.ps1**
   - 健康检查脚本

6. **docker/README.md**
   - 本地开发文档

7. **docs/LOCAL_DEV_OPTIMIZATION.md**
   - 本文档

## 🎓 使用流程

### 首次启动

```powershell
# 1. 克隆代码
git clone https://github.com/maker-community/verdure-mcp-for-xiaozhi.git
cd verdure-mcp-for-xiaozhi

# 2. 启动所有服务
.\scripts\start-local.ps1

# 3. 等待服务启动（约 2-3 分钟）
# 脚本会自动等待并显示进度

# 4. 访问应用
# http://localhost:8080
```

### 日常开发

```powershell
# 查看服务状态
.\scripts\health-check.ps1

# 查看应用日志
.\scripts\view-logs.ps1 -Service app

# 重启应用（不影响数据库）
cd docker
docker-compose -f docker-compose.single-image.yml restart app

# 停止所有服务
.\scripts\stop-local.ps1
```

### 完全重置

```powershell
# 停止并删除所有数据
cd docker
docker-compose -f docker-compose.single-image.yml down -v

# 重新启动
.\scripts\start-local.ps1
```

## 🔒 安全注意事项

⚠️ **仅供开发使用，不适合生产环境**

默认配置使用简单密码，便于本地开发：
- PostgreSQL: `postgres / postgres`
- Keycloak Admin: `admin / admin`
- 演示用户: `admin / admin123`, `demo / demo123`

生产部署时必须：
1. 使用强密码
2. 启用 HTTPS
3. 配置适当的 CORS
4. 使用环境变量管理敏感信息
5. 禁用开发模式

## 🧪 测试场景

### 1. 基本功能测试

```powershell
# 启动服务
.\scripts\start-local.ps1

# 访问应用
Start-Process "http://localhost:8080"

# 使用演示用户登录
# 用户名: admin
# 密码: admin123
```

### 2. 多实例测试

```powershell
# 启动 3 个应用实例
cd docker
docker-compose -f docker-compose.single-image.yml up -d --scale app=3

# 查看日志验证分布式功能
docker-compose -f docker-compose.single-image.yml logs -f app
```

### 3. 故障恢复测试

```powershell
# 停止一个实例
docker stop verdure-mcp-app

# 观察其他实例接管连接
.\scripts\view-logs.ps1 -Service app

# 重启实例
docker start verdure-mcp-app
```

## 📊 性能优化

### 资源限制

docker-compose 中已配置：
- CPU 限制: 2 核心（限制）/ 0.5 核心（预留）
- 内存限制: 1GB（限制）/ 512MB（预留）

### 启动时间

| 服务 | 预计启动时间 |
|------|--------------|
| PostgreSQL | 10-15 秒 |
| Redis | 5-10 秒 |
| Keycloak | 60-90 秒 |
| Application | 30-60 秒 |

**总计**: 约 2-3 分钟（首次启动）

## 🔄 升级路径

如果已有旧的部署，升级步骤：

```powershell
# 1. 备份数据（可选）
docker exec verdure-postgres pg_dump -U postgres verdure_mcp > backup.sql

# 2. 停止旧服务
cd docker
docker-compose -f docker-compose.single-image.yml down

# 3. 拉取最新代码
git pull

# 4. 启动新服务
.\scripts\start-local.ps1
```

## 🆘 常见问题

### Q: Keycloak 启动很慢？
A: Keycloak 首次启动需要初始化数据库，通常需要 60-90 秒，这是正常的。

### Q: 端口被占用怎么办？
A: 修改 `docker/docker-compose.single-image.yml` 中的端口映射。

### Q: 如何重置 Keycloak 配置？
A: 
```powershell
cd docker
docker-compose -f docker-compose.single-image.yml down -v
.\scripts\start-local.ps1
```

### Q: 应用无法连接 Keycloak？
A: 
1. 检查 Keycloak 是否完全启动：`.\scripts\health-check.ps1`
2. 查看日志：`.\scripts\view-logs.ps1 -Service keycloak`

## 📚 相关文档

- [Docker 单镜像部署总结](./SINGLE_IMAGE_DEPLOYMENT_SUMMARY.md)
- [分布式 WebSocket 指南](../docs/architecture/DISTRIBUTED_WEBSOCKET_GUIDE.md)
- [API 使用示例](../docs/guides/API_EXAMPLES.md)
- [部署指南](../docs/guides/DEPLOYMENT.md)

## ✅ 验证清单

部署完成后，验证以下内容：

- [ ] 所有容器正在运行
- [ ] PostgreSQL 创建了 3 个数据库
- [ ] Redis 可以连接
- [ ] Keycloak 可以访问（http://localhost:8180）
- [ ] 应用可以访问（http://localhost:8080）
- [ ] 可以使用演示用户登录
- [ ] 健康检查通过：`.\scripts\health-check.ps1`

## 🎉 总结

这次优化大大简化了本地开发环境的设置：

**之前**：
- 需要手动安装和配置 Keycloak
- 需要手动创建数据库
- 需要手动配置 realm 和 client
- 多个配置文件需要手动同步

**现在**：
- 一键启动所有服务
- 自动配置和初始化
- 预置演示用户
- 完整的管理脚本

**时间节省**：从 30+ 分钟减少到 5 分钟！
