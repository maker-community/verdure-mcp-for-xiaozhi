# DateTime 国际化快速参考

## 🎯 核心原则

```
后端（UTC） → API（UTC） → 前端（本地时间）
```

---

## 📦 使用 DateTimeFormatter 服务

### 注入服务

```razor
@inject IDateTimeFormatter DateTimeFormatter
```

### 常用方法

| 方法 | 输出示例 | 用途 |
|------|---------|------|
| `FormatShortDate()` | `2025-11-09` | 短日期 |
| `FormatDateTime()` | `2025-11-09 14:30` | 标准格式 |
| `FormatFriendlyDate()` | `Nov 09, 2025` | 友好日期 |
| `FormatFriendlyDateTime()` | `Nov 09, 2025 14:30` | 友好格式 |
| `FormatRelativeTime()` | `2 hours ago` / `2 小时前` | 相对时间 |

### 代码示例

```razor
<!-- 基础用法 -->
<MudText>@DateTimeFormatter.FormatDateTime(item.CreatedAt)</MudText>

<!-- 可空时间 -->
<MudText>@DateTimeFormatter.FormatDateTime(item.UpdatedAt, "未更新")</MudText>

<!-- 带 Tooltip -->
<MudText title="@DateTimeFormatter.FormatDateTime(item.CreatedAt)">
    @DateTimeFormatter.FormatFriendlyDate(item.CreatedAt)
</MudText>

<!-- 相对时间 -->
<MudText>@DateTimeFormatter.FormatRelativeTime(item.LastActiveAt)</MudText>
```

---

## 🎨 使用 DateTimeDisplay 组件

```razor
<!-- 友好日期格式 -->
<DateTimeDisplay UtcDateTime="@item.CreatedAt" 
                 Format="DateTimeFormat.FriendlyDate" />

<!-- 完整日期时间 -->
<DateTimeDisplay UtcDateTime="@item.CreatedAt" 
                 Format="DateTimeFormat.FriendlyDateTime" />

<!-- 相对时间 -->
<DateTimeDisplay UtcDateTime="@item.LastActiveAt" 
                 ShowRelative="true" />

<!-- 自定义默认值 -->
<DateTimeDisplay UtcDateTime="@item.UpdatedAt" 
                 DefaultValue="从未更新" />
```

---

## ✅ 最佳实践

### 后端

```csharp
// ✅ 正确
CreatedAt = DateTime.UtcNow;

// ❌ 错误
CreatedAt = DateTime.Now;
```

### 前端

```razor
@* ✅ 正确 - 使用 DateTimeFormatter *@
<MudText>@DateTimeFormatter.FormatDateTime(item.CreatedAt)</MudText>

@* ✅ 正确 - 使用组件 *@
<DateTimeDisplay UtcDateTime="@item.CreatedAt" />

@* ✅ 正确 - 直接转换 *@
<MudText>@item.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")</MudText>

@* ❌ 错误 - 直接显示 UTC 时间 *@
<MudText>@item.CreatedAt.ToString("yyyy-MM-dd HH:mm")</MudText>
```

---

## 🧪 测试不同时区

### Chrome DevTools

1. F12 打开 DevTools
2. Settings (⚙️) → Experiments
3. 启用 "Emulate timezone"
4. Console → ... → Sensors → Location
5. 选择时区测试

### 常用时区

- `Asia/Shanghai` - 中国 (UTC+8)
- `America/New_York` - 美国东部 (UTC-5)
- `Europe/London` - 英国 (UTC+0)
- `Pacific/Auckland` - 新西兰 (UTC+13)

---

## 📚 详细文档

查看完整指南: `docs/guides/DATETIME_BEST_PRACTICES.md`
