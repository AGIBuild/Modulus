## 1. 实现

- [x] 1.1 Blazor Host: 在 `ModuleListViewModel.ToggleModuleAsync` 禁用模块后添加 `using CommunityToolkit.Mvvm.Messaging;` 和 `using Modulus.UI.Abstractions.Messages;`，发送 `MenuItemsRemovedMessage`

## 2. 测试

- [x] 2.1 添加 `ShellViewModelTests.cs` 到 `tests/Modulus.Hosts.Tests/`
- [x] 2.2 测试 `Receive_MenuItemsRemovedMessage_RemovesMenuItemsFromMainMenu`
- [x] 2.3 测试 `Receive_MenuItemsRemovedMessage_RemovesMenuItemsFromBottomMenu`
- [x] 2.4 测试 `Receive_MenuItemsRemovedMessage_IgnoresUnrelatedModules`

## 3. 验证

- [x] 3.1 手动测试：禁用模块后菜单项从导航栏移除
- [x] 3.2 手动测试：重新启用模块后菜单项恢复
