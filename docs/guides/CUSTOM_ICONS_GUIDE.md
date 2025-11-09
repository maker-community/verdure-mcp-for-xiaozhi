# 自定义图标使用指南

## 📚 概述

基于 MudBlazor 图标系统的分析，本项目实现了一套自定义图标解决方案，支持：
- ✅ 与 MudBlazor 图标系统完全兼容
- ✅ 支持颜色自定义（通过 `Color` 参数）
- ✅ 支持尺寸控制（通过 `Size` 参数）
- ✅ 易于扩展和维护
- ✅ 类型安全（编译时检查）

## 🎨 实现原理

### MudBlazor 图标的本质

MudBlazor 的图标实际上就是 **SVG Path 字符串常量**：

```csharp
// MudBlazor 官方实现
public class Icons
{
    public partial class Custom
    {
        public class Brands
        {
            public const string GitHub = "<path d=\"M12 .3a12 12 0 0 0-3.8 23.4c.6.1.8-.3.8-.6v-2c-3.3.7-4-1.6-4-1.6-.6-1.4-1.4-1.8-1.4-1.8-1-.7.1-.7.1-.7 1.2 0 1.9 1.2 1.9 1.2 1 1.8 2.8 1.3 3.5 1 0-.8.4-1.3.7-1.6-2.7-.3-5.5-1.3-5.5-6\"/>";
        }
    }
}
```

### MudIcon 组件渲染流程

```razor
@* MudIcon 的实现 *@
<svg class="@Classname" style="@Style" viewBox="@ViewBox">
    @((MarkupString)Icon)  @* 直接渲染 SVG Path 字符串 *@
</svg>
```

**关键特性**:
- `Icon` 参数接受 SVG Path 字符串
- 支持通过 `Color` 参数控制填充色（`fill="currentColor"`）
- 支持通过 `Size` 参数控制尺寸
- ViewBox 默认为 `"0 0 24 24"`

## 🛠️ 使用方式

### 1. 在 Razor 组件中使用

```razor
@using Verdure.McpPlatform.Web.Icons

@* 使用自定义图标 *@
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Primary" Size="Size.Large" />
<MudIcon Icon="@CustomIcons.SocialMedia.Twitter" Color="Color.Info" />
<MudIcon Icon="@CustomIcons.Languages.Python" Color="Color.Success" Size="Size.Medium" />

@* 在按钮中使用 *@
<MudIconButton Icon="@CustomIcons.SocialMedia.Discord" 
               Color="Color.Secondary" 
               Href="https://discord.gg/your-server" 
               Target="_blank" />

@* 在菜单中使用 *@
<MudMenu Icon="@CustomIcons.Brands.Docker">
    <MudMenuItem Label="容器管理" />
    <MudMenuItem Label="镜像管理" />
</MudMenu>

@* 在卡片中使用 *@
<MudCard>
    <MudCardHeader>
        <CardHeaderContent>
            <MudIcon Icon="@CustomIcons.Languages.CSharp" Color="Color.Primary" Class="mr-2" />
            <MudText Typo="Typo.h6">C# 项目</MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent>
        项目内容...
    </MudCardContent>
</MudCard>
```

### 2. 在 MainLayout 中使用（替换当前的 Bilibili 按钮）

```razor
@* MainLayout.razor *@
<MudTooltip Text="@Loc["TooltipBilibiliChannel"]">
    <MudIconButton Icon="@CustomIcons.SocialMedia.Bilibili" 
                   Color="Color.Inherit" 
                   Href="https://space.bilibili.com/25228512" 
                   Target="_blank" />
</MudTooltip>

@* 添加更多社交媒体链接 *@
<MudTooltip Text="微信公众号">
    <MudIconButton Icon="@CustomIcons.SocialMedia.WeChat" 
                   Color="Color.Inherit" />
</MudTooltip>

<MudTooltip Text="Discord 社区">
    <MudIconButton Icon="@CustomIcons.SocialMedia.Discord" 
                   Color="Color.Inherit" 
                   Href="https://discord.gg/your-server" 
                   Target="_blank" />
</MudTooltip>
```

### 3. 支持的所有颜色

```razor
@* MudBlazor 支持的所有颜色 *@
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Default" />
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Primary" />
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Secondary" />
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Tertiary" />
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Info" />
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Success" />
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Warning" />
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Error" />
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Dark" />
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="Color.Inherit" />
```

### 4. 支持的所有尺寸

```razor
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Size="Size.Small" />   @* 1.25rem *@
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Size="Size.Medium" />  @* 1.5rem (默认) *@
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Size="Size.Large" />   @* 2.25rem *@

@* 自定义尺寸 *@
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Style="font-size: 3rem;" />
```

## ➕ 如何添加新图标

### 步骤 1: 获取 SVG Path

从以下途径获取 SVG 图标：
- **Simple Icons**: https://simpleicons.org/ （推荐，包含大量品牌 Logo）
- **SVG Repo**: https://www.svgrepo.com/
- **Iconify**: https://icon-sets.iconify.design/
- **Font Awesome**: https://fontawesome.com/icons

### 步骤 2: 提取 Path 数据

从 SVG 文件中提取 `<path>` 标签的 `d` 属性：

```xml
<!-- 原始 SVG -->
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z" fill="currentColor"/>
</svg>

<!-- 提取的 Path -->
<path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z" fill="currentColor"/>
```

**重要提示**:
- 保留 `fill="currentColor"` 以支持颜色自定义
- 如果有多个 `<path>` 标签，保留所有
- ViewBox 通常是 `"0 0 24 24"`，如果不同需要特别注意

### 步骤 3: 添加到 CustomIcons.cs

```csharp
namespace Verdure.McpPlatform.Web.Icons;

public static class CustomIcons
{
    public static class YourCategory
    {
        /// <summary>
        /// 图标名称 - 描述
        /// </summary>
        public const string YourIconName = "<path d=\"...\" fill=\"currentColor\"/>";
    }
}
```

### 步骤 4: 使用新图标

```razor
<MudIcon Icon="@CustomIcons.YourCategory.YourIconName" Color="Color.Primary" />
```

## 🎯 实际示例：添加 Bilibili Logo

### 1. 从 Simple Icons 获取 SVG

访问 https://simpleicons.org/?q=bilibili

```xml
<svg role="img" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
    <path d="M17.813 4.653h.854c1.51.054 2.769.578 3.773 1.574 1.004.995 1.524 2.249 1.56 3.76v7.36c-.036 1.51-.556 2.769-1.56 3.773s-2.262 1.524-3.773 1.56H5.333c-1.51-.036-2.769-.556-3.773-1.56S.036 18.858 0 17.347v-7.36c.036-1.511.556-2.765 1.56-3.76 1.004-.996 2.262-1.52 3.773-1.574h.774l-1.174-1.12a1.234 1.234 0 0 1-.373-.906c0-.356.124-.658.373-.907l.027-.027c.267-.249.573-.373.92-.373.347 0 .653.124.92.373L9.653 4.44c.071.071.134.142.187.213h4.267a.836.836 0 0 1 .16-.213l2.853-2.747c.267-.249.573-.373.92-.373.347 0 .662.151.929.4.267.249.391.551.391.907 0 .355-.124.657-.373.906zM5.333 7.24c-.746.018-1.373.276-1.88.773-.506.498-.769 1.13-.786 1.894v7.52c.017.764.28 1.395.786 1.893.507.498 1.134.756 1.88.773h13.334c.746-.017 1.373-.275 1.88-.773.506-.498.769-1.129.786-1.893v-7.52c-.017-.765-.28-1.396-.786-1.894-.507-.497-1.134-.755-1.88-.773zM8 11.107c.373 0 .684.124.933.373.25.249.383.569.4.96v1.173c-.017.391-.15.711-.4.96-.249.25-.56.374-.933.374s-.684-.125-.933-.374c-.25-.249-.383-.569-.4-.96V12.44c0-.373.129-.689.386-.947.258-.257.574-.386.947-.386zm8 0c.373 0 .684.124.933.373.25.249.383.569.4.96v1.173c-.017.391-.15.711-.4.96-.249.25-.56.374-.933.374s-.684-.125-.933-.374c-.25-.249-.383-.569-.4-.96V12.44c.017-.391.15-.711.4-.96.249-.249.56-.373.933-.373Z"/>
</svg>
```

### 2. 添加到 CustomIcons.cs

```csharp
public static class SocialMedia
{
    /// <summary>
    /// 哔哩哔哩 (Bilibili) Logo
    /// </summary>
    public const string Bilibili = "<path d=\"M17.813 4.653h.854c1.51.054 2.769.578 3.773 1.574 1.004.995 1.524 2.249 1.56 3.76v7.36c-.036 1.51-.556 2.769-1.56 3.773s-2.262 1.524-3.773 1.56H5.333c-1.51-.036-2.769-.556-3.773-1.56S.036 18.858 0 17.347v-7.36c.036-1.511.556-2.765 1.56-3.76 1.004-.996 2.262-1.52 3.773-1.574h.774l-1.174-1.12a1.234 1.234 0 0 1-.373-.906c0-.356.124-.658.373-.907l.027-.027c.267-.249.573-.373.92-.373.347 0 .653.124.92.373L9.653 4.44c.071.071.134.142.187.213h4.267a.836.836 0 0 1 .16-.213l2.853-2.747c.267-.249.573-.373.92-.373.347 0 .662.151.929.4.267.249.391.551.391.907 0 .355-.124.657-.373.906zM5.333 7.24c-.746.018-1.373.276-1.88.773-.506.498-.769 1.13-.786 1.894v7.52c.017.764.28 1.395.786 1.893.507.498 1.134.756 1.88.773h13.334c.746-.017 1.373-.275 1.88-.773.506-.498.769-1.129.786-1.893v-7.52c-.017-.765-.28-1.396-.786-1.894-.507-.497-1.134-.755-1.88-.773zM8 11.107c.373 0 .684.124.933.373.25.249.383.569.4.96v1.173c-.017.391-.15.711-.4.96-.249.25-.56.374-.933.374s-.684-.125-.933-.374c-.25-.249-.383-.569-.4-.96V12.44c0-.373.129-.689.386-.947.258-.257.574-.386.947-.386zm8 0c.373 0 .684.124.933.373.25.249.383.569.4.96v1.173c-.017.391-.15.711-.4.96-.249.25-.56.374-.933.374s-.684-.125-.933-.374c-.25-.249-.383-.569-.4-.96V12.44c.017-.391.15-.711.4-.96.249-.249.56-.373.933-.373Z\" fill=\"currentColor\"/>";
}
```

### 3. 在 MainLayout.razor 中使用

```razor
@using Verdure.McpPlatform.Web.Icons

<MudTooltip Text="@Loc["TooltipBilibiliChannel"]">
    <MudIconButton Icon="@CustomIcons.SocialMedia.Bilibili" 
                   Color="Color.Inherit" 
                   Href="https://space.bilibili.com/25228512" 
                   Target="_blank" />
</MudTooltip>
```

## 🎨 高级用法

### 1. 动态颜色

```razor
@code {
    private Color iconColor = Color.Primary;
    
    void ChangeColor()
    {
        iconColor = iconColor == Color.Primary ? Color.Secondary : Color.Primary;
    }
}

<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" Color="@iconColor" />
<MudButton OnClick="ChangeColor">切换颜色</MudButton>
```

### 2. 响应式尺寸

```razor
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" 
         Size="Size.Large" 
         Class="d-none d-sm-inline-flex" />  @* 窄屏隐藏 *@

<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" 
         Size="Size.Small" 
         Class="d-inline-flex d-sm-none" />  @* 窄屏显示小图标 *@
```

### 3. 自定义样式

```razor
<MudIcon Icon="@CustomIcons.SocialMedia.Bilibili" 
         Style="font-size: 3rem; color: #00A1D6; transform: rotate(15deg);" />
```

### 4. 动画效果

```razor
<style>
    .icon-spin {
        animation: spin 2s linear infinite;
    }
    
    @@keyframes spin {
        100% { transform: rotate(360deg); }
    }
</style>

<MudIcon Icon="@CustomIcons.Brands.Docker" 
         Class="icon-spin" 
         Color="Color.Info" />
```

## ✅ 优势总结

### 对比 Font Icon
| 特性 | CustomIcons (推荐) | Font Awesome 等字体图标 |
|------|-------------------|----------------------|
| 颜色控制 | ✅ 完美支持 | ⚠️ 有限支持 |
| 加载性能 | ✅ 按需加载 | ❌ 需要加载整个字体文件 |
| 类型安全 | ✅ 编译时检查 | ❌ 字符串容易出错 |
| 自定义扩展 | ✅ 简单 | ❌ 需要生成字体文件 |
| 包大小 | ✅ 仅包含使用的 | ❌ 包含全部图标 |

### 对比内联 SVG
| 特性 | CustomIcons (推荐) | 直接写 SVG |
|------|-------------------|-----------|
| 代码可维护性 | ✅ 统一管理 | ❌ 分散在各处 |
| 复用性 | ✅ 高 | ❌ 低 |
| IntelliSense | ✅ 有 | ❌ 无 |
| 重构支持 | ✅ 有 | ❌ 无 |

## 📖 参考资源

- **MudBlazor Icons 源码**: https://github.com/MudBlazor/MudBlazor/tree/main/src/MudBlazor/Icons
- **Simple Icons**: https://simpleicons.org/
- **SVG 参考**: https://developer.mozilla.org/en-US/docs/Web/SVG/Element/path
- **MudBlazor 文档**: https://mudblazor.com/features/icons

## ❓ 常见问题

### Q: 为什么我的图标颜色不变？
A: 确保 SVG Path 中使用 `fill="currentColor"` 而不是硬编码的颜色值。

### Q: ViewBox 不是 24x24 怎么办？
A: 可以通过 `MudIcon` 的 `ViewBox` 参数指定：
```razor
<MudIcon Icon="@YourIcon" ViewBox="0 0 48 48" />
```

### Q: 图标显示不完整？
A: 检查 SVG Path 是否完整复制，特别注意引号和特殊字符。

### Q: 可以使用多个 Path 吗？
A: 可以，直接拼接多个 `<path>` 标签：
```csharp
public const string ComplexIcon = "<path d=\"...\"/><path d=\"...\"/>";
```
