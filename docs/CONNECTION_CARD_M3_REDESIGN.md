# Connection Card Material Design 3 重新设计

## 📋 改进概述

根据 [Google Material Design 3 Cards 规范](https://m3.material.io/components/cards/overview) 对连接卡片进行了全面优化。

## 🎯 主要改进

### 1. **移除无意义的地址显示** ✅
**问题**: 之前显示完整的 WebSocket URL（如 `wss://api.xiaozhi.me/mcp/?`）占用空间且无实际意义。

**解决方案**: 
- 完全移除地址显示区域
- 用更有价值的描述信息替代
- 保留了 Tooltip 以便在需要时查看完整地址（可选）

### 2. **优化视觉层次和配色** 🎨

#### 卡片头部
- **渐变背景**: 使用淡蓝色渐变（`rgba(25, 118, 210, 0.08)` 到 `rgba(25, 118, 210, 0.03)`）
- **状态指示器**: 在图标右下角添加彩色圆点：
  - 🟢 **绿色**: 已连接（`#4CAF50`）
  - 🟠 **橙色**: 断开连接（`#FF9800`）
  - 无点: 未启用
- **标题字体**: 加粗（`font-weight: 600`）提升可读性

#### 描述区域
- **固定高度**: 使用 `min-height: 44px` 保持卡片一致性
- **两行截断**: `-webkit-line-clamp: 2` 避免内容溢出
- **占位文本**: 无描述时显示斜体提示文字

#### 状态标签 (Chips)
- **已连接**: 绿色填充背景（`#E8F5E9`）+ 深绿色文字（`#2E7D32`）
- **已断开**: 橙色填充背景（`#FFF3E0`）+ 深橙色文字（`#E65100`）
- **未启动**: 灰色边框（`#BDBDBD`）+ 灰色文字（`#757575`）
- **绑定数量**: 蓝色边框（`#1976D2`）

### 3. **重新设计操作按钮** 🔘

#### 底部操作栏
- **分割线**: 使用 `<MudDivider />` 分隔内容和操作区
- **淡灰背景**: `rgba(0, 0, 0, 0.02)` 提供视觉区分
- **双按钮布局**:
  - **左侧**: 启用/禁用按钮（文本按钮）
  - **右侧**: 管理按钮（填充按钮，主要操作）

#### 按钮样式符合 M3 规范
```css
- 圆角: 20px (pill shape)
- text-transform: none (保留原始大小写)
- font-weight: 600 (主要操作), 500 (次要操作)
- 阴影: 0 1px 2px rgba(0,0,0,0.1) (填充按钮)
```

#### 按钮功能合理性
| 按钮 | 位置 | 类型 | 图标 | 说明 |
|------|------|------|------|------|
| **启用/禁用** | 左下 | Text | PlayArrow / PowerSettingsNew | 快速切换连接状态 |
| **管理** | 右下 | Filled | ArrowForward | 主要操作：查看和管理绑定 |
| **编辑** | 菜单 | MenuItem | Edit | 修改连接信息 |
| **查看绑定** | 菜单 | MenuItem | Link | 备用入口（与管理相同） |
| **删除** | 菜单 | MenuItem | Delete (红色) | 危险操作放在菜单中 |

### 4. **增强交互效果** ✨

#### 悬停动画
```css
.connection-card:hover {
    box-shadow: 0px 4px 8px rgba(0,0,0,0.12), 0px 2px 4px rgba(0,0,0,0.08);
    transform: translateY(-2px); /* 轻微上升 */
}
```

#### 顶部渐变条
```css
.connection-card::before {
    /* 3px 高度的蓝绿渐变条 */
    background: linear-gradient(90deg, #1976D2 0%, #0097A7 100%);
    /* 悬停时从左到右展开 */
    transform: scaleX(0) → scaleX(1);
}
```

### 5. **符合 M3 设计原则** 📐

#### ✅ M3 Cards 规范要点
- [x] **Elevated Card** - 使用 elevation 2，悬停时增强
- [x] **圆角半径** - 12px (`border-radius`)
- [x] **内容间距** - 16-20px (`padding`)
- [x] **视觉层次** - 清晰的头部、内容、操作分区
- [x] **交互反馈** - 悬停、点击状态明确
- [x] **无障碍性** - 适当的对比度和点击目标大小

#### ✅ M3 颜色系统
- **Primary**: `#1976D2` (蓝色)
- **Success**: `#2E7D32` (绿色)
- **Warning**: `#F57C00` (橙色)
- **Error**: `#D32F2F` (红色)
- **Surface**: `#FFFFFF` (白色卡片背景)

#### ✅ M3 动画
- **Duration**: 300ms (标准)
- **Easing**: `cubic-bezier(0.4, 0, 0.2, 1)` (标准缓动)

## 📊 前后对比

### Before (旧版本)
```
┌─────────────────────────────────┐
│ 📡 Connection Name        ⋮     │
├─────────────────────────────────┤
│ Description...                  │
│                                 │
│ Address:                        │
│ wss://api.xiaozhi.me/mcp/?...  │ ← 无意义信息
│                                 │
│ [Connected] [2 Bindings]        │
│ Created: Oct 15, 2024           │
├─────────────────────────────────┤
│ 🔘 Disable    📋 Manage         │ ← 图标按钮不清晰
└─────────────────────────────────┘
```

### After (新版本)
```
┌─────────────────────────────────┐ ← 悬停时顶部出现渐变条
│ 📡🟢 Connection Name      ⋮     │ ← 状态点指示器
│ (淡蓝渐变背景)                   │
├─────────────────────────────────┤
│ Description text here with two  │ ← 两行截断
│ lines maximum display...        │
│                                 │
│ [✓ Connected] [🔗 2 Bindings]  │ ← 彩色标签
│ 🕐 Oct 15, 2024                │
├─────────────────────────────────┤
│ 禁用            [📋 管理 →]     │ ← 清晰的文字按钮
└─────────────────────────────────┘
```

## 🌐 国际化支持

新增本地化键值：

| 键 | 英文 | 中文 |
|---|------|------|
| `NoDescription` | No description provided | 暂无描述 |
| `Disable` | Disable | 禁用 |
| `Enable` | Enable | 启用 |

## 📁 修改文件

1. **ConnectionCard.razor** - 卡片组件重构
2. **m3-styles.css** - 添加悬停效果和渐变条动画
3. **SharedResources.resx** - 英文本地化
4. **SharedResources.zh-CN.resx** - 中文本地化

## 🎨 设计决策

### 为什么移除地址显示？
1. **用户价值低**: WebSocket URL 对普通用户无意义
2. **占用空间**: 浪费宝贵的卡片垂直空间
3. **视觉噪音**: 长 URL 破坏卡片的视觉平衡
4. **替代方案**: 在编辑页面或详情页面提供完整信息

### 为什么使用填充按钮作为主要操作？
根据 M3 规范：
- **Filled Button**: 用于最重要的操作（管理绑定）
- **Text Button**: 用于次要操作（启用/禁用）
- **Icon Button**: 用于工具栏或简单操作（菜单）

### 为什么使用状态点而不是文字标签？
- **空间效率**: 点指示器占用更少空间
- **视觉清晰**: 绿/橙色点一目了然
- **冗余信息**: 下方已有状态 Chip，头部只需简单提示

## 🚀 性能影响

- **CSS 动画**: 使用 `transform` 和 `opacity`，GPU 加速
- **无额外请求**: 所有样式内联或在现有 CSS 中
- **轻量级组件**: 没有增加组件复杂度

## ✅ 测试清单

- [ ] 卡片在不同连接状态下显示正确（已连接/已断开/未启用）
- [ ] 悬停效果流畅（阴影、上升、渐变条）
- [ ] 按钮功能正常（启用、禁用、管理、编辑、删除）
- [ ] 国际化切换正常（中英文）
- [ ] 响应式布局（xs/sm/md/lg 屏幕）
- [ ] 无障碍性（键盘导航、屏幕阅读器）

## 📚 参考资料

- [Material Design 3 - Cards](https://m3.material.io/components/cards/overview)
- [Material Design 3 - Cards Specs](https://m3.material.io/components/cards/specs)
- [Material Design 3 - Color System](https://m3.material.io/styles/color/overview)
- [Material Design 3 - Elevation](https://m3.material.io/styles/elevation/overview)

---

**更新日期**: 2025-01-06  
**设计师**: Material Design 3 规范  
**开发**: GitHub Copilot
