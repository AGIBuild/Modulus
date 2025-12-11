# Change: 扩展管理界面安装功能

## Why
当前扩展管理界面仅支持通过手动输入 manifest.json 路径导入开发模块。用户需要从 UI 界面直接选择并安装 `.modpkg` 包文件，安装后立即可用（加载/卸载等功能正常工作），无需重启应用。

## What Changes
- 在 Avalonia Host 扩展管理页面添加"安装包"按钮，打开文件选择对话框
- 在 Blazor Host 扩展管理页面添加文件上传功能
- 新增 `IModuleInstallerService.InstallFromPackageAsync` 方法支持从 `.modpkg` 安装
- 安装流程：解压包 → 复制到用户模块目录 → 注册数据库 → 运行时加载
- 安装后自动刷新模块列表并启用新安装的模块
- 支持覆盖已存在模块（带确认对话框）

## Impact
- Affected specs: `extension-management` (新增)
- Affected code:
  - `src/Modulus.Core/Installation/IModuleInstallerService.cs`
  - `src/Modulus.Core/Installation/ModuleInstallerService.cs`
  - `src/Hosts/Modulus.Host.Avalonia/Shell/ViewModels/ModuleListViewModel.cs`
  - `src/Hosts/Modulus.Host.Avalonia/Shell/Views/ModuleListView.axaml`
  - `src/Hosts/Modulus.Host.Blazor/Shell/ViewModels/ModuleListViewModel.cs`
  - `src/Hosts/Modulus.Host.Blazor/Components/Pages/Modules.razor`

