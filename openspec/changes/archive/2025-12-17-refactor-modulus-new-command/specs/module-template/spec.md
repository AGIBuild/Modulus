# module-template Specification (Delta)

## MODIFIED Requirements

### Requirement: CLI Template Command

系统 SHALL 提供 `modulus new` CLI 命令，用于创建 Modulus 模块，并遵循 `dotnet new` 的最佳实践：以模板选择为主、以通用参数为辅，避免暴露模板内部元数据参数。

#### Scenario: Default template is module-avalonia
- **WHEN** 用户执行 `modulus new -n MyModule`
- **THEN** 系统使用 `module-avalonia` 模板创建模块
- **AND** 创建包含 `MyModule.Core` 与 `MyModule.UI.Avalonia` 的解决方案

#### Scenario: Explicit template creates module
- **WHEN** 用户执行 `modulus new module-blazor -n MyModule`
- **THEN** 系统使用 `module-blazor` 模板创建模块
- **AND** 创建包含 `MyModule.Core` 与 `MyModule.UI.Blazor` 的解决方案

#### Scenario: List templates
- **WHEN** 用户执行 `modulus new --list`
- **THEN** 系统输出可用模板列表（至少包含 `module-avalonia` 与 `module-blazor`）
- **AND** 命令退出码为 0
- **AND** 不创建任何文件或目录

#### Scenario: Removed legacy options are rejected
- **WHEN** 用户执行 `modulus new MyModule --target avalonia`
- **THEN** 系统提示参数不受支持（legacy options removed）
- **AND** 命令退出码为非 0


