# 更新日志 (Changelog)

## [未发布] - 2025-01-XX

### ✨ 新增功能

#### 前端界面改进
- **新增欢迎首页** (`/`)
  - 为未登录用户展示平台介绍和核心特性
  - 显示架构设计和快速开始指南
  - 集成社区资源链接
  - 已登录用户自动跳转到仪表盘

- **独立的仪表盘页面** (`/dashboard`)
  - 从原 Home.razor 迁移功能
  - 需要登录认证才能访问
  - 显示用户的 MCP 服务统计数据
  - 提供快速操作入口

- **全新 Footer 组件**
  - 项目信息和技术栈展示
  - 快速导航链接
  - 社区资源集成（GitHub、B站、QQ群）
  - 响应式设计支持

#### 导航栏增强
- **顶部导航栏改进**
  - 添加社区快捷图标按钮（GitHub、项目源码、B站）
  - 改进的用户菜单（下拉式，显示用户信息）
  - Tooltip 提示优化
  - 分隔线优化视觉层次

- **侧边导航优化**
  - 区分"首页"和"仪表盘"
  - 登录状态自适应显示
  - 更清晰的导航层级

### 🎨 用户体验改进

- **简化登录流程**
  - 精简登录页面，去除冗余信息
  - 更直观的视觉设计
  - 清晰的操作引导

- **简化登出流程**
  - 友好的退出确认界面
  - 自动处理登出逻辑
  - 优雅的加载动画

### 📝 文档更新

- 更新主 README.md，添加社区信息和徽章
- 新增 FRONTEND_IMPROVEMENTS.md 详细说明前端改进
- 新增 TESTING_GUIDE.md 提供测试指南
- 新增 start-dev.ps1 快速启动脚本

### 🔗 社区集成

#### GitHub 创客社区
- 组织: [maker-community](https://github.com/maker-community)
- 仓库: [verdure-mcp-for-xiaozhi](https://github.com/maker-community/verdure-mcp-for-xiaozhi)

#### B站 UP主
- 昵称: 绿荫阿广
- 主页: [space.bilibili.com/25228512](https://space.bilibili.com/25228512)

#### QQ 交流群
- 群名: 绿荫DIY硬件交流群
- 群号: 1023487000

## 文件变更摘要

### 新增文件
- `src/Verdure.McpPlatform.Web/Pages/Index.razor` - 欢迎首页
- `src/Verdure.McpPlatform.Web/Pages/Dashboard.razor` - 仪表盘页面
- `src/Verdure.McpPlatform.Web/Layout/Footer.razor` - 页脚组件
- `FRONTEND_IMPROVEMENTS.md` - 前端改进文档
- `TESTING_GUIDE.md` - 测试指南
- `CHANGELOG.md` - 变更日志（本文件）
- `start-dev.ps1` - 开发启动脚本

### 修改文件
- `src/Verdure.McpPlatform.Web/Layout/MainLayout.razor` - 添加 Footer，改进导航栏
- `src/Verdure.McpPlatform.Web/Layout/NavMenu.razor` - 优化导航菜单
- `src/Verdure.McpPlatform.Web/Pages/Login.razor` - 简化登录页面
- `src/Verdure.McpPlatform.Web/Pages/Logout.razor` - 简化登出页面
- `README.md` - 添加社区信息和项目介绍

### 待处理
- [ ] 删除旧的 `Home.razor`（可选，已被 Dashboard.razor 替代）
- [ ] 为新页面添加完整的多语言资源
- [ ] 移动端响应式优化
- [ ] 添加界面截图到 README

## 技术细节

### 架构保持一致
- 遵循 eShop 的代码风格
- 使用 MudBlazor Material Design 3 组件
- 保持领域驱动设计 (DDD) 原则
- 仓储模式 (Repository Pattern) 不变

### 性能考虑
- Blazor WebAssembly 客户端渲染
- 最小化不必要的 API 调用
- 优化组件加载顺序

### 安全性
- 所有敏感页面保持认证要求
- 使用 `@attribute [Authorize]` 保护路由
- OpenID Connect 认证流程不变

## 升级指南

如果从旧版本升级：

1. **拉取最新代码**
   ```bash
   git pull origin main
   ```

2. **清理编译输出**
   ```powershell
   .\start-dev.ps1 -Mode clean
   ```

3. **恢复依赖**
   ```bash
   dotnet restore
   ```

4. **重新编译**
   ```bash
   dotnet build
   ```

5. **启动应用**
   ```powershell
   .\start-dev.ps1
   ```

## 已知问题

无重大已知问题。

## 下一步计划

- [ ] 添加更多社区互动功能
- [ ] 优化移动端体验
- [ ] 增加暗色主题支持
- [ ] 添加用户引导和帮助文档
- [ ] 性能监控和优化
- [ ] 单元测试和集成测试

---

**更新者**: AI Assistant  
**日期**: 2025-01-XX  
**版本**: v1.1.0-dev
