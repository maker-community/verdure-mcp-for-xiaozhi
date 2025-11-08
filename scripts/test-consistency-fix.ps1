# Database-Redis 一致性修复验证脚本
# 用于测试 Redis 数据丢失后的自动恢复机制

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database-Redis 一致性修复验证" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查 Redis 是否运行
Write-Host "检查 Redis 状态..." -ForegroundColor Yellow
try {
    $redisTest = redis-cli ping
    if ($redisTest -eq "PONG") {
        Write-Host "✅ Redis 正在运行" -ForegroundColor Green
    } else {
        Write-Host "❌ Redis 未响应" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ 无法连接到 Redis。请确保 Redis 正在运行。" -ForegroundColor Red
    Write-Host "   可以使用: docker run -d -p 6379:6379 redis:7-alpine" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "测试场景选择" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. 模拟 Redis 数据丢失（清空所有 MCP 连接数据）" -ForegroundColor Yellow
Write-Host "2. 查看当前 Redis 中的连接状态" -ForegroundColor Yellow
Write-Host "3. 查看监控恢复日志（需要 API 正在运行）" -ForegroundColor Yellow
Write-Host "4. 完整测试流程（推荐）" -ForegroundColor Yellow
Write-Host ""

$choice = Read-Host "请选择测试场景 (1-4)"

switch ($choice) {
    "1" {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "清空 Redis MCP 连接数据" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        
        # 获取所有 MCP 相关的 key
        Write-Host "查找所有 MCP 连接数据..." -ForegroundColor Yellow
        $mcpKeys = redis-cli --scan --pattern "mcp:connection:*"
        $allConnectionsKey = "mcp:connections:all"
        
        if ($mcpKeys) {
            $keyCount = ($mcpKeys | Measure-Object).Count
            Write-Host "找到 $keyCount 个连接状态 key" -ForegroundColor Yellow
            
            Write-Host ""
            Write-Host "连接状态列表:" -ForegroundColor Cyan
            $mcpKeys | ForEach-Object {
                $key = $_
                $data = redis-cli GET $key
                if ($data) {
                    $json = $data | ConvertFrom-Json
                    Write-Host "  - $($json.ServerName) (ID: $($json.ServerId), Status: $($json.Status))" -ForegroundColor Gray
                }
            }
            
            Write-Host ""
            $confirm = Read-Host "确认要删除这些连接数据吗? (y/N)"
            
            if ($confirm -eq "y" -or $confirm -eq "Y") {
                # 删除所有连接状态
                $mcpKeys | ForEach-Object {
                    redis-cli DEL $_ | Out-Null
                }
                
                # 删除连接列表
                redis-cli DEL $allConnectionsKey | Out-Null
                
                Write-Host ""
                Write-Host "✅ 已清空所有 MCP 连接数据" -ForegroundColor Green
                Write-Host ""
                Write-Host "📊 测试结果观察:" -ForegroundColor Yellow
                Write-Host "  1. 如果 API 正在运行，查看日志（约30秒内）" -ForegroundColor Gray
                Write-Host "  2. 应该看到: 'Found X enabled servers in database but missing from Redis'" -ForegroundColor Gray
                Write-Host "  3. 应该看到: 'Successfully recovered connection for server...'" -ForegroundColor Gray
                Write-Host ""
                Write-Host "💡 提示: 运行脚本选项 3 查看实时日志" -ForegroundColor Cyan
            } else {
                Write-Host "已取消" -ForegroundColor Yellow
            }
        } else {
            Write-Host "❌ 没有找到 MCP 连接数据" -ForegroundColor Yellow
            Write-Host "   请确保有已启用的服务器并且 API 正在运行" -ForegroundColor Gray
        }
    }
    
    "2" {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "当前 Redis 连接状态" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        
        $mcpKeys = redis-cli --scan --pattern "mcp:connection:*"
        
        if ($mcpKeys) {
            $keyCount = ($mcpKeys | Measure-Object).Count
            Write-Host "找到 $keyCount 个活跃连接" -ForegroundColor Green
            Write-Host ""
            
            $mcpKeys | ForEach-Object {
                $key = $_
                $data = redis-cli GET $key
                
                if ($data) {
                    try {
                        $json = $data | ConvertFrom-Json
                        
                        Write-Host "📡 $($json.ServerName)" -ForegroundColor Cyan
                        Write-Host "   ID: $($json.ServerId)" -ForegroundColor Gray
                        Write-Host "   状态: $($json.Status)" -ForegroundColor $(if ($json.Status -eq "Connected") { "Green" } else { "Yellow" })
                        Write-Host "   实例: $($json.InstanceId)" -ForegroundColor Gray
                        Write-Host "   端点: $($json.WebSocketEndpoint)" -ForegroundColor Gray
                        Write-Host "   最后心跳: $($json.LastHeartbeat)" -ForegroundColor Gray
                        
                        if ($json.LastConnectedTime) {
                            Write-Host "   连接时间: $($json.LastConnectedTime)" -ForegroundColor Gray
                        }
                        
                        if ($json.ReconnectAttempts -gt 0) {
                            Write-Host "   重连次数: $($json.ReconnectAttempts)" -ForegroundColor Yellow
                        }
                        
                        Write-Host ""
                    } catch {
                        Write-Host "   ⚠️  无法解析数据: $data" -ForegroundColor Yellow
                        Write-Host ""
                    }
                }
            }
            
            # 显示连接列表
            $allConnections = redis-cli SMEMBERS "mcp:connections:all"
            if ($allConnections) {
                Write-Host "📋 连接列表 (mcp:connections:all):" -ForegroundColor Cyan
                $allConnections | ForEach-Object {
                    Write-Host "   - $_" -ForegroundColor Gray
                }
            }
        } else {
            Write-Host "❌ Redis 中没有连接数据" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "可能的原因:" -ForegroundColor Yellow
            Write-Host "  1. API 服务未运行" -ForegroundColor Gray
            Write-Host "  2. 没有启用的服务器" -ForegroundColor Gray
            Write-Host "  3. Redis 数据已被清空" -ForegroundColor Gray
        }
    }
    
    "3" {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "监控恢复日志（实时）" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "⚠️  此功能需要 API 服务正在运行" -ForegroundColor Yellow
        Write-Host "💡 按 Ctrl+C 退出日志监控" -ForegroundColor Cyan
        Write-Host ""
        
        # 等待用户确认
        Read-Host "按 Enter 开始监控日志..."
        
        Write-Host ""
        Write-Host "正在监控日志中的关键事件..." -ForegroundColor Yellow
        Write-Host "查找关键词:" -ForegroundColor Gray
        Write-Host "  - 'Found X enabled servers in database but missing from Redis'" -ForegroundColor Gray
        Write-Host "  - 'Recovering missing connection'" -ForegroundColor Gray
        Write-Host "  - 'Successfully recovered connection'" -ForegroundColor Gray
        Write-Host ""
        
        # 尝试查找日志文件（根据实际情况调整）
        # 或者提示用户查看控制台输出
        Write-Host "请在 API 服务的控制台输出中查找上述日志信息" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "典型的恢复日志示例:" -ForegroundColor Cyan
        Write-Host @"
[15:30:00 INF] Found 2 enabled servers in database but missing from Redis - attempting recovery
[15:30:00 INF] Recovering missing connection for enabled server abc123 (小智测试服务器)
[15:30:02 INF] Successfully recovered connection for server abc123 (小智测试服务器)
"@ -ForegroundColor Gray
    }
    
    "4" {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host "完整测试流程" -ForegroundColor Cyan
        Write-Host "========================================" -ForegroundColor Cyan
        Write-Host ""
        
        Write-Host "步骤 1: 检查当前状态" -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor Gray
        
        # 检查数据库（需要运行的 API）
        Write-Host "⚠️  请确保 API 服务正在运行" -ForegroundColor Yellow
        Write-Host ""
        
        # 检查 Redis
        $mcpKeys = redis-cli --scan --pattern "mcp:connection:*"
        if ($mcpKeys) {
            $keyCount = ($mcpKeys | Measure-Object).Count
            Write-Host "✅ Redis 中有 $keyCount 个连接状态" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Redis 中没有连接数据" -ForegroundColor Yellow
        }
        Write-Host ""
        
        Read-Host "按 Enter 继续到步骤 2..."
        
        Write-Host ""
        Write-Host "步骤 2: 清空 Redis 数据（模拟数据丢失）" -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor Gray
        
        if ($mcpKeys) {
            $mcpKeys | ForEach-Object {
                redis-cli DEL $_ | Out-Null
            }
            redis-cli DEL "mcp:connections:all" | Out-Null
            Write-Host "✅ 已清空 Redis 中的 MCP 连接数据" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Redis 中本来就没有数据" -ForegroundColor Yellow
        }
        Write-Host ""
        
        Read-Host "按 Enter 继续到步骤 3..."
        
        Write-Host ""
        Write-Host "步骤 3: 等待自动恢复（约 30 秒）" -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor Gray
        Write-Host "监控服务将在下一个周期检测到不一致并自动恢复..." -ForegroundColor Gray
        Write-Host ""
        
        for ($i = 30; $i -gt 0; $i--) {
            Write-Host -NoNewline "`r⏱️  倒计时: $i 秒   "
            Start-Sleep -Seconds 1
        }
        Write-Host ""
        Write-Host ""
        
        Write-Host "步骤 4: 验证恢复结果" -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor Gray
        
        $mcpKeys = redis-cli --scan --pattern "mcp:connection:*"
        if ($mcpKeys) {
            $keyCount = ($mcpKeys | Measure-Object).Count
            Write-Host "✅ 成功! Redis 中恢复了 $keyCount 个连接状态" -ForegroundColor Green
            Write-Host ""
            Write-Host "恢复的连接:" -ForegroundColor Cyan
            $mcpKeys | ForEach-Object {
                $key = $_
                $data = redis-cli GET $key
                if ($data) {
                    $json = $data | ConvertFrom-Json
                    Write-Host "  ✅ $($json.ServerName) - $($json.Status)" -ForegroundColor Green
                }
            }
        } else {
            Write-Host "❌ 失败: Redis 中仍然没有连接数据" -ForegroundColor Red
            Write-Host ""
            Write-Host "可能的原因:" -ForegroundColor Yellow
            Write-Host "  1. API 服务未运行" -ForegroundColor Gray
            Write-Host "  2. 数据库中没有启用的服务器 (IsEnabled=true)" -ForegroundColor Gray
            Write-Host "  3. 连接创建失败（检查 API 日志）" -ForegroundColor Gray
            Write-Host "  4. 监控间隔配置过长（检查 appsettings.json）" -ForegroundColor Gray
        }
        
        Write-Host ""
        Write-Host "📊 测试完成!" -ForegroundColor Green
        Write-Host "查看 API 日志获取详细的恢复过程记录" -ForegroundColor Yellow
    }
    
    default {
        Write-Host "无效的选择" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "测试完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📖 详细文档: docs/DATABASE_REDIS_CONSISTENCY_FIX.md" -ForegroundColor Cyan
Write-Host ""
