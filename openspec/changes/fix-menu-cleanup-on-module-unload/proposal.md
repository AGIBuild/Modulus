# Change: 修复卸载模块后菜单未移除的问题

## Why

当模块被禁用或卸载时，`ModuleLoader.UnloadAsync` 正确清理了 `IMenuRegistry`，但 Blazor Host 的 `ModuleListViewModel` 没有发送 `MenuItemsRemovedMessage`，导致 `ShellViewModel` 的 `ObservableCollection<MenuItem>` 没有更新，菜单项仍然显示在导航栏中。

## What Changes

- Blazor Host: `ModuleListViewModel.ToggleModuleAsync` 在禁用模块后发送 `MenuItemsRemovedMessage`
- 与 Avalonia Host 行为对齐（Avalonia 已正确实现）

## Impact

- Affected specs: `shell-layout`
- Affected code:
  - `src/Hosts/Modulus.Host.Blazor/Shell/ViewModels/ModuleListViewModel.cs:157-161`
