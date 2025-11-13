# WebSocket 断开检测修复 - 快速参考

## 🎯 问题

**症状**：WebSocket 连接断开后，系统显示仍然连接，永不恢复

**根本原因**：
1. WebSocket 断开时没有触发状态更新
2. 后台监控只更新正常连接的心跳，忽略断开的连接
3. 断开的 session 永久驻留内存，阻止重连

## ✅ 修复内容

### 6 个关键修复点

| # | 修复点 | 文件 | 效果 |
|---|--------|------|------|
| 1 | 添加 OnDisconnected 回调 | McpSessionService.cs | 断开时立即通知 |
| 2 | 处理 OnDisconnected 事件 | McpSessionManager.cs | 更新数据库和 Redis |
| 3 | 优化后台监控逻辑 | ConnectionMonitorHostedService.cs | 清理断开的 session |
| 4 | Ping 中检测状态 | McpSessionService.cs | Ping 失败立即断开 |
| 5 | 发送响应时检测 | McpSessionService.cs | 发送失败抛异常 |
| 6 | 增强日志 | McpSessionService.cs | 更好的诊断 |

## ⏱️ 修复效果

| 场景 | 修复前 | 修复后 | 改进 |
|------|--------|--------|------|
| 正常关闭 | 90秒 | **0秒** | 90秒 ↓ |
| Ping 失败 | 不检测 | **0秒** | ∞ → 0 |
| 网络中断 | 90秒 | **30秒** | 60秒 ↓ |
| 自动恢复 | ❌ 永不恢复 | ✅ 90秒后恢复 | 质的飞跃！ |

## 🧪 快速测试

```powershell
# 1. 启动 API
dotnet run --project src/Verdure.McpPlatform.Api

# 2. 运行测试脚本
.\scripts\test-disconnect-detection.ps1

# 3. 测试场景：关闭小智服务器
#    - 观察日志中的 "OnDisconnected" 
#    - 检查 UI 状态变为断开
#    - 等待 60 秒后自动重连

# 4. 查看日志
.\scripts\view-logs.ps1
```

## 📊 关键日志

### 成功的断开检测

```
[Warning] Server xxx: WebSocket connection ended, triggering disconnect notification
[Warning] Session for server xxx disconnected
[Debug] Updated database status to Disconnected
[Information] Successfully updated disconnect status in Redis
```

### 后台监控检测

```
[Warning] Detected disconnected session for server xxx in local heartbeat check
[Information] Removing disconnected session from local manager
```

### 自动重连

```
[Information] Attempting to reconnect server xxx, attempt #1
[Information] Successfully reconnected server xxx
```

## ⚙️ 可调参数

编辑 `appsettings.json`：

```json
{
  "ConnectionMonitor": {
    "CheckIntervalSeconds": 30,      // 检查间隔（推荐 15-30）
    "HeartbeatTimeoutSeconds": 90,   // 心跳超时（推荐 45-90）
    "ReconnectCooldownSeconds": 60   // 重连冷却（推荐 30-60）
  }
}
```

**快速恢复配置**：
- CheckIntervalSeconds: **15** （更快检测）
- HeartbeatTimeoutSeconds: **45** （更快判定）
- ReconnectCooldownSeconds: **30** （更快重连）

## 🔍 故障排查

### 问题：断开后没有自动重连

**检查清单**：
- [ ] 检查 `IsEnabled` 是否为 true
- [ ] 检查日志中是否有 "OnDisconnected" 
- [ ] 检查 Redis 中状态是否为 Disconnected
- [ ] 等待 60 秒冷却期
- [ ] 检查后台监控服务是否运行

**命令**：
```powershell
# 检查 Redis 状态
redis-cli GET "mcp:connection:<server-id>"

# 检查后台服务日志
.\scripts\view-logs.ps1 | Select-String "ConnectionMonitor"
```

### 问题：状态显示不一致

**可能原因**：
1. Redis 未运行
2. 多实例之间时钟不同步
3. 数据库事务未提交

**解决**：
```powershell
# 检查 Redis 连接
redis-cli PING

# 重启 API 服务
dotnet run --project src/Verdure.McpPlatform.Api
```

## 📝 验证步骤

### 最小验证流程（5分钟）

1. **启动服务**
   ```powershell
   dotnet run --project src/Verdure.McpPlatform.Api
   ```

2. **创建连接**
   - 打开 UI
   - 创建一个小智连接
   - 等待连接成功

3. **触发断开**
   - 关闭小智服务器

4. **验证检测**（< 1秒）
   - 查看日志是否有 "OnDisconnected"
   - UI 状态是否变为断开
   - Redis 状态是否更新

5. **验证重连**（90秒后）
   - 重启小智服务器
   - 等待自动重连
   - 检查日志 "Successfully reconnected"

### 完整测试流程（15分钟）

参考 `docs/WEBSOCKET_DISCONNECT_FIX_SUMMARY.md` 的测试场景 1-3

## 🎓 技术细节

### 三层检测机制

```
Layer 1: OnDisconnected 回调 → 0秒检测
    ↓
Layer 2: Ping 处理检测 → <30秒检测
    ↓
Layer 3: 后台监控 → 30秒周期检测
```

### 状态流转

```
Connected → Disconnected → (60s cooldown) → Reconnecting → Connected
                ↓                                ↓
           (清理session)                    (失败)
                                               ↓
                                            Failed
                                               ↓
                                          (重试 10 次)
```

## 📚 相关文档

- **详细分析**：`docs/WEBSOCKET_DISCONNECTION_ANALYSIS.md`
- **修复总结**：`docs/WEBSOCKET_DISCONNECT_FIX_SUMMARY.md`
- **测试脚本**：`scripts/test-disconnect-detection.ps1`
- **架构文档**：`docs/architecture/DISTRIBUTED_WEBSOCKET_GUIDE.md`

## 🆘 紧急恢复

如果修复后出现问题，可以临时回滚：

```powershell
# 1. 查看修改的文件
git diff HEAD

# 2. 回滚到之前的版本（谨慎！）
git checkout HEAD -- src/Verdure.McpPlatform.Api/Services

# 3. 重新构建
dotnet build
```

但建议先排查问题，因为修复是改进，不应该回滚。

## ✨ 最佳实践

1. **开启详细日志**（排查时）
   ```json
   "Logging": {
     "LogLevel": {
       "Verdure.McpPlatform.Api.Services": "Debug"
     }
   }
   ```

2. **监控 Redis**
   ```powershell
   # 实时监控 Redis 命令
   redis-cli MONITOR | Select-String "mcp:connection"
   ```

3. **定期检查连接**
   ```powershell
   # 每天运行一次健康检查
   .\scripts\health-check.ps1
   ```

---

**修复日期**：2025-11-14  
**编译状态**：✅ 成功（3个警告，非关键）  
**就绪状态**：✅ 可以部署测试
