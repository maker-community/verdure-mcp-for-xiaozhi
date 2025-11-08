# Database-Redis 一致性修复 - 快速参考

## 🎯 问题简述

**症状**: 数据库中服务器已启用（`IsEnabled=true`），但 Redis 中没有连接数据，导致连接无法自动恢复。

**根因**: 监控循环只检查 Redis 中已存在的状态，不会主动对比数据库和 Redis。

---

## ✅ 修复内容

新增 `CheckDatabaseRedisConsistencyAsync()` 方法，每个监控周期（默认30秒）：
1. 从数据库获取所有启用的服务器
2. 从 Redis 获取所有连接状态
3. 对比找出缺失的服务器
4. 自动尝试恢复连接

---

## 🧪 快速测试

### 方法 1: 使用测试脚本（推荐）

```powershell
# 运行交互式测试脚本
.\scripts\test-consistency-fix.ps1

# 选择选项 4: 完整测试流程
# 脚本会自动执行：清空Redis → 等待恢复 → 验证结果
```

### 方法 2: 手动测试

```powershell
# 1. 启动 API 服务
dotnet run --project src/Verdure.McpPlatform.Api

# 2. 清空 Redis（模拟数据丢失）
redis-cli
> DEL mcp:connections:all
> KEYS mcp:connection:*
# ... 手动删除所有 key

# 3. 观察 API 日志（约30秒内）
# 应该看到:
# [INFO] Found X enabled servers in database but missing from Redis
# [INFO] Recovering missing connection for enabled server...
# [INFO] Successfully recovered connection for server...

# 4. 验证 Redis 数据恢复
redis-cli
> KEYS mcp:connection:*
> GET mcp:connection:state:<serverId>
```

---

## 📊 预期日志输出

### 成功恢复
```
[15:30:00 INF] Connection monitor service starting
[15:30:10 INF] Checking for enabled servers to connect on startup
[15:30:12 INF] Found 2 enabled servers
[15:30:30 INF] Found 2 enabled servers in database but missing from Redis - attempting recovery
[15:30:30 INF] Recovering missing connection for enabled server abc123 (小智测试服务器)
[15:30:31 INF] Acquired distributed lock for server abc123
[15:30:31 INF] Created session for server abc123 (小智测试服务器)
[15:30:32 INF] Successfully recovered connection for server abc123 (小智测试服务器)
```

### 失败情况
```
[15:30:30 WRN] Failed to recover connection for server abc123 (小智测试服务器) - may be handled by another instance
# 或
[15:30:30 ERR] Error recovering connection for server abc123 (小智测试服务器)
System.Exception: Connection failed...
```

---

## 🔧 配置调整

如需调整监控频率，编辑 `appsettings.json`:

```json
{
  "ConnectionMonitor": {
    "CheckIntervalSeconds": 30,        // 改为 15 可以更快检测（开发环境）
    "HeartbeatTimeoutSeconds": 90,
    "ReconnectCooldownSeconds": 60
  }
}
```

---

## 🐛 故障排查

### 问题: 30秒后仍未恢复

**检查清单**:
- [ ] API 服务是否正在运行？
- [ ] 数据库中是否有 `IsEnabled=true` 的服务器？
- [ ] 监控服务是否启动？（日志中应有 "Connection monitor service starting"）
- [ ] 网络是否可达小智服务器？
- [ ] 是否有错误日志？

**诊断命令**:
```powershell
# 检查数据库启用的服务器
# 在 API 中运行或使用数据库工具查询：
# SELECT * FROM xiaozhi_mcp_endpoints WHERE is_enabled = true;

# 检查 Redis
redis-cli
> KEYS mcp:*
> GET mcp:connection:state:<serverId>

# 查看 API 日志级别
# 确保 appsettings.json 中日志级别至少为 Information
```

---

## 📈 性能影响

- **额外查询**: 每30秒1次数据库查询 + 1次Redis查询
- **额外开销**: < 100ms per cycle
- **内存**: O(n) 哈希集合对比，n = 服务器数量
- **影响**: 可忽略（通常 < 100 个服务器）

---

## 🎓 适用场景

✅ **自动修复**:
- Redis 重启（数据丢失）
- 手动清空 Redis
- 服务启动时连接失败
- 手动启用服务器后连接创建失败

❌ **不适用**:
- 数据库中服务器被禁用（`IsEnabled=false`）
- 小智服务器长期不可达（会持续重试）
- 分布式锁获取失败（其他实例处理中）

---

## 📚 相关文档

- 详细分析: `docs/DATABASE_REDIS_CONSISTENCY_FIX.md`
- 分布式架构: `docs/architecture/DISTRIBUTED_WEBSOCKET_GUIDE.md`
- 故障恢复: `docs/architecture/FAILURE_RECOVERY_EXPLAINED.md`

---

## 🚀 生产环境建议

1. **监控告警**:
   ```
   日志关键词: "Found X enabled servers in database but missing from Redis"
   告警条件: 持续出现 > 5 分钟
   ```

2. **健康检查**:
   - 定期检查 Redis 连接
   - 监控一致性检查触发次数
   - 监控恢复成功率

3. **日志保留**:
   - 至少保留一致性检查日志 7 天
   - 记录所有恢复尝试和结果

4. **定期审计**:
   - 每周比对数据库和 Redis 数据
   - 检查是否有长期无法连接的服务器

---

**修复时间**: 2025-11-08  
**修复文件**: `ConnectionMonitorHostedService.cs`  
**向后兼容**: 是  
**测试脚本**: `scripts/test-consistency-fix.ps1`
