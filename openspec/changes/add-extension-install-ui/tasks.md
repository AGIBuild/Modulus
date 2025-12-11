## 1. Core 安装服务扩展

- [x] 1.1 新增 `ModuleInstallResult` 返回类型（Success, ModuleId, Error, RequiresConfirmation）
- [x] 1.2 在 `IModuleInstallerService` 添加 `InstallFromPackageAsync` 方法签名
- [x] 1.3 实现 `ModuleInstallerService.InstallFromPackageAsync`（解压 → 验证 → 复制 → 注册）
- [x] 1.4 处理已存在模块的覆盖逻辑（先卸载运行中模块）

## 2. Avalonia Host UI 实现

- [x] 2.1 在 `ModuleListViewModel` 添加 `InstallPackageAsync` 方法
- [x] 2.2 实现文件选择对话框调用（`StorageProvider.OpenFilePickerAsync`）
- [x] 2.3 添加确认覆盖对话框（`INotificationService.ConfirmAsync`）
- [x] 2.4 安装后自动调用 `IModuleLoader.LoadAsync` 加载模块
- [x] 2.5 发送 `MenuItemsAddedMessage` 更新导航
- [x] 2.6 在 `ModuleListView.axaml` 添加"Install from Package..."按钮

## 3. Blazor Host UI 实现

- [x] 3.1 在 `ModuleListViewModel` 添加 `InstallFromStreamAsync` 方法
- [x] 3.2 在 `Modules.razor` 添加 `MudFileUpload` 组件
- [x] 3.3 实现文件上传到临时目录逻辑
- [x] 3.4 安装后自动加载模块并刷新列表
- [x] 3.5 添加覆盖确认对话框（`MudDialog`）

## 4. 验证

- [ ] 4.1 手动测试：Avalonia 安装 modpkg → 验证加载/卸载/重新加载正常
- [ ] 4.2 手动测试：Blazor 上传安装 modpkg → 验证同上
- [ ] 4.3 测试覆盖安装流程
