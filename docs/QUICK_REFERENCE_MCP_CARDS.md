# MCP 服务卡片页面 - 快速参考

## 🎯 核心改动

### 页面结构
```
[Hero Banner]
  - 标题 + 统计信息
  - "添加服务" 按钮

[Tab + 搜索栏]
  - Tab: 我的服务 | 公开服务
  - 搜索框 + 排序选择器

[卡片网格 - 虚拟化滚动]
  - 响应式网格 (1-4 列)
  - 无限滚动加载
```

### 关键组件

#### ServiceConfigCard
```razor
<ServiceConfigCard 
    Service="@service"
    IsPublicView="@IsPublicTab"
    OnViewDetails="@(() => ViewDetails(service.Id))"
    OnSyncTools="@(() => SyncTools(service))"
    OnEdit="@(() => EditService(service.Id))"
    OnDelete="@(() => DeleteService(service))" />
```

**Props**:
- `Service` - MCP 服务配置对象
- `IsPublicView` - 是否为公开服务视图（隐藏敏感信息）
- `OnViewDetails` - 查看详情回调
- `OnSyncTools` - 同步工具回调（仅私有）
- `OnEdit` - 编辑回调（仅私有）
- `OnDelete` - 删除回调（仅私有）

## 🔌 新增 API 端点

### 公开服务分页
```http
GET /api/mcp-services/public/paged?Page={page}&PageSize={pageSize}&SearchTerm={term}&SortBy={field}&SortOrder={order}
```

**响应**:
```json
{
  "items": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 12,
  "totalPages": 9
}
```

## 📋 DTO 变更

### McpServiceConfigDto
```csharp
public record McpServiceConfigDto
{
    // ... 现有字段
    public string? LogoUrl { get; init; }  // 新增
}
```

## 🎨 样式类

```css
.service-config-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12) !important;
}
```

## 🌐 本地化键

| 键 | 中文 | 用途 |
|---|------|-----|
| `TotalMcpServices` | 共 {0} 个 MCP 服务 | 统计显示 |
| `MyServices` | 我的服务 | Tab 标签 |
| `PublicServices` | 公开服务 | Tab 标签 |
| `NoPublicServicesYet` | 暂无公开服务 | 空状态 |
| `CreateFirstService` | 创建您的第一个服务 | 空状态按钮 |
| `NoMatchingServices` | 未找到与 "{0}" 匹配的服务 | 搜索无结果 |

## 🔐 安全规则

### 公开服务限制
- ❌ 不显示 `Endpoint`
- ❌ 不显示认证配置
- ❌ 不显示"最后同步"时间
- ✅ 只显示查看详情按钮
- ✅ 只能查看工具列表

### 私有服务
- ✅ 显示完整信息
- ✅ 完整操作菜单（查看/同步/编辑/删除）

## 🔄 数据流

```
Tab 切换
  ↓
_activeTabIndex 改变
  ↓
OnAfterRenderAsync 检测
  ↓
RefreshData()
  ↓
LoadServicesVirtualized()
  ↓
根据 IsPublicTab 调用不同 API
  ├─ false → GetServicesPagedAsync()
  └─ true → GetPublicServicesPagedAsync()
```

## 🛠️ 调试技巧

### 控制台日志
```csharp
Console.WriteLine($"🔄 LoadServicesVirtualized: Tab={_activeTabIndex}");
Console.WriteLine($"🔍 Search changed: '{_searchTerm}'");
Console.WriteLine($"📑 Tab changed from {_previousTabIndex} to {_activeTabIndex}");
```

### 检查虚拟化状态
- 查看浏览器控制台日志
- 检查网络请求（应该是 12 项/页）
- 滚动时观察新数据加载

## 📱 响应式断点

| 断点 | 屏幕宽度 | 列数 |
|------|---------|------|
| xs | < 600px | 1 |
| sm | 600-960px | 2 |
| md | 960-1280px | 3 |
| lg | > 1280px | 4 |

## 🚨 常见问题

### Q: 卡片不显示？
A: 检查 API 返回数据，确认 `totalCount > 0`

### Q: Tab 切换无反应？
A: 检查 `OnAfterRenderAsync` 中的 Tab 变化检测逻辑

### Q: 滚动不加载？
A: 检查容器高度设置，确保 `overflow-y: auto`

### Q: 公开服务显示敏感信息？
A: 检查 `IsPublicView` 参数是否正确传递

## 📦 相关文件

**后端**:
- `McpServiceConfigApi.cs` - API 端点
- `McpServiceConfigService.cs` - 业务逻辑
- `McpServiceConfigRepository.cs` - 数据访问

**前端**:
- `McpServiceConfigs.razor` - 主页面
- `ServiceConfigCard.razor` - 卡片组件
- `McpServiceConfigClientService.cs` - HTTP 客户端

**资源**:
- `SharedResources.zh-CN.resx` - 中文本地化

---

**完整文档**: [MCP_SERVICE_CARD_REDESIGN.md](./MCP_SERVICE_CARD_REDESIGN.md)
