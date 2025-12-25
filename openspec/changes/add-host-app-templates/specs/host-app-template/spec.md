# host-app-template Specification (Delta)

## ADDED Requirements

### Requirement: CLI Host App Templates
The system SHALL provide CLI templates to create plugin-based Host App projects via `modulus new avaloniaapp` and `modulus new blazorapp`.

#### Scenario: Create Avalonia Host App from CLI
- **WHEN** 用户执行 `modulus new avaloniaapp -n MyApp`
- **THEN** 生成一个可编译的 Host App 解决方案
- **AND** 该 Host App 基于 Host SDK 完成运行时组合（不复制仓库 Host 源码）

#### Scenario: Create Blazor Hybrid(MAUI) Host App from CLI
- **WHEN** 用户执行 `modulus new blazorapp -n MyApp`
- **THEN** 生成一个可编译的 Host App 解决方案（Blazor Hybrid(MAUI)）
- **AND** 该 Host App 基于 Host SDK 完成运行时组合（不复制仓库 Host 源码）

#### Scenario: List includes app and module templates
- **WHEN** 用户执行 `modulus new --list`
- **THEN** 输出至少包含：`avaloniaapp`, `blazorapp`, `module-avalonia`, `module-blazor`
- **AND** 命令退出码为 0
- **AND** 不创建任何文件或目录

### Requirement: Visual Studio Host App Templates
The system SHALL provide Visual Studio wizard templates that create the same Host App projects as the CLI templates.

#### Scenario: VS wizard creates Avalonia Host App
- **WHEN** 用户在 Visual Studio 中选择 “Modulus Host App (Avalonia)” 模板并完成向导
- **THEN** 生成的项目结构与 CLI `modulus new avaloniaapp` 等价（同样的 Host SDK 接入点与配置契约）

#### Scenario: VS wizard creates Blazor Hybrid Host App
- **WHEN** 用户在 Visual Studio 中选择 “Modulus Host App (Blazor Hybrid)” 模板并完成向导
- **THEN** 生成的项目结构与 CLI `modulus new blazorapp` 等价（同样的 Host SDK 接入点与配置契约）

### Requirement: Host Project Design Contract
Generated Host App projects MUST use Host SDK builder/options for runtime composition, and MUST NOT copy or depend on repository host implementation types.

#### Scenario: Host composition uses Host SDK only
- **WHEN** 用户查看生成的 Host 入口代码
- **THEN** 入口代码只负责构建配置/日志并调用 Host SDK 创建 `IModulusApplication`
- **AND** 不包含“安装/投影/模块加载/Host 绑定”等仓库 Host 内部实现的复制代码

### Requirement: Shared Assembly Policy in Host App Templates (Prefixes)
Generated Host App projects SHALL configure shared assembly policy using prefix/pattern rules for framework families.

#### Scenario: Host appsettings uses prefix rules
- **WHEN** 生成 Host App 项目
- **THEN** `appsettings.json` 中包含 `Modulus:Runtime:SharedAssemblyPrefixes`
- **AND** 该列表至少包含与目标 Host 对应的框架前缀规则（例如 Avalonia 或 MAUI/MudBlazor）


