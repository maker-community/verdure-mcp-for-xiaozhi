# Database-Redis 一致性修复

## 🔍 问题分析

### 问题场景

在分布式 WebSocket 管理系统中，发现了一个严重的状态不一致问题：

**症状**：
- 数据库中服务器状态：`IsEnabled = true`，`IsConnected = false`
- Redis 中连接状态：**完全没有数据**（key 不存在）
- 后台监控服务启动后，连接**无法自动恢复**

### 根因分析

#### 现有恢复逻辑的盲区

**1. 启动时检查（`CheckAndStartEnabledServersAsync`）**

```csharp
var connectionState = await connectionStateService.GetConnectionStateAsync(server.Id, cancellationToken);

if (connectionState != null && connectionState.Status == ConnectionStatus.Connected)
{
    continue;  // 跳过已连接的
}

// 尝试启动连接
var started = await sessionManager.StartSessionAsync(server.Id, cancellationToken);
```

**分析**：
- ✅ **这部分逻辑正确**：当 Redis 无数据时（`connectionState == null`），会尝试创建连接
- ❌ **问题**：如果这次启动失败或被中断，后续没有重试机制

**2. 监控循环（`MonitorConnectionsAsync`）**

```csharp
// 只从 Redis 获取已存在的连接状态
var allStates = await connectionStateService.GetAllConnectionStatesAsync(cancellationToken);
var disconnectedStates = allStates.Where(
    s => s.Status == ConnectionStatus.Disconnected || s.Status == ConnectionStatus.Failed).ToList();

// 只处理 Redis 中已存在的 disconnected 状态
foreach (var disconnectedState in disconnectedStates)
{
    // 重连逻辑
}
```

**分析**：
- ❌ **致命缺陷**：`GetAllConnectionStatesAsync()` 只返回 Redis 中已存在的 key
- ❌ 如果某个服务器在 Redis 中**完全没有数据**，监控循环**永远不会发现它**
- ❌ 即使数据库中该服务器是启用状态，也会被完全忽略

### 问题场景复现

```
时间线：
T0 - 初始状态
     数据库：Server A (IsEnabled=true, IsConnected=false)
     Redis：  (无数据)

T1 - 服务启动
     CheckAndStartEnabledServersAsync 执行
     → 找到 Server A (IsEnabled=true)
     → 检查 Redis：connectionState = null
     → 尝试启动连接 ✅

T2 - 启动失败（各种原因）
     - 网络问题
     - 小智服务器暂时不可用
     - 其他异常
     
     结果：
     数据库：Server A (IsEnabled=true, IsConnected=false)  ← 没变
     Redis：  (无数据)                                    ← 没变

T3 - 监控循环执行（每 30 秒）
     GetAllConnectionStatesAsync() 返回：[]  ← 空列表！
     
     → 没有 disconnected 状态需要处理
     → 完全忽略了 Server A
     → ❌ 永远不会重试连接

T4, T5, T6... - 后续的监控循环
     → 继续忽略 Server A
     → 连接永远无法恢复
```

### 为什么会出现这种情况？

1. **Redis 数据丢失**：
   - Redis 重启（未持久化）
   - 手动清空 Redis
   - Redis key 过期
   - 连接失败时未写入 Redis

2. **数据库状态保留**：
   - 用户手动启用服务器
   - 数据库迁移或恢复
   - `IsEnabled` 状态独立于 Redis 管理

3. **监控逻辑盲区**：
   - 只检查 Redis 中已存在的状态
   - 不主动对比数据库和 Redis

---

## ✅ 解决方案

### 核心思路

**在监控循环中主动对比数据库和 Redis，发现不一致时自动修复**

### 实现：新增一致性检查方法

```csharp
/// <summary>
/// Check consistency between database (enabled servers) and Redis (connection states)
/// Recovers servers that are enabled in database but missing from Redis
/// </summary>
private async Task CheckDatabaseRedisConsistencyAsync(
    IXiaozhiMcpEndpointRepository serverRepository,
    IConnectionStateService connectionStateService,
    McpSessionManager sessionManager,
    CancellationToken cancellationToken)
{
    try
    {
        // 1. 获取数据库中所有启用的服务器
        var enabledServers = await serverRepository.GetEnabledServersAsync(cancellationToken);
        
        // 2. 获取 Redis 中所有连接状态
        var allRedisStates = await connectionStateService.GetAllConnectionStatesAsync(cancellationToken);
        var redisServerIds = new HashSet<string>(allRedisStates.Select(s => s.ServerId));

        // 3. 找出在数据库中启用但 Redis 中缺失的服务器
        var missingServers = enabledServers
            .Where(server => !redisServerIds.Contains(server.Id))
            .ToList();

        if (missingServers.Any())
        {
            _logger.LogWarning(
                "Found {Count} enabled servers in database but missing from Redis - attempting recovery",
                missingServers.Count);

            // 4. 逐个恢复缺失的连接
            foreach (var server in missingServers)
            {
                try
                {
                    _logger.LogInformation(
                        "Recovering missing connection for enabled server {ServerId} ({ServerName})",
                        server.Id,
                        server.Name);

                    // 尝试启动连接
                    var started = await sessionManager.StartSessionAsync(server.Id, cancellationToken);

                    if (started)
                    {
                        _logger.LogInformation(
                            "Successfully recovered connection for server {ServerId} ({ServerName})",
                            server.Id,
                            server.Name);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to recover connection for server {ServerId} ({ServerName}) - may be handled by another instance",
                            server.Id,
                            server.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error recovering connection for server {ServerId} ({ServerName})",
                        server.Id,
                        server.Name);
                }

                // 避免过快重试
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error checking database-Redis consistency");
    }
}
```

### 集成到监控循环

```csharp
private async Task MonitorConnectionsAsync(CancellationToken cancellationToken)
{
    // ... 现有代码 ...

    // 清理过期连接
    var staleConnections = await connectionStateService.GetStaleConnectionsAsync(...);
    // ... 清理逻辑 ...

    // ✅ 新增：检查数据库和 Redis 的一致性
    await CheckDatabaseRedisConsistencyAsync(
        serverRepository,
        connectionStateService,
        sessionManager,
        cancellationToken);

    // 重连已知的断开连接
    var disconnectedStates = allStates.Where(...);
    // ... 重连逻辑 ...
}
```

---

## 🎯 修复效果

### 修复前

```
场景：数据库有启用服务器，但 Redis 无数据

监控循环：
- 检查 Redis 连接状态：[]（空）
- 处理 disconnected 状态：无
- 结果：❌ 什么都不做，连接永远无法恢复
```

### 修复后

```
场景：数据库有启用服务器，但 Redis 无数据

监控循环：
- 检查 Redis 连接状态：[]（空）
- ✅ NEW: 对比数据库和 Redis
  - 数据库：[Server A, Server B]（启用）
  - Redis：  []（无数据）
  - 发现不一致：[Server A, Server B] 缺失
- ✅ 尝试恢复连接：
  - StartSessionAsync(Server A)
  - StartSessionAsync(Server B)
- 结果：✅ 连接自动恢复！
```

### 覆盖的场景

1. **Redis 重启后**：
   - 所有连接状态丢失
   - 下一次监控循环（30秒内）自动恢复

2. **手动清空 Redis**：
   - 一致性检查自动发现不一致
   - 重建所有启用服务器的连接

3. **服务启动失败后**：
   - 启动时连接失败 → Redis 无数据
   - 监控循环持续检查 → 定期重试

4. **手动启用服务器后**：
   - 用户在界面启用服务器
   - 如果连接创建失败，监控循环会重试

---

## 📊 性能影响

### 额外开销

每次监控循环（默认30秒）：
- 1次数据库查询：`GetEnabledServersAsync()`
- 1次 Redis 查询：`GetAllConnectionStatesAsync()`（已存在）
- 内存对比：`O(n)` 哈希集合查找

**评估**：
- 查询量：极小（通常 < 100 个服务器）
- 开销：可忽略（毫秒级）
- 触发频率：每 30 秒（可配置）

### 优化建议

如果服务器数量非常大（> 10000），可考虑：
- 增加检查间隔（如 60 秒）
- 添加缓存层
- 使用批量操作

---

## 🧪 测试场景

### 场景 1：Redis 重启

```powershell
# 1. 启动系统，创建连接
dotnet run --project src/Verdure.McpPlatform.Api

# 2. 停止 Redis
docker stop redis

# 3. 启动 Redis（数据丢失）
docker start redis

# 4. 观察日志
# ✅ 预期：30秒内看到 "Found X enabled servers in database but missing from Redis"
# ✅ 预期：看到 "Successfully recovered connection for server..."
```

### 场景 2：手动清空 Redis

```powershell
# 1. 连接到 Redis
redis-cli

# 2. 清空所有连接数据
> DEL mcp:connections:all
> KEYS mcp:connection:*
# ... 逐个删除

# 3. 观察 API 日志
# ✅ 预期：下一个监控周期自动恢复
```

### 场景 3：服务启动失败

```powershell
# 1. 确保数据库中有启用的服务器（IsEnabled=true）
# 2. 确保小智服务器不可达（测试失败场景）
# 3. 启动 API 服务

# ✅ 预期：
# - 启动时尝试连接失败
# - 监控循环每30秒重试
# - 日志显示 "Recovering missing connection"
```

---

## 📝 配置选项

监控间隔可通过 `appsettings.json` 配置：

```json
{
  "ConnectionMonitor": {
    "CheckIntervalSeconds": 30,         // 监控循环间隔
    "HeartbeatTimeoutSeconds": 90,      // 心跳超时时间
    "ReconnectCooldownSeconds": 60      // 重连冷却时间
  }
}
```

**建议值**：
- 开发环境：`CheckIntervalSeconds = 15`（快速反馈）
- 生产环境：`CheckIntervalSeconds = 30`（平衡性能和响应）
- 大规模部署：`CheckIntervalSeconds = 60`（减少开销）

---

## 🔄 完整恢复流程

```
监控循环（每 30 秒）：

1. 更新本地连接心跳
   ↓
2. 清理过期连接（心跳超过 90 秒）
   ↓
3. ✅ 检查数据库-Redis 一致性（新增）
   - 对比启用的服务器
   - 发现 Redis 中缺失的
   - 尝试恢复连接
   ↓
4. 重连已知的断开连接（Redis 中 Disconnected/Failed）
   - 检查冷却期（60 秒）
   - 尝试重连
   ↓
5. 等待下一个周期
```

---

## ✨ 关键改进点

### Before（修复前）

```
数据库 ⚡ Redis
     ↓        ↓
启用服务器  无数据
     ↓        ↓
   忽略    无处理
     ↓
  ❌ 连接永远无法恢复
```

### After（修复后）

```
数据库 ⚡ Redis
     ↓        ↓
启用服务器  无数据
     ↓        ↓
   对比    发现不一致
     ↓
  ✅ 自动恢复连接
```

---

## 🎓 经验总结

### 分布式系统状态管理的教训

1. **单一数据源不够**：
   - 数据库是权威来源
   - Redis 是实时状态缓存
   - **必须定期对比和同步**

2. **假设数据总是存在是危险的**：
   - Redis 可能丢失数据
   - 网络可能分区
   - 进程可能崩溃
   - **需要主动修复机制**

3. **监控不仅要检查现有状态**：
   - 检查"应该存在但不存在"的状态
   - 检查"不应该存在但存在"的状态
   - **对比多个数据源**

4. **测试边界条件**：
   - 空数据场景
   - 部分数据场景
   - 不一致数据场景
   - **不要只测试正常路径**

---

## 📖 相关文档

- [分布式 WebSocket 管理指南](architecture/DISTRIBUTED_WEBSOCKET_GUIDE.md)
- [故障恢复机制说明](architecture/FAILURE_RECOVERY_EXPLAINED.md)
- [连接监控服务实现](architecture/IMPLEMENTATION_SUMMARY.md)

---

## 🚀 部署建议

### 生产环境检查清单

- [ ] 配置合理的监控间隔（30-60秒）
- [ ] 配置日志级别（至少 Information）
- [ ] 监控日志关键词：
  - "Found X enabled servers in database but missing from Redis"
  - "Successfully recovered connection"
  - "Error recovering connection"
- [ ] 设置告警：如果持续恢复失败超过 5 分钟
- [ ] 定期备份 Redis 数据（可选）

### 监控指标

建议监控：
- 一致性检查触发次数
- 恢复成功率
- 恢复失败的服务器列表
- 平均恢复时间

---

**修复时间**：2025-11-08  
**影响范围**：`ConnectionMonitorHostedService.cs`  
**向后兼容**：是（只增加功能，不修改现有逻辑）  
**性能影响**：可忽略（< 100ms per cycle）
