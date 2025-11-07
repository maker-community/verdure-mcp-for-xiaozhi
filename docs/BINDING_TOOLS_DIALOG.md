# 绑定工具对话框优化

## 📋 概述

优化了服务绑定页面的工具显示方式，将卡片上的工具列表移除，改为点击工具数量标签时弹出对话框展示完整列表。

## ✨ 主要改进

### 1. 移除卡片上的工具列表
- **之前**: 卡片显示前3个工具 + "更多" 标签
- **现在**: 仅显示工具数量标签（可点击）
- **优势**: 
  - 解决了工具数量多时卡片内容过长的问题
  - 卡片更简洁、紧凑
  - 提升了页面的整体视觉效果

### 2. 新增工具列表对话框
- **组件**: `BindingToolsDialog.razor`
- **触发**: 点击工具数量标签
- **功能**:
  - 显示服务名称和图标
  - 显示工具总数
  - 列出所有绑定的工具（按字母顺序排序）
  - 每个工具带有工具图标
  - 悬停效果和动画

### 3. 交互体验优化
- 工具数量标签现在可点击，带有悬停效果
- 对话框采用 Material Design 3 风格
- 工具列表项带有悬停动画
- 响应式设计，适配不同屏幕尺寸

## 📁 文件修改

### 1. McpServiceBindingCard.razor
**位置**: `src/Verdure.McpPlatform.Web/Components/McpServiceBindingCard.razor`

**修改内容**:
- 移除了显示前3个工具的代码块
- 为工具数量 Chip 添加了 `OnClick` 事件
- 添加了 `ShowToolsDialog()` 方法
- 注入了 `IDialogService`
- 新增 CSS 样式：`.tools-chip:hover` 悬停效果

### 2. BindingToolsDialog.razor (新建)
**位置**: `src/Verdure.McpPlatform.Web/Components/BindingToolsDialog.razor`

**组件结构**:
```razor
<MudDialog>
    <DialogContent>
        <!-- 服务信息头部 -->
        <!-- 工具列表 -->
        <!-- 空状态提示 -->
    </DialogContent>
    <DialogActions>
        <!-- 关闭按钮 -->
    </DialogActions>
</MudDialog>
```

**参数**:
- `ServiceName`: 服务名称
- `ToolNames`: 工具名称列表

### 3. 本地化资源
**文件**: 
- `SharedResources.resx`
- `SharedResources.zh-CN.resx`

**新增键值**:
| 键 | 英文 | 中文 |
|---|------|------|
| `BindingTools` | Binding Tools | 绑定工具 |
| `NoToolsSelected` | No tools selected | 未选择工具 |

## 🎨 设计特点

### 对话框样式
- **宽度**: MaxWidth.Small
- **布局**: 全宽 (FullWidth)
- **头部**: 服务图标 + 名称 + 工具数量
- **工具列表**: 
  - 左侧蓝色边框
  - 工具图标
  - 按字母顺序排序
  - 悬停时向右平移效果

### 视觉反馈
- 工具数量标签悬停时放大 (scale 1.05)
- 工具数量标签悬停时背景色变化
- 工具列表项悬停时向右平移 4px
- 工具列表项悬停时背景色加深

## 🔧 技术实现

### 对话框调用
```csharp
private async Task ShowToolsDialog()
{
    var parameters = new DialogParameters
    {
        { nameof(BindingToolsDialog.ServiceName), Binding.ServiceName },
        { nameof(BindingToolsDialog.ToolNames), Binding.SelectedToolNames }
    };

    var options = new DialogOptions 
    { 
        CloseButton = true, 
        MaxWidth = MaxWidth.Small,
        FullWidth = true
    };
    
    await DialogService.ShowAsync<BindingToolsDialog>(
        Loc["BindingTools"], 
        parameters, 
        options);
}
```

### CSS 动画
```css
.tools-chip:hover {
    background: rgba(25, 118, 210, 0.08) !important;
    transform: scale(1.05);
}

.tool-item:hover {
    background: rgba(25, 118, 210, 0.08) !important;
    transform: translateX(4px);
    border-left-color: #1565C0 !important;
}
```

## ✅ 测试检查项

- [x] 编译成功，无错误
- [ ] 点击工具数量标签能正常打开对话框
- [ ] 对话框显示正确的服务名称
- [ ] 对话框显示正确的工具数量
- [ ] 所有工具按字母顺序排列
- [ ] 悬停效果正常工作
- [ ] 关闭按钮正常工作
- [ ] 中英文本地化正确显示
- [ ] 响应式布局在不同屏幕尺寸下正常

## 📸 预期效果

### 卡片视图
```
┌────────────────────────────────┐
│ 🧩 Connection → Service Name   │
│                                 │
│ Description text here...        │
│                                 │
│ [Active] [🔧 5 tools] ← 可点击  │
│                                 │
│ 📅 Nov 07, 2025                 │
└────────────────────────────────┘
```

### 对话框视图
```
┌─────────────────────────────────┐
│   绑定工具              [×]      │
├─────────────────────────────────┤
│ 🧩 Service Name                 │
│    5 个工具                      │
│ ─────────────────────            │
│                                  │
│ ▌🔧 tool-name-1                 │
│ ▌🔧 tool-name-2                 │
│ ▌🔧 tool-name-3                 │
│ ▌🔧 tool-name-4                 │
│ ▌🔧 tool-name-5                 │
│                                  │
│              [关闭]             │
└─────────────────────────────────┘
```

## 🎯 用户价值

1. **更简洁的界面**: 卡片不再因为工具列表过长而显得拥挤
2. **更好的浏览体验**: 用户可以快速浏览所有绑定，无需滚动过长的卡片
3. **按需查看**: 只有在需要时才展开查看完整工具列表
4. **更好的性能**: 减少了初始渲染的元素数量

## 📝 后续可能的优化

- [ ] 在对话框中添加工具搜索功能
- [ ] 在对话框中显示每个工具的描述
- [ ] 支持从对话框中直接取消选择某些工具
- [ ] 添加工具分类或分组显示
- [ ] 添加工具使用统计信息

---

**修改日期**: 2025-11-07  
**修改人**: AI Assistant  
**审核状态**: 待测试
