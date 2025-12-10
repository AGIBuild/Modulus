## ADDED Requirements

### Requirement: Bundled modules configuration
Host 项目 SHALL 通过 `bundled-modules.json` 配置文件声明内置模块列表。

#### Scenario: Configuration file format
- **WHEN** Host 项目包含 `bundled-modules.json`
- **THEN** 文件格式为 JSON，包含 `modules` 数组
- **AND** 数组元素为模块目录名（如 `"EchoPlugin"`）

#### Scenario: Empty configuration
- **WHEN** `bundled-modules.json` 的 `modules` 数组为空
- **THEN** 不打包任何内置模块
- **AND** 构建正常完成

#### Scenario: Missing configuration file
- **WHEN** Host 项目不包含 `bundled-modules.json`
- **THEN** 不打包任何内置模块
- **AND** 构建正常完成（无警告）

### Requirement: Bundle modules build target
构建系统 SHALL 提供 `BundleModules` 目标验证并打包内置模块。

#### Scenario: Build bundles configured modules
- **WHEN** 执行 `nuke build`
- **AND** `bundled-modules.json` 配置了 `["EchoPlugin", "ComponentsDemo"]`
- **THEN** 验证 `artifacts/Modules/EchoPlugin/` 存在
- **AND** 验证 `artifacts/Modules/ComponentsDemo/` 存在
- **AND** 输出打包成功信息

#### Scenario: Build fails on missing module
- **WHEN** 配置了模块名 `"NonExistentModule"`
- **AND** `artifacts/Modules/NonExistentModule/` 不存在
- **THEN** 构建失败
- **AND** 输出错误：模块 NonExistentModule 未找到

#### Scenario: Bundle respects target host
- **WHEN** 执行 `nuke build --app avalonia`
- **THEN** 仅读取 `Modulus.Host.Avalonia` 的 `bundled-modules.json`
- **AND** 不处理其他 Host 的配置

### Requirement: Bundled modules runtime behavior
内置模块 SHALL 在 Host 启动时自动加载并标记为系统模块。

#### Scenario: Bundled modules loaded as system
- **WHEN** Host 启动（RELEASE 模式）
- **AND** `{AppBaseDir}/Modules/` 包含模块
- **THEN** 模块以 `IsSystem: true` 加载
- **AND** 自动注册到数据库

#### Scenario: System modules cannot be uninstalled
- **WHEN** 用户尝试卸载内置模块
- **THEN** 操作被拒绝
- **AND** UI 不显示卸载选项

#### Scenario: System modules take precedence
- **WHEN** 内置模块与用户安装模块 ID 相同
- **THEN** 内置模块优先加载
- **AND** 用户模块被忽略

