# Change: add-view-level-menus（引入 View 级菜单声明 + 分层导航 + ViewModel 导航拦截）

## Why
当前导航/菜单体系存在三个核心问题：
- 菜单缺少“模块 → 视图”层级结构，无法在主导航中以模块为一级、模块内页面为二级进行呈现。
- Avalonia 导航依赖运行时 Type 全域扫描（`Type.GetType` + 遍历 assemblies），性能不可控，且把“导航 key”与类型名强绑定，重构易破坏。
- ViewModel 无法以统一、可覆写的方式参与 `NavigateFrom/NavigateTo` 拦截与生命周期回调，导致拦截逻辑分散在 Guard/Service/UI 里。

本变更提供一套默认基础设施：模块开发者只需要在每个 View（或其对应 ViewModel）上声明 View 级菜单信息，框架即可在安装期进行 metadata-only 投影，运行时按 Key 解析目标并完成 View/ViewModel 绑定，无需额外注册代码。

## What Changes
- **新增** View 级菜单声明机制（metadata-only），并将 DB `Menus` 投影从扁平结构升级为“模块(父) → View(子)”层级结构（使用 `ParentId`）。
- **修改** 导航解析：导航服务以稳定 `NavigationKey` 为入口，通过注册表/索引解析目标，避免 Avalonia 全域类型扫描。
- **新增** ViewModel 导航生命周期与拦截点（可覆写 `CanNavigateFrom/CanNavigateTo/OnNavigatedFrom/OnNavigatedTo`），并与现有 `INavigationGuard` 协同工作。
- **修改** 模块模板（VS/CLI）：生成的示例 View/VM 默认包含 View 级菜单声明与导航生命周期覆写示例。

## Impact
- Affected specs:
  - `openspec/specs/navigation/spec.md`
  - `openspec/specs/runtime/spec.md`
  - `openspec/specs/module-template/spec.md`
- Affected code (implementation phase):
  - 安装期菜单投影：`src/Modulus.Core/Installation/ModuleInstallerService.cs`、`src/Modulus.Core/Installation/ModuleMenuAttributeReader.cs`
  - 运行时菜单组装：`src/Modulus.Core/Runtime/ModulusApplication.cs`
  - 导航：`src/Hosts/Modulus.Host.Avalonia/Services/AvaloniaNavigationService.cs`、`src/Hosts/Modulus.Host.Blazor/Services/BlazorNavigationService.cs`
  - 模板：`templates/**`、`src/Modulus.Cli/Templates/**`


