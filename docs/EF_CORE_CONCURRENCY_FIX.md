# EF Core 并发问题修复

## 问题描述

在使用 PostgreSQL 时，出现以下错误：

```
System.InvalidOperationException: "A second operation was started on this context instance 
before a previous operation completed. This is usually caused by different threads concurrently 
using the same instance of DbContext."
```

## 根本原因

在 `McpServiceBindingService.MapToDtoAsync` 方法中，使用 `Task.WhenAll` 并行执行两个数据库查询：

```csharp
// ❌ 错误的实现 - 并发访问同一个 DbContext
private async Task<McpServiceBindingDto> MapToDtoAsync(McpServiceBinding binding)
{
    var endpointTask = _repository.GetAsync(binding.XiaozhiMcpEndpointId);
    var configTask = _configRepository.GetByIdAsync(binding.McpServiceConfigId);
    
    await Task.WhenAll(endpointTask, configTask);  // 两个任务并发执行
    
    var endpoint = await endpointTask;
    var config = await configTask;
    // ...
}
```

### 为什么会出错？

1. **DbContext 不是线程安全的**：EF Core 的 DbContext 设计为每个请求（Scoped lifetime）使用一个实例
2. **并发操作冲突**：两个仓储（`_repository` 和 `_configRepository`）注入了同一个 DbContext 实例
3. **Task.WhenAll 触发并发**：当两个任务同时执行查询时，它们尝试同时使用同一个 DbContext 连接

### 为什么 SQLite 不报错，PostgreSQL 报错？

- **SQLite**：使用文件锁，单连接模式，内部有队列机制，错误不明显
- **PostgreSQL**：真正的客户端-服务器数据库，严格的并发控制，立即检测到并发问题

## 修复方案

### 方案 1：串行执行查询（✅ 已采用）

将并行查询改为串行执行：

```csharp
// ✅ 正确的实现 - 串行执行
private async Task<McpServiceBindingDto> MapToDtoAsync(McpServiceBinding binding)
{
    // Fetch endpoint and config sequentially to avoid EF Core concurrency issues
    // Note: Parallel execution with Task.WhenAll causes "A second operation was started" 
    // error with PostgreSQL as the same DbContext instance is used concurrently
    var endpoint = await _repository.GetAsync(binding.XiaozhiMcpEndpointId);
    var config = await _configRepository.GetByIdAsync(binding.McpServiceConfigId);
    
    return new McpServiceBindingDto
    {
        // ...
    };
}
```

**优点**：
- ✅ 简单、可靠
- ✅ 不需要改变 DbContext 注册方式
- ✅ 符合 EF Core 最佳实践

**缺点**：
- ❌ 性能稍慢（两次顺序查询而非并行）
- ❌ 在大量数据映射时可能影响响应时间

### 方案 2：为每个仓储创建独立的 DbContext

注册 DbContext 时使用 Transient 生命周期：

```csharp
// ⚠️ 不推荐 - 会导致其他问题
services.AddDbContext<McpPlatformContext>(options =>
{
    options.UseNpgsql(connectionString);
}, ServiceLifetime.Transient);  // 每次注入都创建新实例
```

**优点**：
- ✅ 可以并行执行

**缺点**：
- ❌ 破坏了 UnitOfWork 模式
- ❌ 事务管理变得复杂
- ❌ 增加数据库连接开销
- ❌ 不符合 EF Core 推荐模式

### 方案 3：使用 AsSplitQuery（部分场景适用）

对于 Include 的关联查询，使用 AsSplitQuery：

```csharp
public async Task<McpServiceConfig?> GetByIdAsync(string id)
{
    var config = await _context.McpServiceConfigs
        .Include(s => s.Tools)
        .AsSplitQuery()  // 将一个查询拆分为多个查询
        .FirstOrDefaultAsync(s => s.Id == id);

    return config;
}
```

**注意**：这个方法**不能**解决本次的问题，因为我们的问题是两个不同的查询并发执行。

## EF Core 最佳实践

### ✅ 推荐做法

1. **使用 Scoped DbContext**（默认）
   ```csharp
   services.AddDbContext<McpPlatformContext>(options => 
       options.UseNpgsql(connectionString));
   // 默认 ServiceLifetime.Scoped
   ```

2. **避免并发操作同一个 DbContext**
   ```csharp
   // ❌ 不要这样做
   await Task.WhenAll(
       _repository1.GetAsync(id1),
       _repository2.GetAsync(id2)
   );

   // ✅ 应该这样做
   var result1 = await _repository1.GetAsync(id1);
   var result2 = await _repository2.GetAsync(id2);
   ```

3. **只读查询使用 AsNoTracking**
   ```csharp
   public async Task<IEnumerable<McpServiceConfig>> GetByUserAsync(string userId)
   {
       return await _context.McpServiceConfigs
           .AsNoTracking()  // 不跟踪变更，性能更好
           .Include(s => s.Tools)
           .Where(s => s.UserId == userId)
           .ToListAsync();
   }
   ```

4. **复杂关联使用 AsSplitQuery**
   ```csharp
   var entity = await _context.Entities
       .Include(e => e.Collection1)
       .Include(e => e.Collection2)
       .AsSplitQuery()  // 避免笛卡尔积
       .FirstOrDefaultAsync(e => e.Id == id);
   ```

### ❌ 应避免的做法

1. **不要在多个线程间共享 DbContext**
2. **不要在循环中创建 DbContext**（应该注入）
3. **不要对同一个实体同时执行多个查询**
4. **不要在 Singleton 服务中注入 DbContext**

## 性能影响分析

### 修复前（并行查询）
```
端点查询：50ms ──┐
                   ├─> Task.WhenAll
配置查询：50ms ──┘
总耗时：约 50ms（并行）
```

### 修复后（串行查询）
```
端点查询：50ms ──> 完成
配置查询：50ms ──> 完成
总耗时：约 100ms（串行）
```

### 性能优化建议

如果性能成为瓶颈，可以考虑：

1. **使用投影减少数据传输**
   ```csharp
   var endpoint = await _context.XiaozhiMcpEndpoints
       .Where(e => e.Id == endpointId)
       .Select(e => new { e.Id, e.Name })
       .FirstOrDefaultAsync();
   ```

2. **添加数据库索引**
   - 确保 `XiaozhiMcpEndpointId` 和 `McpServiceConfigId` 有索引

3. **使用缓存**
   ```csharp
   // 对于不常变化的配置数据
   var config = await _cache.GetOrCreateAsync(
       $"config:{configId}",
       async entry => {
           entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
           return await _configRepository.GetByIdAsync(configId);
       });
   ```

4. **批量加载（如果有多个绑定需要映射）**
   ```csharp
   // 先收集所有 ID
   var endpointIds = bindings.Select(b => b.XiaozhiMcpEndpointId).Distinct();
   var configIds = bindings.Select(b => b.McpServiceConfigId).Distinct();
   
   // 批量查询
   var endpoints = await _context.XiaozhiMcpEndpoints
       .Where(e => endpointIds.Contains(e.Id))
       .ToListAsync();
   var configs = await _context.McpServiceConfigs
       .Where(c => configIds.Contains(c.Id))
       .ToListAsync();
   
   // 在内存中映射
   // ...
   ```

## 测试验证

### 验证步骤

1. **启动 PostgreSQL**
   ```powershell
   docker run -d -p 5432:5432 `
     -e POSTGRES_PASSWORD=postgres `
     postgres:16
   ```

2. **配置连接字符串**
   ```json
   {
     "Database": {
       "Provider": "PostgreSQL"
     },
     "ConnectionStrings": {
       "mcpdb": "Host=localhost;Database=verdure_mcp;Username=postgres;Password=postgres"
     }
   }
   ```

3. **运行应用并测试**
   ```powershell
   dotnet run --project src/Verdure.McpPlatform.Api
   ```

4. **触发问题场景**
   - 创建绑定
   - 查询绑定列表（会调用 `MapToDtoAsync`）
   - 观察是否还有并发错误

## 相关文件

### 已修改
- `src/Verdure.McpPlatform.Application/Services/McpServiceBindingService.cs`

### 相关代码
- `src/Verdure.McpPlatform.Infrastructure/Repositories/McpServiceConfigRepository.cs`
- `src/Verdure.McpPlatform.Infrastructure/Repositories/XiaozhiMcpEndpointRepository.cs`
- `src/Verdure.McpPlatform.Infrastructure/Data/McpPlatformContext.cs`
- `src/Verdure.McpPlatform.Api/Extensions/DatabaseExtensions.cs`

## 参考资料

- [EF Core: DbContext Lifetime, Configuration, and Initialization](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/)
- [EF Core: Threading Issues](https://learn.microsoft.com/en-us/ef/core/miscellaneous/threading)
- [EF Core: Split Queries](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries)
- [Best Practices for DbContext](https://learn.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency)

## 总结

这个问题是典型的 EF Core 并发问题，由于在同一个 DbContext 实例上并行执行多个操作导致。通过将并行查询改为串行执行，问题得到完全解决。虽然性能略有下降，但保证了代码的正确性和可靠性。如果未来性能成为瓶颈，可以采用缓存、批量加载等优化策略。

---

**修复日期**: 2025-11-07  
**修复版本**: v1.0.0  
**影响范围**: PostgreSQL 数据库使用场景
