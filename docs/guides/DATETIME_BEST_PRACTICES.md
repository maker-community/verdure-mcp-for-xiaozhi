# DateTime 最佳实践指南 (DateTime Best Practices)

## 📋 概述

本文档说明 Verdure MCP Platform 项目中时间处理的最佳实践，确保全球用户都能看到正确的本地时间。

---

## 🎯 核心原则

### 1. 三层时间架构

```
┌─────────────────────────────────────────────────────────┐
│                    后端 (Backend)                         │
│                  所有时间使用 UTC                          │
│              DateTime.UtcNow / DateTimeOffset.UtcNow     │
└─────────────────────┬───────────────────────────────────┘
                      │
                      │ API 传输 (ISO 8601 格式)
                      │
┌─────────────────────▼───────────────────────────────────┐
│                   API Layer                              │
│              传输 UTC 时间 (JSON)                         │
│         "2025-11-09T14:30:00Z"                          │
└─────────────────────┬───────────────────────────────────┘
                      │
                      │ HTTP Response
                      │
┌─────────────────────▼───────────────────────────────────┐
│               前端 (Frontend)                            │
│         转换为浏览器本地时区显示                          │
│    DateTime.ToLocalTime() / IDateTimeFormatter          │
└─────────────────────────────────────────────────────────┘
```

### 2. 为什么使用 UTC？

✅ **优势**:
- **全球一致性**: 无论服务器部署在哪个时区，数据库存储的时间都是统一的
- **避免夏令时问题**: UTC 没有夏令时，避免时间跳跃问题
- **易于比较**: 不同时区的时间可以直接比较
- **分布式系统友好**: 多服务器部署时不会因为时区不同导致数据不一致

❌ **如果不使用 UTC 的问题**:
- 服务器在中国部署时存储 `CST`，迁移到美国后所有历史数据时区错误
- 夏令时切换时可能出现时间重复或跳跃
- 多服务器时区不同导致日志时间混乱

---

## 🔧 实现指南

### 后端实现

#### ✅ 正确的做法

```csharp
// Domain/AggregatesModel/XiaozhiMcpEndpointAggregate/XiaozhiMcpEndpoint.cs
public class XiaozhiMcpEndpoint : Entity, IAggregateRoot
{
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastConnectedAt { get; private set; }

    private XiaozhiMcpEndpoint()
    {
        GenerateId();
        CreatedAt = DateTime.UtcNow; // ✅ 使用 UTC
    }

    public void MarkConnected()
    {
        IsConnected = true;
        LastConnectedAt = DateTime.UtcNow; // ✅ 使用 UTC
        UpdatedAt = DateTime.UtcNow; // ✅ 使用 UTC
    }
}
```

#### ❌ 错误的做法

```csharp
// ❌ 不要使用 DateTime.Now
CreatedAt = DateTime.Now; // 使用服务器本地时间

// ❌ 不要使用 DateTimeOffset.Now
CreatedAt = DateTimeOffset.Now.DateTime;
```

#### 数据库配置

```csharp
// Infrastructure/Data/EntityConfigurations/XiaozhiMcpEndpointEntityTypeConfiguration.cs
public class XiaozhiMcpEndpointEntityTypeConfiguration 
    : IEntityTypeConfiguration<XiaozhiMcpEndpoint>
{
    public void Configure(EntityTypeBuilder<XiaozhiMcpEndpoint> builder)
    {
        // DateTime 属性配置
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp without time zone"); // PostgreSQL
            // .HasColumnType("datetime"); // SQL Server

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp without time zone");

        builder.Property(e => e.LastConnectedAt)
            .HasColumnType("timestamp without time zone");
    }
}
```

### 前端实现

#### 方案 1: 使用 DateTimeFormatter 服务（推荐）

```razor
@inject IDateTimeFormatter DateTimeFormatter

<MudText Typo="Typo.body1" title="@DateTimeFormatter.FormatDateTime(Connection.CreatedAt)">
    @DateTimeFormatter.FormatFriendlyDate(Connection.CreatedAt)
</MudText>

<!-- 相对时间显示 -->
<MudText Typo="Typo.caption">
    @DateTimeFormatter.FormatRelativeTime(Connection.LastConnectedAt, "-")
</MudText>
```

#### 方案 2: 使用 DateTimeDisplay 组件

```razor
<DateTimeDisplay UtcDateTime="@Connection.CreatedAt" 
                 Format="DateTimeDisplay.DateTimeFormat.FriendlyDate" />

<!-- 显示相对时间 -->
<DateTimeDisplay UtcDateTime="@Connection.LastConnectedAt" 
                 ShowRelative="true" />
```

#### 方案 3: 直接使用 ToLocalTime()

```razor
@* 简单场景可以直接使用 *@
<MudText>@Connection.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")</MudText>

@* 可空时间需要处理 *@
@if (Connection.LastConnectedAt.HasValue)
{
    <MudText>@Connection.LastConnectedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm")</MudText>
}
```

---

## 📦 DateTimeFormatter 服务

### 服务接口

```csharp
public interface IDateTimeFormatter
{
    // 短日期格式：2025-11-09
    string FormatShortDate(DateTime utcDateTime);
    string FormatShortDate(DateTime? utcDateTime, string defaultValue = "-");
    
    // 日期时间格式：2025-11-09 14:30
    string FormatDateTime(DateTime utcDateTime);
    string FormatDateTime(DateTime? utcDateTime, string defaultValue = "-");
    
    // 友好格式：Nov 09, 2025
    string FormatFriendlyDate(DateTime utcDateTime);
    string FormatFriendlyDate(DateTime? utcDateTime, string defaultValue = "-");
    
    // 友好日期时间：Nov 09, 2025 14:30
    string FormatFriendlyDateTime(DateTime utcDateTime);
    string FormatFriendlyDateTime(DateTime? utcDateTime, string defaultValue = "-");
    
    // 相对时间：2 hours ago / 2 小时前
    string FormatRelativeTime(DateTime utcDateTime, CultureInfo? culture = null);
    string FormatRelativeTime(DateTime? utcDateTime, string defaultValue = "-", CultureInfo? culture = null);
    
    // 转换为本地时间
    DateTime ToLocalTime(DateTime utcDateTime);
    DateTime? ToLocalTime(DateTime? utcDateTime);
}
```

### 注册服务

```csharp
// Program.cs
builder.Services.AddScoped<IDateTimeFormatter, DateTimeFormatter>();
```

### 使用示例

```csharp
@inject IDateTimeFormatter DateTimeFormatter

<div>
    <!-- 标准日期时间 -->
    <p>Created: @DateTimeFormatter.FormatDateTime(item.CreatedAt)</p>
    
    <!-- 相对时间（支持中英文） -->
    <p>Last activity: @DateTimeFormatter.FormatRelativeTime(item.LastActiveAt)</p>
    
    <!-- 带 tooltip 的友好显示 -->
    <p title="@DateTimeFormatter.FormatDateTime(item.UpdatedAt)">
        @DateTimeFormatter.FormatFriendlyDate(item.UpdatedAt)
    </p>
</div>
```

---

## 🌐 Blazor WebAssembly 中的时区处理

### 工作原理

Blazor WebAssembly 在浏览器中运行，`DateTime.ToLocalTime()` 会自动使用浏览器的时区设置：

```csharp
// 后端返回 UTC 时间: "2025-11-09T06:30:00Z"
var utcTime = DateTime.Parse("2025-11-09T06:30:00Z", null, DateTimeStyles.RoundtripKind);

// 浏览器在中国（UTC+8）
var localTime = utcTime.ToLocalTime(); // 2025-11-09 14:30:00

// 浏览器在美国东部（UTC-5）
var localTime = utcTime.ToLocalTime(); // 2025-11-09 01:30:00
```

### JavaScript Interop（可选）

如果需要更精细的控制，可以使用 JavaScript Interop：

```javascript
// wwwroot/js/timezone.js
export function getUserTimezone() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}

export function formatToLocalTime(utcDateString, format) {
    const date = new Date(utcDateString);
    return new Intl.DateTimeFormat('zh-CN', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    }).format(date);
}
```

```csharp
@inject IJSRuntime JS

private async Task<string> GetUserTimezoneAsync()
{
    var module = await JS.InvokeAsync<IJSObjectReference>(
        "import", "./js/timezone.js");
    return await module.InvokeAsync<string>("getUserTimezone");
}
```

---

## 🧪 测试场景

### 测试不同时区

```powershell
# 设置浏览器时区（Chrome DevTools）
1. 打开 Chrome DevTools (F12)
2. Settings (⚙️) → Experiments
3. 启用 "Emulate timezone"
4. Console → 点击 "..." → Sensors → Location
5. 选择不同的时区进行测试

# 常用测试时区
- Asia/Shanghai (UTC+8) - 中国
- America/New_York (UTC-5) - 美国东部
- Europe/London (UTC+0) - 英国
- Pacific/Auckland (UTC+13) - 新西兰
```

### 单元测试

```csharp
[TestMethod]
public void FormatDateTime_UtcTime_ConvertsToLocalTime()
{
    // Arrange
    var formatter = new DateTimeFormatter();
    var utcTime = new DateTime(2025, 11, 9, 6, 30, 0, DateTimeKind.Utc);
    
    // Act
    var result = formatter.FormatDateTime(utcTime);
    
    // Assert
    var expectedLocal = utcTime.ToLocalTime();
    Assert.AreEqual(expectedLocal.ToString("yyyy-MM-dd HH:mm"), result);
}

[TestMethod]
public void FormatRelativeTime_RecentTime_ReturnsMinutesAgo()
{
    // Arrange
    var formatter = new DateTimeFormatter();
    var utcTime = DateTime.UtcNow.AddMinutes(-30);
    
    // Act
    var result = formatter.FormatRelativeTime(utcTime);
    
    // Assert
    Assert.IsTrue(result.Contains("30 minute") || result.Contains("30 分钟"));
}
```

---

## ⚠️ 常见错误和解决方案

### 错误 1: 直接使用 DateTime.Now

```csharp
// ❌ 错误
public DateTime CreatedAt { get; set; } = DateTime.Now;

// ✅ 正确
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
```

### 错误 2: 前端不转换为本地时间

```razor
@* ❌ 错误 - 直接显示 UTC 时间 *@
<MudText>@Connection.CreatedAt.ToString("yyyy-MM-dd HH:mm")</MudText>

@* ✅ 正确 - 转换为本地时间 *@
<MudText>@Connection.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")</MudText>

@* ✅ 更好 - 使用 DateTimeFormatter *@
<MudText>@DateTimeFormatter.FormatDateTime(Connection.CreatedAt)</MudText>
```

### 错误 3: 混合使用 DateTime.Kind

```csharp
// ❌ 错误 - Kind 不明确
var time = DateTime.Parse("2025-11-09 14:30:00");

// ✅ 正确 - 明确指定 Kind
var utcTime = DateTime.SpecifyKind(time, DateTimeKind.Utc);
var localTime = DateTime.SpecifyKind(time, DateTimeKind.Local);

// ✅ 最佳 - 使用 DateTimeOffset
var time = DateTimeOffset.Parse("2025-11-09T14:30:00+08:00");
```

### 错误 4: JSON 序列化问题

```csharp
// ASP.NET Core 默认已正确配置
// Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
        
        // DateTime 自动序列化为 ISO 8601 格式
        // "2025-11-09T06:30:00Z"
    });
```

---

## 📚 最佳实践总结

### ✅ Do's（应该做的）

1. **后端始终使用 UTC**
   ```csharp
   CreatedAt = DateTime.UtcNow;
   ```

2. **前端转换为本地时间**
   ```csharp
   @DateTimeFormatter.FormatDateTime(item.CreatedAt)
   ```

3. **使用明确的 DateTimeKind**
   ```csharp
   var utcTime = DateTime.SpecifyKind(time, DateTimeKind.Utc);
   ```

4. **添加 Tooltip 显示完整时间**
   ```razor
   <MudText title="@DateTimeFormatter.FormatDateTime(item.CreatedAt)">
       @DateTimeFormatter.FormatFriendlyDate(item.CreatedAt)
   </MudText>
   ```

5. **数据库存储时不带时区信息**
   ```sql
   timestamp without time zone  -- PostgreSQL
   datetime                     -- SQL Server
   ```

### ❌ Don'ts（不应该做的）

1. **不要使用 DateTime.Now**
   ```csharp
   // ❌ 错误
   CreatedAt = DateTime.Now;
   ```

2. **不要在前端直接显示 UTC 时间**
   ```razor
   @* ❌ 错误 *@
   <MudText>@item.CreatedAt.ToString("yyyy-MM-dd HH:mm")</MudText>
   ```

3. **不要假设用户时区**
   ```csharp
   // ❌ 错误 - 硬编码时区
   var chinaTime = utcTime.AddHours(8);
   ```

4. **不要在 API 中返回本地时间**
   ```csharp
   // ❌ 错误
   return new { timestamp = DateTime.Now };
   
   // ✅ 正确
   return new { timestamp = DateTime.UtcNow };
   ```

---

## 🔍 验证清单

在部署前检查：

- [ ] 所有实体的时间属性都使用 `DateTime.UtcNow`
- [ ] API 返回的时间都是 UTC（JSON 中有 "Z" 后缀）
- [ ] 前端显示时间都转换为本地时间
- [ ] 时间比较操作都在同一时区（推荐 UTC）
- [ ] 数据库列类型正确（`timestamp without time zone`）
- [ ] 在不同时区测试过应用
- [ ] 日志中的时间戳都是 UTC

---

## 📖 相关资源

- [Microsoft - DateTime Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/datetime/best-practices)
- [ISO 8601 标准](https://www.iso.org/iso-8601-date-and-time-format.html)
- [IANA Time Zone Database](https://www.iana.org/time-zones)
- [MDN - Intl.DateTimeFormat](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl/DateTimeFormat)

---

## 📝 变更日志

### 2025-11-09
- ✅ 后端已全部使用 UTC 时间
- ✅ 创建 `IDateTimeFormatter` 服务
- ✅ 创建 `DateTimeDisplay` 组件
- ✅ 更新前端组件使用时间格式化服务
- ✅ 添加相对时间显示功能（如：2 hours ago）
- ✅ 支持中英文相对时间
