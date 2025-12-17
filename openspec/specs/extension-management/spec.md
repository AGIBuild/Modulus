# extension-management Specification

## Purpose
TBD - created by archiving change add-extension-install-ui. Update Purpose after archive.
## Requirements
### Requirement: Package Installation Service
扩展安装服务 SHALL 提供从 `.modpkg` 文件安装模块的能力。

#### Scenario: Install from valid modpkg file
- **WHEN** 调用 `InstallFromPackageAsync` 并传入有效的 `.modpkg` 文件路径
- **THEN** 解压包内容到临时目录
- **AND** 验证 `extension.vsixmanifest` 存在且有效
- **AND** 复制文件到用户模块目录 `%APPDATA%/Modulus/Modules/{ModuleId}/`
- **AND** 注册模块到数据库
- **AND** 返回 `Success=true` 及 `ModuleId`

#### Scenario: Package file not found
- **WHEN** 指定的 `.modpkg` 文件不存在
- **THEN** 返回 `Success=false`
- **AND** `Error` 包含文件不存在信息

#### Scenario: Invalid package format
- **WHEN** 包文件不是有效的 ZIP 格式或不包含 `extension.vsixmanifest`
- **THEN** 返回 `Success=false`
- **AND** `Error` 包含格式错误信息

#### Scenario: Module already exists without overwrite
- **WHEN** 目标模块目录已存在
- **AND** `overwrite=false`
- **THEN** 返回 `Success=false`
- **AND** `RequiresConfirmation=true`
- **AND** 不修改现有安装

#### Scenario: Module already exists with overwrite
- **WHEN** 目标模块目录已存在
- **AND** `overwrite=true`
- **THEN** 先卸载正在运行的模块（如果有）
- **AND** 删除现有目录
- **AND** 继续安装流程
- **AND** 返回 `Success=true`

### Requirement: Avalonia Package Installation UI
Avalonia Host 扩展管理界面 SHALL 提供文件选择器安装 `.modpkg` 包。

#### Scenario: Open file picker and select package
- **WHEN** 用户点击"Install Package..."按钮
- **THEN** 打开系统文件选择对话框
- **AND** 过滤器仅显示 `.modpkg` 文件
- **AND** 用户选择文件后开始安装流程

#### Scenario: Installation success with auto-load
- **WHEN** `.modpkg` 安装成功
- **THEN** 自动运行时加载新安装的模块
- **AND** 注册模块菜单到导航
- **AND** 刷新模块列表显示新模块
- **AND** 显示成功通知

#### Scenario: Installation requires overwrite confirmation
- **WHEN** 安装返回 `RequiresConfirmation=true`
- **THEN** 显示确认对话框询问是否覆盖
- **AND** 用户确认后以 `overwrite=true` 重新安装
- **AND** 用户取消则中止安装

#### Scenario: Installation failure notification
- **WHEN** 安装过程出错
- **THEN** 显示错误通知包含失败原因

### Requirement: Blazor Package Installation UI
Blazor Host 扩展管理界面 SHALL 提供文件上传安装 `.modpkg` 包。

#### Scenario: Upload and install package
- **WHEN** 用户通过文件上传组件选择 `.modpkg` 文件
- **THEN** 上传文件到临时目录
- **AND** 调用安装服务安装模块
- **AND** 安装成功后自动加载模块

#### Scenario: Upload success with auto-load
- **WHEN** `.modpkg` 上传并安装成功
- **THEN** 自动运行时加载新安装的模块
- **AND** 刷新模块列表显示新模块
- **AND** 显示成功提示

#### Scenario: Upload failure handling
- **WHEN** 文件上传或安装失败
- **THEN** 显示错误提示包含失败原因
- **AND** 清理临时文件

### Requirement: Post-Installation Runtime Load
安装完成后 SHALL 自动加载模块到运行时，无需重启应用。

#### Scenario: Auto-load after installation
- **WHEN** 模块安装成功且 `IsEnabled=true`
- **THEN** 调用 `IModuleLoader.LoadAsync` 加载模块
- **AND** 模块状态变为 `Active`
- **AND** 模块菜单添加到导航

#### Scenario: Load failure marks module error
- **WHEN** 安装成功但运行时加载失败
- **THEN** 模块状态标记为 `Error`
- **AND** 显示加载失败的诊断信息
- **AND** 模块仍然已安装，可重试加载

