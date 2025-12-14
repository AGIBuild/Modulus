# module-packaging Specification (Delta)

## MODIFIED Requirements

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
- **AND** 不包含共享程序集（Agibuild.Modulus.Core, Agibuild.Modulus.Sdk, Agibuild.Modulus.UI.* 等）


