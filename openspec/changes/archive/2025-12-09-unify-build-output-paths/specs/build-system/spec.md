## ADDED Requirements

### Requirement: Unified Output Path
构建系统 SHALL 将所有构建输出到 `artifacts/` 根目录，确保 IDE 和 CLI 构建行为一致。

#### Scenario: IDE build outputs to artifacts
- **WHEN** 开发者在 IDE 中构建项目
- **THEN** 输出程序集 SHALL 放置在 `artifacts/`

#### Scenario: Nuke compile outputs to artifacts
- **WHEN** 运行 `nuke compile`
- **THEN** 输出程序集 SHALL 放置在 `artifacts/`

#### Scenario: Host executable location
- **WHEN** 宿主应用程序构建完成
- **THEN** 可执行文件 SHALL 位于 `artifacts/Modulus.Host.{HostType}.exe`

### Requirement: Module Output Path
模块 SHALL 输出到 `artifacts/Modules/{ModuleName}/` 子目录，按模块名称组织。

#### Scenario: Module build output
- **WHEN** 构建模块
- **THEN** 模块文件 SHALL 放置在 `artifacts/Modules/{ModuleName}/`

#### Scenario: Module manifest location
- **WHEN** 模块打包完成
- **THEN** `manifest.json` SHALL 位于 `artifacts/Modules/{ModuleName}/manifest.json`

### Requirement: Configuration Switch
构建系统 SHALL 通过 `--configuration` 参数支持 Debug/Release 模式切换。

#### Scenario: Default debug configuration
- **WHEN** 运行 `nuke build` 不带配置参数
- **THEN** SHALL 使用 Debug 配置构建

#### Scenario: Specify release configuration
- **WHEN** 运行 `nuke build --configuration Release`
- **THEN** SHALL 使用 Release 配置构建

### Requirement: Test Suite Execution
`nuke test` 目标 SHALL 成功执行所有测试。

#### Scenario: Nuke test completes without errors
- **WHEN** 运行 `nuke test`
- **THEN** 所有测试 SHALL 通过
- **AND** 退出码 SHALL 为 0
