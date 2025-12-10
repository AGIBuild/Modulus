# module-packaging Specification

## Purpose
TBD - created by archiving change add-module-packaging. Update Purpose after archive.
## Requirements
### Requirement: Module packaging via Nuke build
构建系统 SHALL 提供 `pack-module` 目标将模块打包为 `.modpkg` 文件。

#### Scenario: Pack all modules
- **WHEN** 执行 `nuke pack-module` 不带参数
- **THEN** 打包 `src/Modules/` 下所有包含 `extension.vsixmanifest` 的模块
- **AND** 输出到 `artifacts/packages/{ModuleName}-{Version}.modpkg`
- **AND** 版本号从清单 `Identity/@Version` 读取

#### Scenario: Pack single module by name
- **WHEN** 执行 `nuke pack-module --name EchoPlugin`
- **THEN** 仅打包指定名称的模块
- **AND** 输出到 `artifacts/packages/EchoPlugin-{Version}.modpkg`

#### Scenario: Module without manifest is skipped
- **WHEN** 模块目录不包含 `extension.vsixmanifest`
- **THEN** 跳过该模块并输出警告

#### Scenario: Package includes all dependencies
- **WHEN** 模块依赖第三方 NuGet 包
- **THEN** 打包输出包含所有依赖 DLL
- **AND** 不包含共享程序集（Modulus.Core, Modulus.Sdk, Modulus.UI.* 等）

### Requirement: Module package format
`.modpkg` 文件 SHALL 为 ZIP 格式，包含模块运行所需的所有文件。

#### Scenario: Package structure is valid ZIP
- **WHEN** 用户获得 `.modpkg` 文件
- **THEN** 可使用标准 ZIP 工具解压
- **AND** 根目录包含 `extension.vsixmanifest`

#### Scenario: Package contains required files
- **WHEN** 打包完成
- **THEN** 包内包含 `extension.vsixmanifest`
- **AND** 包含清单 `Assets` 中声明的所有 DLL
- **AND** 包含模块依赖的第三方 DLL

#### Scenario: Optional files included when present
- **WHEN** 模块目录包含 `README.md` 或 `LICENSE.txt`
- **THEN** 这些文件包含在打包输出中

### Requirement: CLI install command
CLI SHALL 提供 `install` 命令从 `.modpkg` 文件或目录安装模块，并写入数据库。

#### Scenario: Install from modpkg file
- **WHEN** 执行 `modulus install ./MyModule-1.0.0.modpkg`
- **THEN** 解压包内容到 `%APPDATA%/Modulus/Modules/{ModuleId}/`
- **AND** 写入模块记录到数据库（ModuleEntity, MenuEntity）
- **AND** 输出安装成功信息（模块名、版本、安装路径）
- **AND** 提示用户重启 Host 加载模块

#### Scenario: Install from directory
- **WHEN** 执行 `modulus install ./artifacts/Modules/MyModule/`
- **AND** 目录包含 `extension.vsixmanifest`
- **THEN** 复制目录内容到 `%APPDATA%/Modulus/Modules/{ModuleId}/`
- **AND** 写入模块记录到数据库

#### Scenario: Install prompts on existing module
- **WHEN** 目标模块目录已存在
- **THEN** 提示用户确认覆盖或取消
- **AND** 使用 `--force` 参数可跳过确认

#### Scenario: Install validates manifest
- **WHEN** 包内 `extension.vsixmanifest` 格式无效
- **THEN** 安装失败并输出错误详情

#### Scenario: Install with invalid path fails
- **WHEN** 执行 `modulus install ./nonexistent.modpkg`
- **THEN** 输出错误：文件不存在

#### Scenario: Install runs database migrations
- **WHEN** 数据库表结构不存在或需要更新
- **THEN** 自动运行 EF Core migrations
- **AND** 继续安装流程

### Requirement: CLI uninstall command
CLI SHALL 提供 `uninstall` 命令移除已安装的模块，包括文件和数据库记录。

#### Scenario: Uninstall by module name
- **WHEN** 执行 `modulus uninstall MyModule`
- **AND** 数据库中存在匹配的模块记录
- **THEN** 删除模块目录
- **AND** 从数据库删除 ModuleEntity 和关联的 MenuEntity
- **AND** 输出卸载成功信息

#### Scenario: Uninstall by module ID
- **WHEN** 执行 `modulus uninstall a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d`
- **THEN** 删除匹配该 GUID 的模块目录和数据库记录

#### Scenario: Uninstall nonexistent module
- **WHEN** 指定的模块在数据库中不存在
- **THEN** 输出错误：模块未安装

#### Scenario: Uninstall prompts for confirmation
- **WHEN** 执行卸载命令
- **THEN** 提示用户确认
- **AND** 使用 `--force` 参数可跳过确认

#### Scenario: Uninstall system module blocked
- **WHEN** 尝试卸载 IsSystem=true 的模块
- **THEN** 输出错误：系统模块不可卸载

### Requirement: CLI list command
CLI SHALL 提供 `list` 命令从数据库查询并显示已安装模块。

#### Scenario: List shows installed modules
- **WHEN** 执行 `modulus list`
- **THEN** 从数据库查询所有模块
- **AND** 输出模块的名称、版本、ID、状态
- **AND** 按名称排序

#### Scenario: List with verbose flag
- **WHEN** 执行 `modulus list --verbose`
- **THEN** 额外显示安装路径、是否系统模块、验证时间

#### Scenario: List with no modules
- **WHEN** 数据库中没有已安装的模块
- **THEN** 输出提示信息：无已安装模块

### Requirement: Host version compatibility warning
CLI install SHALL 输出 Host 版本兼容性警告。

#### Scenario: Install shows version warning
- **WHEN** 执行 `modulus install ./MyModule-1.0.0.modpkg`
- **AND** 模块声明 `InstallationTarget Version="[1.2,2.0)"`
- **THEN** 输出警告：模块要求 Host 版本 [1.2,2.0)，请确保 Host 版本兼容
- **AND** 安装继续进行（不阻塞）

