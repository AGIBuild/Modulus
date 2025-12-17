# Change: 添加 Home 主页模块

## Why

当前应用启动后默认显示的是 Module List（模块管理）页面，无法让用户第一时间理解 Modulus 框架的核心价值和特色。需要一个风格新颖、视觉吸引力强的主页模块，作为用户的"第一印象"页面。

## What Changes

- 新增 `Home` bundled 模块，包含：
  - `Home.Core` - 核心逻辑（ViewModel、统计服务）
  - `Home.UI.Avalonia` - Avalonia 桌面端视图
  - `Home.UI.Blazor` - Blazor 移动端视图
- 采用"轨道控制中心"设计风格：
  - 深空渐变背景 + 星点粒子效果
  - 中央发光 Logo + 脉冲动画
  - 模块卡片围绕核心"漂浮"展示
  - 底部 CLI 快速入门命令展示
- **BREAKING**: 应用启动后默认导航到 Home 模块（而非 ModuleListViewModel）
- 更新 `bundled-modules.json` 包含 Home 模块配置

## Impact

- Affected specs: `navigation`（添加默认导航配置）
- Affected code:
  - `src/Modules/Home/` - 新增模块
  - `src/Hosts/Modulus.Host.Avalonia/App.axaml.cs` - 修改默认导航
  - `src/Hosts/Modulus.Host.Blazor/` - 修改默认路由
  - `src/Hosts/*/Resources/bundled-modules.json` - 添加 Home 模块注册

