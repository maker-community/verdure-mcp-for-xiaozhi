# UI 卡片重构 - 快速参考

## 🚀 快速开始

```powershell
# 1. 验证实现
.\scripts\test-ui-refactoring.ps1

# 2. 启动应用
dotnet run --project src\Verdure.McpPlatform.AppHost

# 3. 访问演示页面
# https://localhost:5001/connections-new
```

---

## 📋 API 端点

### 小智连接分页
```
GET /api/xiaozhi-mcp-endpoints/paged?Page=1&PageSize=12&SearchTerm=test&SortBy=Name&SortOrder=asc
```

**响应**:
```json
{
  "items": [...],
  "totalCount": 45,
  "page": 1,
  "pageSize": 12,
  "totalPages": 4,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### MCP 服务分页
```
GET /api/mcp-services/paged?Page=1&PageSize=12&SearchTerm=search&SortBy=Name&SortOrder=desc
```

---

## 🎴 卡片组件使用

### ConnectionCard

```razor
<ConnectionCard 
    ServerData="@server"
    OnEdit="@HandleEdit"
    OnDelete="@HandleDelete"
    OnEnable="@HandleEnable"
    OnDisable="@HandleDisable"
    OnViewBindings="@HandleViewBindings" />

@code {
    private async Task HandleEdit(XiaozhiMcpEndpointDto server)
    {
        Navigation.NavigateTo($"/connections/edit/{server.Id}");
    }

    private async Task HandleDelete(XiaozhiMcpEndpointDto server)
    {
        bool? confirm = await DialogService.ShowMessageBox(
            "Delete Connection",
            $"Are you sure you want to delete '{server.Name}'?",
            yesText: "Delete", cancelText: "Cancel");
            
        if (confirm == true)
        {
            await ServerService.DeleteServerAsync(server.Id);
            Snackbar.Add("Connection deleted", Severity.Success);
            await LoadData();
        }
    }
}
```

### ServiceConfigCard

```razor
<ServiceConfigCard 
    ServiceData="@service"
    OnEdit="@HandleEdit"
    OnDelete="@HandleDelete"
    OnSyncTools="@HandleSyncTools"
    OnViewDetails="@HandleViewDetails" />
```

---

## 📱 响应式布局

```razor
<MudGrid>
    @foreach (var item in _items)
    {
        <MudItem xs="12" sm="6" md="4" lg="3">
            <!-- xs: 手机 (1列) -->
            <!-- sm: 平板 (2列) -->
            <!-- md: 桌面 (3列) -->
            <!-- lg: 大屏 (4列) -->
            <ConnectionCard ServerData="@item" />
        </MudItem>
    }
</MudGrid>
```

---

## 🔍 搜索实现

```razor
<MudTextField 
    @bind-Value="_searchTerm"
    Label="Search"
    Adornment="Adornment.Start"
    AdornmentIcon="@Icons.Material.Filled.Search"
    Immediate="true"
    DebounceInterval="500"
    OnDebounceIntervalElapsed="OnSearchChanged" />

@code {
    private string _searchTerm = "";
    
    private async Task OnSearchChanged()
    {
        _currentPage = 1;
        await LoadData(reset: true);
    }
}
```

---

## 📄 分页加载

```razor
@if (_loading)
{
    <!-- 骨架加载状态 -->
    @for (int i = 0; i < 8; i++)
    {
        <MudItem xs="12" sm="6" md="4" lg="3">
            <MudCard Class="skeleton-card">
                <MudCardContent>
                    <MudSkeleton SkeletonType="SkeletonType.Text" Height="40px" />
                    <MudSkeleton SkeletonType="SkeletonType.Text" />
                    <MudSkeleton SkeletonType="SkeletonType.Text" />
                </MudCardContent>
            </MudCard>
        </MudItem>
    }
}
else if (_items.Count == 0)
{
    <!-- 空状态 -->
    <MudText Typo="Typo.h6" Align="Align.Center">
        No items found
    </MudText>
}
else
{
    <!-- 卡片列表 -->
    @foreach (var item in _items)
    {
        <MudItem xs="12" sm="6" md="4" lg="3">
            <ConnectionCard ServerData="@item" />
        </MudItem>
    }
}

<!-- 加载更多按钮 -->
@if (_hasMoreData && !_loading)
{
    <MudItem xs="12" Class="d-flex justify-center mt-4">
        <MudButton 
            Variant="Variant.Outlined"
            Color="Color.Primary"
            OnClick="LoadMoreAsync"
            Disabled="@_loadingMore">
            @if (_loadingMore)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" />
                <span class="ml-2">Loading...</span>
            }
            else
            {
                <span>Load More</span>
            }
        </MudButton>
    </MudItem>
}

@code {
    private List<XiaozhiMcpEndpointDto> _items = new();
    private int _currentPage = 1;
    private int _pageSize = 12;
    private bool _loading = false;
    private bool _loadingMore = false;
    private bool _hasMoreData = true;
    private int _totalCount = 0;

    private async Task LoadData(bool reset = false)
    {
        _loading = true;
        try
        {
            var request = new PagedRequest
            {
                Page = _currentPage,
                PageSize = _pageSize,
                SearchTerm = _searchTerm,
                SortBy = "CreatedAt",
                SortOrder = "desc"
            };

            var result = await Service.GetPagedAsync(request);
            
            if (reset)
                _items = result.Items.ToList();
            else
                _items.AddRange(result.Items);
                
            _totalCount = result.TotalCount;
            _hasMoreData = result.HasNextPage;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadMoreAsync()
    {
        if (!_hasMoreData) return;
        
        _loadingMore = true;
        _currentPage++;
        await LoadData(reset: false);
        _loadingMore = false;
    }
}
```

---

## 🎨 CSS 样式

### 卡片样式

```css
/* 基础卡片 */
.connection-card {
    height: 100%;
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

/* 悬停效果（GPU加速） */
.connection-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
}

/* 骨架加载 */
.skeleton-card {
    background: linear-gradient(
        90deg,
        var(--mud-palette-surface) 0%,
        var(--mud-palette-surface-lighten) 50%,
        var(--mud-palette-surface) 100%
    );
    background-size: 200% 100%;
    animation: loading 1.5s ease-in-out infinite;
}

@keyframes loading {
    0% { background-position: -200% 0; }
    100% { background-position: 200% 0; }
}
```

---

## 📊 性能优化

### 数据库查询

```csharp
public async Task<PagedResult<XiaozhiMcpEndpointDto>> GetByUserIdPagedAsync(
    string userId,
    PagedRequest request)
{
    var query = _context.XiaozhiMcpEndpoints
        .Where(x => x.UserId == userId);

    // 搜索
    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
    {
        var search = request.SearchTerm.ToLower();
        query = query.Where(x =>
            x.Name.ToLower().Contains(search) ||
            x.Address.ToLower().Contains(search));
    }

    // 排序
    query = request.SortBy?.ToLower() switch
    {
        "name" => request.SortOrder == "desc" 
            ? query.OrderByDescending(x => x.Name) 
            : query.OrderBy(x => x.Name),
        _ => query.OrderByDescending(x => x.CreatedAt)
    };

    // 获取总数
    var totalCount = await query.CountAsync();

    // 分页（AsNoTracking 优化只读查询）
    var items = await query
        .AsNoTracking()
        .Skip(request.GetSkip())
        .Take(request.GetSafePageSize())
        .ToListAsync();

    return PagedResult<XiaozhiMcpEndpointDto>.Create(
        items.Select(MapToDto),
        totalCount,
        request.Page,
        request.PageSize);
}
```

---

## 🧪 测试命令

```powershell
# 运行自动化测试
.\scripts\test-ui-refactoring.ps1

# 仅测试后端（跳过 Web 构建）
.\scripts\test-ui-refactoring.ps1 -ApiOnly

# 跳过构建（仅验证文件）
.\scripts\test-ui-refactoring.ps1 -SkipBuild

# 生成测试数据
.\scripts\test-ui-refactoring.ps1 -TestData
```

---

## 📚 文档资源

| 文档 | 用途 |
|------|------|
| `docs/UI_REFACTORING_COMPLETE.md` | 完成总结 |
| `docs/guides/UI_CARD_REFACTORING_SUMMARY.md` | 完整实施指南 |
| `docs/guides/UI_TESTING_GUIDE.md` | 详细测试指南 |
| `scripts/test-ui-refactoring.ps1` | 自动化测试脚本 |

---

## 🔧 常见任务

### 替换现有页面

```razor
<!-- 在 Connections.razor 中 -->
@page "/connections"
<!-- 将整个内容替换为 ConnectionsCardView.razor 的实现 -->
```

### 添加新的卡片页面

1. 创建卡片组件（参考 `ConnectionCard.razor`）
2. 创建页面（参考 `ConnectionsCardView.razor`）
3. 实现分页逻辑
4. 添加搜索和排序
5. 更新导航菜单

### 自定义样式

```css
/* 在 m3-styles.css 中添加 */
.my-custom-card {
    /* 自定义样式 */
}
```

---

## ⚡ 快捷键提示

- **F12**: 打开浏览器开发者工具
- **Ctrl+Shift+M**: 切换设备模式（响应式测试）
- **Ctrl+Shift+C**: 检查元素
- **Ctrl+R**: 刷新页面
- **F5**: 硬刷新（清除缓存）

---

**最后更新**: 2024年  
**版本**: Phase 1 & 2 完成
