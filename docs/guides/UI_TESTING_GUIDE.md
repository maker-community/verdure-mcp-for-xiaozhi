# UI 卡片重构 - 测试和验证指南

## 🧪 快速测试步骤

### 1. 启动应用

```powershell
# 使用 Aspire 启动完整应用
dotnet run --project src/Verdure.McpPlatform.AppHost

# 或分别启动（调试时）
# Terminal 1 - API
dotnet run --project src/Verdure.McpPlatform.Api

# Terminal 2 - Web
dotnet run --project src/Verdure.McpPlatform.Web
```

### 2. 测试后端分页 API

#### 使用浏览器测试 (Swagger)
访问: `https://localhost:5000/swagger` (API 端口)

测试端点:
- `GET /api/xiaozhi-mcp-endpoints/paged`
- `GET /api/mcp-services/paged`

#### 使用 PowerShell 测试

```powershell
# 测试小智连接分页
$response = Invoke-RestMethod -Uri "https://localhost:5000/api/xiaozhi-mcp-endpoints/paged?Page=1&PageSize=12" `
    -Headers @{Authorization="Bearer YOUR_TOKEN_HERE"}

# 查看结果
$response | ConvertTo-Json -Depth 3

# 输出示例
# {
#   "items": [...],
#   "totalCount": 25,
#   "page": 1,
#   "pageSize": 12,
#   "totalPages": 3,
#   "hasPreviousPage": false,
#   "hasNextPage": true
# }

# 测试搜索
$response = Invoke-RestMethod -Uri "https://localhost:5000/api/xiaozhi-mcp-endpoints/paged?Page=1&PageSize=12&SearchTerm=test" `
    -Headers @{Authorization="Bearer YOUR_TOKEN_HERE"}

# 测试排序
$response = Invoke-RestMethod -Uri "https://localhost:5000/api/xiaozhi-mcp-endpoints/paged?Page=1&PageSize=12&SortBy=Name&SortOrder=asc" `
    -Headers @{Authorization="Bearer YOUR_TOKEN_HERE"}
```

### 3. 测试前端卡片视图

#### 访问新页面
1. 登录应用
2. 访问 `https://localhost:5001/connections-new` (Web 端口)
3. 应该看到卡片网格布局

#### 测试响应式设计
使用浏览器开发者工具（F12）:

**设备模拟**:
- iPhone SE (375px) - 应显示 1 列
- iPad (768px) - 应显示 2 列
- iPad Pro (1024px) - 应显示 3 列  
- 桌面 (1920px) - 应显示 4 列

**测试步骤**:
```
1. Ctrl+Shift+M (切换设备模式)
2. 选择不同设备
3. 确认卡片布局正确调整
4. 检查卡片悬停效果
```

#### 测试搜索功能
1. 在搜索框输入文本
2. 等待 500ms（防抖）
3. 观察卡片列表更新
4. 清空搜索，列表恢复

#### 测试分页加载
1. 滚动到底部
2. 点击"加载更多"按钮
3. 观察新卡片添加到列表
4. 检查"正在加载"状态显示
5. 当没有更多数据时，显示"所有项目已加载"

### 4. 性能测试

#### 测试大数据集
创建测试数据（使用 SQL 或 API）:

```sql
-- 创建100条测试数据
INSERT INTO xiaozhi_mcp_endpoints (id, name, address, user_id, is_enabled, is_connected, created_at)
SELECT 
    gen_random_uuid()::text,
    'Test Connection ' || generate_series,
    'ws://localhost:' || (5000 + generate_series),
    'test-user-id',
    true,
    random() > 0.5,
    NOW()
FROM generate_series(1, 100);
```

#### 性能检查清单
- [ ] 首屏加载时间 < 1秒
- [ ] 搜索响应时间 < 300ms
- [ ] 滚动流畅（无卡顿）
- [ ] 卡片悬停动画流畅
- [ ] 加载更多按钮响应快速

### 5. 功能测试清单

#### 连接卡片功能
- [ ] 显示连接名称、地址
- [ ] 显示正确的连接状态（已连接/未连接/未启动）
- [ ] 显示绑定数量
- [ ] 启用/禁用按钮工作
- [ ] 编辑按钮导航正确
- [ ] 删除按钮显示确认对话框
- [ ] 管理按钮导航到绑定页面

#### 服务卡片功能（未来）
- [ ] 显示服务名称、端点
- [ ] 显示协议类型
- [ ] 显示可见性（公开/私有）
- [ ] 显示工具数量
- [ ] 同步工具按钮工作
- [ ] 查看详情导航正确

#### 空状态
- [ ] 无数据时显示空状态图标
- [ ] 显示正确的提示文本
- [ ] 搜索无结果时显示不同文案
- [ ] "创建连接"按钮工作

#### 加载状态
- [ ] 初始加载显示骨架屏（8个）
- [ ] 骨架屏有动画效果
- [ ] 加载更多显示进度圆圈
- [ ] 加载完成后显示提示

### 6. 浏览器兼容性测试

测试浏览器:
- [ ] Chrome (最新版)
- [ ] Firefox (最新版)
- [ ] Edge (最新版)
- [ ] Safari (如果使用 macOS)
- [ ] 移动浏览器（Chrome Mobile, Safari Mobile）

### 7. 可访问性测试

使用浏览器开发者工具的 Lighthouse:
```
1. F12 打开开发者工具
2. 选择 "Lighthouse" 标签
3. 勾选 "Accessibility"
4. 点击 "Analyze page load"
5. 目标分数: > 90
```

检查项:
- [ ] 键盘导航（Tab 键）
- [ ] 屏幕阅读器兼容
- [ ] 颜色对比度 (WCAG AA)
- [ ] 可点击区域足够大（44x44px）

---

## 🐛 常见问题排查

### 问题 1: 分页 API 返回 401 Unauthorized

**原因**: 未登录或 Token 过期

**解决**:
```powershell
# 检查认证配置
dotnet user-secrets list --project src/Verdure.McpPlatform.Api

# 确保已登录
# 在浏览器中先访问 /login
```

### 问题 2: 卡片不显示或样式错误

**原因**: CSS 未加载或 MudBlazor 组件未引用

**解决**:
1. 检查 `_Imports.razor` 包含:
```razor
@using MudBlazor
```

2. 检查 `index.html` 包含:
```html
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
<link href="css/m3-styles.css" rel="stylesheet" />
```

### 问题 3: 搜索不工作

**原因**: 防抖未触发或 API 调用失败

**解决**:
1. 检查浏览器控制台（F12）是否有错误
2. 检查网络请求（Network 标签）
3. 确认输入延迟 500ms 后触发

### 问题 4: 加载更多无响应

**原因**: `_hasMoreData` 为 false 或 API 错误

**解决**:
1. 检查 `result.HasNextPage` 是否正确
2. 验证 `_currentPage` 递增
3. 检查后端返回的 `TotalPages`

### 问题 5: 响应式布局不正确

**原因**: MudGrid 配置错误

**解决**:
```razor
<!-- 确保使用正确的断点 -->
<MudItem xs="12" sm="6" md="4" lg="3">
    <!-- xs: 手机(1列) -->
    <!-- sm: 平板(2列) -->
    <!-- md: 桌面(3列) -->
    <!-- lg: 大屏(4列) -->
</MudItem>
```

---

## 📊 性能基准

### 目标指标

| 指标 | 目标值 | 测量方法 |
|------|--------|---------|
| 首屏加载 | < 1s | Network 标签 |
| 搜索响应 | < 300ms | Performance 标签 |
| 滚动 FPS | ≥ 55 | Rendering 标签 |
| API 响应 | < 200ms | Network 标签 |
| LCP | < 2.5s | Lighthouse |
| CLS | < 0.1 | Lighthouse |
| FID | < 100ms | Lighthouse |

### 测量工具

```powershell
# Chrome DevTools Performance Recording
1. F12 → Performance
2. 点击 Record (圆点)
3. 执行操作（搜索、滚动、加载更多）
4. 停止录制
5. 分析 Main 线程活动

# Lighthouse 全面测试
1. F12 → Lighthouse
2. 选择 Desktop 或 Mobile
3. 勾选所有分类
4. Generate report
```

---

## ✅ 发布前检查清单

### 代码质量
- [ ] 无编译警告
- [ ] 无控制台错误
- [ ] 代码已格式化（`dotnet format`）
- [ ] 注释完整清晰

### 功能完整性
- [ ] 所有基本操作工作正常
- [ ] 错误处理到位
- [ ] 用户反馈清晰（Snackbar）
- [ ] 加载状态正确显示

### 性能
- [ ] 大数据集测试通过（100+ 项）
- [ ] 移动端流畅
- [ ] 内存无泄漏（长时间使用）

### 文档
- [ ] README 更新
- [ ] CHANGELOG 记录
- [ ] API 文档更新
- [ ] 用户指南更新

---

## 🎉 成功标准

当以下所有项都通过时，重构成功:

1. ✅ 后端 API 编译无错误
2. ✅ 前端 Web 编译无错误
3. ✅ 分页 API 返回正确数据
4. ✅ 卡片布局响应式正常
5. ✅ 搜索功能工作
6. ✅ 加载更多功能工作
7. ✅ 性能指标达标
8. ✅ 移动端体验良好
9. ✅ 无可访问性重大问题
10. ✅ 文档完整

---

**测试完成后，请将结果反馈给开发团队！**  
**如有问题，请参考 `UI_CARD_REFACTORING_SUMMARY.md` 文档。**
