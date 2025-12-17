## 1. 创建 Home 模块项目结构

- [x] 1.1 创建 `src/Modules/Home/Home.Core/` 项目
- [x] 1.2 创建 `src/Modules/Home/Home.UI.Avalonia/` 项目
- [x] 1.3 创建 `src/Modules/Home/Home.UI.Blazor/` 项目
- [x] 1.4 创建 `src/Modules/Home/extension.vsixmanifest` 清单文件
- [x] 1.5 将项目添加到 `Modulus.sln`

## 2. 实现 Home.Core

- [x] 2.1 创建 `HomeModule.cs` (ModulusPackage 入口)
- [x] 2.2 创建 `HomeViewModel.cs` - 主页 ViewModel
- [x] 2.3 创建 `IHomeStatisticsService.cs` - 统计服务接口
- [x] 2.4 实现 `HomeStatisticsService.cs` - 获取已安装模块数、运行状态等

## 3. 实现 Home.UI.Avalonia（轨道控制中心风格）

- [x] 3.1 创建 `HomeAvaloniaModule.cs` (UI 入口)
- [x] 3.2 创建 `HomeView.axaml` 主视图
- [x] 3.3 实现星空粒子背景 (深空渐变 + CSS 动画)
- [x] 3.4 实现中央 Logo 脉冲动画
- [x] 3.5 实现模块卡片轨道布局组件（含运行状态指示灯）
- [x] 3.6 实现特性卡片网格
- [x] 3.7 实现 CLI 命令展示区域
- [x] 3.8 实现"快速创建模块"按钮及交互（通过 CLI 命令展示）

## 4. 实现 Home.UI.Blazor

- [x] 4.1 创建 `HomeBlazorModule.cs` (UI 入口)
- [x] 4.2 创建 `HomePage.razor` 主页面
- [x] 4.3 实现响应式布局（复用轨道控制中心设计）
- [x] 4.4 实现模块状态指示和"快速创建"按钮

## 5. 集成与配置

- [x] 5.1 更新 `bundled-modules.json` 添加 Home 模块（Order=1，置顶）
- [x] 5.2 修改 Avalonia Host 默认导航到 `HomeViewModel`
- [x] 5.3 修改 Blazor Host 默认路由到 `/home`
- [x] 5.4 构建并测试模块加载
