# WebSocket 分布式连接管理 - 快速开始

## 本地测试指南

本指南将帮助您在本地环境测试多实例 WebSocket 连接管理功能。

## 前置条件

1. **.NET 9 SDK** 已安装
2. **Docker Desktop** 已安装并运行
3. **Redis** 服务器（通过 Docker 启动）

## 步骤 1: 启动 Redis

打开 PowerShell 终端，运行：

```powershell
# 启动 Redis 容器
docker run -d --name redis-mcp -p 6379:6379 redis:latest

# 验证 Redis 是否正常运行
docker ps | Select-String redis
```

您应该看到 Redis 容器正在运行。

## 步骤 2: 启动第一个 API 实例

在第一个终端窗口中：

```powershell
cd c:\github-verdure\verdure-mcp-for-xiaozhi\src\Verdure.McpPlatform.Api

# 设置端口为 5001
$env:ASPNETCORE_URLS="http://localhost:5001"
$env:ConnectionStrings__Redis="localhost:6379"

dotnet run
```

观察日志输出，您应该看到：
- `"Connection state service initialized with instance ID: ..."`
- `"Connection monitor service starting"`
- `"Checking for enabled servers to connect on startup"`

## 步骤 3: 启动第二个 API 实例

在第二个终端窗口中：

```powershell
cd c:\github-verdure\verdure-mcp-for-xiaozhi\src\Verdure.McpPlatform.Api

# 设置端口为 5002
$env:ASPNETCORE_URLS="http://localhost:5002"
$env:ConnectionStrings__Redis="localhost:6379"

dotnet run
```

## 步骤 4: 配置并启用一个小智连接

使用浏览器或 Postman 访问 API：

### 4.1 登录并获取 Token
```http
POST http://localhost:5001/api/auth/login
Content-Type: application/json

{
  "username": "your-username",
  "password": "your-password"
}
```

### 4.2 创建小智连接
```http
POST http://localhost:5001/api/mcp-servers
Authorization: Bearer {your-token}
Content-Type: application/json

{
  "name": "测试服务器",
  "address": "wss://your-xiaozhi-websocket-endpoint"
}
```

### 4.3 启用连接
```http
POST http://localhost:5001/api/mcp-servers/{serverId}/enable
Authorization: Bearer {your-token}
```

## 步骤 5: 观察日志

在两个实例的日志中观察以下内容：

### 实例 1 的日志 (可能获得连接)：
```
info: Verdure.McpPlatform.Api.Services.DistributedLock.RedisDistributedLockService[0]
      Acquired distributed lock for resource: mcp:lock:connection:{serverId}

info: Verdure.McpPlatform.Api.Services.WebSocket.McpSessionManager[0]
      Created session for server {serverId} (测试服务器)

info: Verdure.McpPlatform.Api.Services.ConnectionState.RedisConnectionStateService[0]
      Registered connection for server {serverId} on instance {instanceId}
```

### 实例 2 的日志 (检测到连接已存在)：
```
info: Verdure.McpPlatform.Api.Services.WebSocket.McpSessionManager[0]
      Server {serverId} is already connected on instance {instanceId-1}
```

## 步骤 6: 测试连接监控和自动重连

### 6.1 手动停止实例 1

在实例 1 的终端按 `Ctrl+C` 停止服务。

### 6.2 观察实例 2 的日志

大约 45-90 秒后（取决于心跳超时配置），实例 2 应该检测到：

```
warn: Verdure.McpPlatform.Api.Services.BackgroundServices.ConnectionMonitorHostedService[0]
      Stale connection detected for server {serverId}, last heartbeat: {time}

info: Verdure.McpPlatform.Api.Services.BackgroundServices.ConnectionMonitorHostedService[0]
      Attempting to reconnect server {serverId}

info: Verdure.McpPlatform.Api.Services.WebSocket.McpSessionManager[0]
      Acquired distributed lock for server {serverId}

info: Verdure.McpPlatform.Api.Services.WebSocket.McpSessionManager[0]
      Successfully reconnected server {serverId}
```

### 6.3 重新启动实例 1

再次启动实例 1：

```powershell
$env:ASPNETCORE_URLS="http://localhost:5001"
$env:ConnectionStrings__Redis="localhost:6379"
dotnet run
```

观察它的日志，应该看到：

```
info: Verdure.McpPlatform.Api.Services.BackgroundServices.ConnectionMonitorHostedService[0]
      Server {serverId} (测试服务器) is already connected on instance {instanceId-2}
```

这证明实例 1 识别到连接已由实例 2 持有，不会创建重复连接。

## 步骤 7: 使用 Redis CLI 检查状态

打开新的终端：

```powershell
# 连接到 Redis 容器
docker exec -it redis-mcp redis-cli

# 在 Redis CLI 中执行以下命令：

# 查看所有连接
SMEMBERS mcp:connections:all

# 查看特定连接详情（替换 {serverId} 为实际ID）
GET mcp:connection:{serverId}

# 查看所有连接键
KEYS mcp:connection:*

# 查看分布式锁（如果存在）
KEYS mcp:lock:*

# 退出
exit
```

示例输出：
```
127.0.0.1:6379> GET mcp:connection:your-server-id
"{\"ServerId\":\"your-server-id\",\"InstanceId\":\"DESKTOP-ABC:12345:guid\",\"Status\":\"Connected\",\"LastConnectedTime\":\"2025-11-05T10:30:00Z\",\"LastHeartbeat\":\"2025-11-05T10:35:00Z\",\"ReconnectAttempts\":0,\"ServerName\":\"测试服务器\",\"WebSocketEndpoint\":\"wss://...\"}"
```

## 步骤 8: 测试负载均衡

### 8.1 创建多个小智连接

重复步骤 4，创建 3-5 个不同的小智连接并全部启用。

### 8.2 观察连接分布

在两个实例的日志中，您应该看到连接被分配到不同的实例：

**实例 1 可能管理**:
- 服务器 A
- 服务器 C

**实例 2 可能管理**:
- 服务器 B
- 服务器 D

这表明分布式锁机制正常工作，每个连接只被一个实例持有。

## 步骤 9: 压力测试

### 9.1 启动第三个实例

```powershell
$env:ASPNETCORE_URLS="http://localhost:5003"
$env:ConnectionStrings__Redis="localhost:6379"
dotnet run
```

### 9.2 创建大量连接

使用脚本或工具创建 20-50 个小智连接配置。

### 9.3 同时启用所有连接

观察三个实例如何协调并分配连接，确保没有重复。

## 故障场景测试

### 场景 1: Redis 连接失败

```powershell
# 停止 Redis
docker stop redis-mcp

# 观察 API 实例的日志 - 应该有错误但不会崩溃

# 重新启动 Redis
docker start redis-mcp

# 观察连接自动恢复
```

### 场景 2: 网络分区模拟

临时断开一个实例的网络，观察其他实例如何接管连接。

### 场景 3: 快速重启

快速重启所有实例，观察连接状态的恢复。

## 清理

测试完成后，清理环境：

```powershell
# 停止所有 API 实例（在各自终端按 Ctrl+C）

# 停止并删除 Redis 容器
docker stop redis-mcp
docker rm redis-mcp

# 清理数据库（可选）
Remove-Item -Path "c:\github-verdure\verdure-mcp-for-xiaozhi\src\Verdure.McpPlatform.Api\Data\*.db*" -Force
```

## 常见问题

### Q: 两个实例都创建了相同的连接怎么办？

A: 这表明分布式锁未正常工作。检查：
1. Redis 是否正常运行
2. 两个实例是否连接到同一个 Redis
3. 日志中是否有锁相关的错误

### Q: 连接断开后不会自动重连

A: 检查：
1. `ConnectionMonitor` 配置是否正确
2. 服务器在数据库中是否仍然是 `IsEnabled=true`
3. 冷却期是否已过（默认 30-60 秒）

### Q: 如何调整重连行为？

A: 修改 `appsettings.Development.json`:
```json
{
  "ConnectionMonitor": {
    "CheckIntervalSeconds": 10,       // 更频繁检查
    "HeartbeatTimeoutSeconds": 30,    // 更快检测断开
    "ReconnectCooldownSeconds": 15    // 更快重连
  }
}
```

## 下一步

完成基本测试后，您可以：

1. 阅读 `DISTRIBUTED_WEBSOCKET_GUIDE.md` 了解详细架构
2. 配置生产环境的 Redis 集群
3. 设置监控和告警
4. 部署到 Kubernetes 或其他容器编排平台

## 性能基准

在本地测试中，您应该观察到：

- **连接建立时间**: < 1 秒
- **锁获取时间**: < 100 毫秒
- **心跳更新**: 每 30 秒，< 10 毫秒
- **故障检测时间**: 45-90 秒
- **自动重连时间**: 30-60 秒（冷却期后）

祝测试顺利！🚀
