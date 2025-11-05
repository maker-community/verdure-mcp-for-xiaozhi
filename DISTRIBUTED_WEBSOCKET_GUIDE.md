# WebSocket 分布式连接管理方案

## 概述

本方案解决了多实例部署场景下 WebSocket 连接的管理问题，包括：

1. **避免重复连接**：使用 Redis 分布式锁确保多个实例不会同时创建相同的 WebSocket 连接
2. **连接状态共享**：所有实例的连接状态存储在 Redis 中，实现跨实例可见性
3. **自动重连机制**：后台服务监控断开的连接，并在服务重启或连接失败后自动重连
4. **心跳检测**：定期更新连接心跳，检测僵尸连接并清理

## 架构组件

### 1. 分布式锁服务 (IDistributedLockService)

**位置**: `Services/DistributedLock/RedisDistributedLockService.cs`

**功能**:
- 使用 RedLock 算法实现分布式锁
- 确保只有一个实例能够创建特定的 WebSocket 连接
- 支持锁超时和自动释放

**关键方法**:
```csharp
// 尝试获取锁（带重试）
Task<IDistributedLockHandle?> AcquireLockAsync(
    string resourceKey,
    TimeSpan expiryTime,
    TimeSpan waitTime,
    TimeSpan retryTime,
    CancellationToken cancellationToken = default);

// 快速尝试获取锁（不等待）
Task<IDistributedLockHandle?> TryAcquireLockAsync(
    string resourceKey,
    TimeSpan expiryTime,
    CancellationToken cancellationToken = default);
```

### 2. 连接状态服务 (IConnectionStateService)

**位置**: `Services/ConnectionState/RedisConnectionStateService.cs`

**功能**:
- 在 Redis 中存储和管理所有实例的连接状态
- 记录连接的实例 ID、状态、心跳时间等信息
- 支持查询和更新连接状态

**连接状态类型**:
- `Connected`: 连接活跃
- `Disconnected`: 连接已断开
- `Connecting`: 正在连接中
- `Failed`: 连接失败

**关键方法**:
```csharp
// 注册新连接
Task RegisterConnectionAsync(string serverId, string serverName, string endpoint, CancellationToken cancellationToken = default);

// 更新连接状态
Task UpdateConnectionStatusAsync(string serverId, ConnectionStatus status, CancellationToken cancellationToken = default);

// 更新心跳
Task UpdateHeartbeatAsync(string serverId, CancellationToken cancellationToken = default);

// 获取所有连接状态
Task<List<ConnectionStateInfo>> GetAllConnectionStatesAsync(CancellationToken cancellationToken = default);

// 查找过期连接
Task<List<ConnectionStateInfo>> GetStaleConnectionsAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
```

### 3. 改进的会话管理器 (McpSessionManager)

**位置**: `Services/WebSocket/McpSessionManager.cs`

**改进内容**:
- 启动连接前先检查 Redis 中的连接状态
- 使用分布式锁避免重复连接
- 双重检查机制（获取锁前后都检查状态）
- 连接成功后注册到 Redis
- 连接失败或停止时从 Redis 注销

**启动连接流程**:
```
1. 检查本地是否已有连接
2. 检查 Redis 中是否已有活跃连接
3. 尝试获取分布式锁（10秒超时）
4. 获取锁后再次检查 Redis 状态（双重检查）
5. 创建连接并注册到 Redis
6. 启动连接，定期更新心跳
```

### 4. 连接监控后台服务 (ConnectionMonitorHostedService)

**位置**: `Services/BackgroundServices/ConnectionMonitorHostedService.cs`

**功能**:
- 服务启动时自动连接所有已启用的服务器
- 定期检查连接状态并更新心跳
- 检测并清理僵尸连接（心跳超时）
- 自动重连断开的连接（带冷却期）
- 支持可配置的检查间隔和超时时间

**配置项**:
```json
{
  "ConnectionMonitor": {
    "CheckIntervalSeconds": 30,          // 检查间隔
    "HeartbeatTimeoutSeconds": 90,       // 心跳超时
    "ReconnectCooldownSeconds": 60       // 重连冷却期
  }
}
```

## 配置说明

### Redis 连接配置

在 `appsettings.json` 中配置 Redis 连接字符串：

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

对于生产环境，可以配置更复杂的 Redis 连接：

```json
{
  "ConnectionStrings": {
    "Redis": "redis-server:6379,password=yourpassword,ssl=true,abortConnect=false"
  }
}
```

### 监控服务配置

开发环境（`appsettings.Development.json`）:
```json
{
  "ConnectionMonitor": {
    "CheckIntervalSeconds": 15,      // 更频繁的检查
    "HeartbeatTimeoutSeconds": 45,   // 更短的超时
    "ReconnectCooldownSeconds": 30   // 更短的冷却期
  }
}
```

生产环境（`appsettings.json`）:
```json
{
  "ConnectionMonitor": {
    "CheckIntervalSeconds": 30,      // 标准检查间隔
    "HeartbeatTimeoutSeconds": 90,   // 标准超时
    "ReconnectCooldownSeconds": 60   // 标准冷却期
  }
}
```

## 部署指南

### 1. 准备 Redis 服务器

**使用 Docker**:
```bash
docker run -d --name redis -p 6379:6379 redis:latest
```

**使用 Docker Compose**:
```yaml
version: '3.8'
services:
  redis:
    image: redis:latest
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes

volumes:
  redis-data:
```

### 2. 部署多个 API 实例

**方法 1: Docker Compose**
```yaml
version: '3.8'
services:
  redis:
    image: redis:latest
    ports:
      - "6379:6379"

  api-instance-1:
    build: .
    ports:
      - "5001:8080"
    environment:
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      - redis

  api-instance-2:
    build: .
    ports:
      - "5002:8080"
    environment:
      - ConnectionStrings__Redis=redis:6379
    depends_on:
      - redis
```

**方法 2: Kubernetes**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mcp-platform-api
spec:
  replicas: 3  # 3个实例
  selector:
    matchLabels:
      app: mcp-platform-api
  template:
    metadata:
      labels:
        app: mcp-platform-api
    spec:
      containers:
      - name: api
        image: your-registry/mcp-platform-api:latest
        env:
        - name: ConnectionStrings__Redis
          value: "redis-service:6379"
        ports:
        - containerPort: 8080
```

### 3. 负载均衡配置

使用 Nginx 或 Kubernetes Ingress 进行负载均衡：

**Nginx 配置示例**:
```nginx
upstream mcp_api {
    server api-instance-1:5001;
    server api-instance-2:5002;
    server api-instance-3:5003;
}

server {
    listen 80;
    
    location /api/ {
        proxy_pass http://mcp_api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
    }
}
```

## 工作原理

### 启动场景

1. **首次启动（所有实例）**:
   - 每个实例的 `ConnectionMonitorHostedService` 启动
   - 查询数据库获取所有已启用的服务器
   - 每个实例尝试连接这些服务器
   - 只有成功获取分布式锁的实例能建立连接
   - 其他实例看到连接已存在，跳过

2. **服务重启（部分实例）**:
   - 重启的实例检测已启用的服务器
   - 检查 Redis 中的连接状态
   - 如果连接由其他实例持有且活跃，跳过
   - 如果连接不存在或已断开，尝试重新连接

### 运行时场景

1. **正常运行**:
   - 持有连接的实例每 15-30 秒更新一次心跳
   - 监控服务检查所有连接的心跳状态
   - 其他实例看到活跃的连接，不会尝试重复连接

2. **连接断开**:
   - WebSocket 连接断开时，更新 Redis 状态为 `Disconnected`
   - 从本地内存和 Redis 中移除连接信息
   - 监控服务在冷却期后检测到断开的连接
   - 某个实例（获得锁的）尝试重新连接

3. **实例崩溃**:
   - 崩溃实例的心跳停止更新
   - 其他实例的监控服务检测到心跳超时（90秒）
   - 清理僵尸连接状态
   - 在冷却期后，某个实例尝试重新建立连接

### 数据流

```
实例 A              Redis                实例 B
  |                  |                      |
  |-- 尝试连接 ------>|                      |
  |   (获取锁)        |                      |
  |<------ 锁 --------|                      |
  |                   |                      |
  |-- 注册连接 ------>|                      |
  |   (状态=Connected)|                      |
  |                   |                      |
  |                   |<---- 检查连接 -------|
  |                   |----> 已存在 -------->|
  |                   |                      |
  |-- 更新心跳 ------>|                      |
  |   (每30秒)        |                      |
  |                   |<---- 监控心跳 -------|
  |                   |                      |
```

## Redis 数据结构

### 连接状态键
```
Key: mcp:connection:{serverId}
Type: String (JSON)
Value: {
  "ServerId": "server-123",
  "InstanceId": "MACHINE-NAME:12345:guid",
  "Status": "Connected",
  "LastConnectedTime": "2025-11-05T10:00:00Z",
  "LastDisconnectedTime": null,
  "LastHeartbeat": "2025-11-05T10:05:00Z",
  "ReconnectAttempts": 0,
  "ServerName": "Xiaozhi Server 1",
  "WebSocketEndpoint": "wss://api.example.com/ws"
}
```

### 连接列表键
```
Key: mcp:connections:all
Type: Set
Members: ["server-123", "server-456", "server-789"]
```

### 分布式锁键
```
Key: mcp:lock:connection:{serverId}
Type: String
Value: {lockId}
TTL: 300 seconds
```

## 监控和调试

### 查看 Redis 中的连接状态

```bash
# 连接到 Redis
redis-cli

# 查看所有连接
SMEMBERS mcp:connections:all

# 查看特定连接的详细信息
GET mcp:connection:{serverId}

# 查看所有连接键
KEYS mcp:connection:*

# 查看分布式锁
KEYS mcp:lock:*
```

### 日志级别配置

在 `appsettings.Development.json` 中启用详细日志：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Verdure.McpPlatform.Api.Services.BackgroundServices": "Debug",
      "Verdure.McpPlatform.Api.Services.ConnectionState": "Debug",
      "Verdure.McpPlatform.Api.Services.DistributedLock": "Debug",
      "Verdure.McpPlatform.Api.Services.WebSocket": "Debug"
    }
  }
}
```

### 关键日志信息

查找以下日志消息来监控系统行为：

- `"Acquired distributed lock for server {ServerId}"` - 成功获取锁
- `"Successfully started connection for server {ServerId}"` - 连接成功
- `"Server {ServerId} is already connected on instance {InstanceId}"` - 检测到重复连接被阻止
- `"Stale connection detected"` - 发现僵尸连接
- `"Attempting to reconnect server {ServerId}"` - 开始重连
- `"Updated heartbeats for {Count} local connections"` - 心跳更新

## 故障排查

### 问题 1: 连接未自动重连

**症状**: 连接断开后不会自动重连

**检查**:
1. 确认 Redis 连接正常
2. 检查 `ConnectionMonitor` 配置是否正确
3. 查看日志中是否有错误信息
4. 确认服务器在数据库中仍然是 `IsEnabled=true`

**解决**:
```bash
# 检查 Redis 连接
redis-cli ping

# 查看后台服务日志
# 确认 ConnectionMonitorHostedService 正在运行
```

### 问题 2: 多个实例创建重复连接

**症状**: 同一个服务器有多个 WebSocket 连接

**原因**: 
- Redis 连接失败或配置错误
- 分布式锁服务未正常工作

**解决**:
1. 检查所有实例的 Redis 连接配置
2. 确认 RedLock 服务正常注册
3. 检查日志中的锁获取消息

### 问题 3: 连接频繁断开重连

**症状**: 连接不稳定，频繁重连

**原因**:
- 心跳超时设置太短
- 网络不稳定
- 小智服务器端问题

**解决**:
1. 增加 `HeartbeatTimeoutSeconds` 配置
2. 增加 `ReconnectCooldownSeconds` 避免过于频繁重连
3. 检查网络连接质量

## 性能考虑

### Redis 性能

- 每个连接每 30 秒一次心跳更新（轻量级操作）
- 连接状态查询使用 Redis String GET（O(1)复杂度）
- 分布式锁操作使用 RedLock 算法（高可用）

### 建议配置

- **小规模部署** (< 10 实例, < 100 连接):
  - 单个 Redis 实例即可
  - CheckIntervalSeconds: 30
  
- **中等规模部署** (10-50 实例, 100-1000 连接):
  - Redis 主从或哨兵模式
  - CheckIntervalSeconds: 30-60
  
- **大规模部署** (> 50 实例, > 1000 连接):
  - Redis 集群模式
  - CheckIntervalSeconds: 60
  - 考虑分片策略

## 未来增强

可能的改进方向：

1. **连接池管理**: 支持每个服务器多个连接以提高吞吐量
2. **优先级队列**: 为重要连接提供更高的重连优先级
3. **健康检查 API**: 提供 HTTP 端点查询所有连接状态
4. **指标监控**: 集成 Prometheus 指标
5. **告警机制**: 连接失败时发送告警通知

## 总结

此方案通过以下机制解决了多实例部署的挑战：

✅ **避免重复连接** - 使用 Redis 分布式锁  
✅ **状态共享** - 所有状态存储在 Redis  
✅ **自动重连** - 后台服务持续监控并重连  
✅ **心跳检测** - 及时发现和清理僵尸连接  
✅ **高可用性** - 实例故障不影响其他实例  
✅ **可扩展性** - 支持水平扩展到多个实例  

这是一个生产就绪的解决方案，可以安全地部署在多实例环境中。
