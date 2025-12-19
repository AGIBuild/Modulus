# module-template Specification

## Purpose
模块项目模板规范，定义 Visual Studio 模板和 CLI 模板命令的行为。本规范覆盖生成文件结构、默认依赖、可编译性与清单有效性等最低保障，以避免模板漂移导致的创建失败或编译失败。
## Requirements
### Requirement: Visual Studio Project Template

系统 SHALL 提供 Visual Studio 多项目模板，支持通过 VS 向导创建 Modulus 模块。

#### Scenario: VS 向导创建 Avalonia 模块

- **WHEN** 用户在 VS 中选择"Modulus Module (Avalonia)"模板
- **AND** 输入项目名称为 "MyModule"
- **THEN** VS 创建包含 `MyModule.Core` 和 `MyModule.UI.Avalonia` 的解决方案
- **AND** 自动生成唯一 GUID
- **AND** 项目使用 NuGet 包引用

#### Scenario: VS 向导创建 Blazor 模块

- **WHEN** 用户在 VS 中选择"Modulus Module (Blazor)"模板
- **AND** 输入项目名称为 "MyModule"
- **THEN** VS 创建包含 `MyModule.Core` 和 `MyModule.UI.Blazor` 的解决方案
- **AND** 自动生成唯一 GUID
- **AND** 项目使用 NuGet 包引用

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

### Requirement: Module Project Structure
系统 SHALL 根据目标 Host 生成对应的模块项目结构。

#### Scenario: Avalonia 模块包含 View 级菜单声明
- **WHEN** 创建 Avalonia 模块
- **THEN** 生成的默认 ViewModel（如 `MainViewModel`）包含 View 级菜单声明（例如 `[AvaloniaViewMenu]`）
- **AND** 生成的默认 View（如 `MainView.axaml`）与 ViewModel 通过约定绑定（无需额外 `IViewRegistry.Register` 代码）
- **AND** 生成的 View 不包含无参构造函数，仅提供 DI 构造函数（例如 `MainView(MainViewModel vm)`）
- **AND** 生成的 View 使用 `Design.IsDesignMode` 分支支持设计态渲染（无需运行时 DI）
- **AND** 模板包含覆写导航生命周期/拦截的示例代码（如 `CanNavigateFromAsync/CanNavigateToAsync`）

#### Scenario: Blazor 模块包含 View 级菜单声明
- **WHEN** 创建 Blazor 模块
- **THEN** 生成的默认页面（如 `MainView.razor`）包含 View 级菜单声明（例如 `@attribute [BlazorViewMenu]`）
- **AND** 页面包含/继承支持 ViewModel 绑定的基类示例（使 ViewModel 可覆写导航生命周期/拦截）

### Requirement: NuGet Package References

系统 SHALL 在生成的项目中使用与当前 CLI/SDK API 一致的依赖方式，确保生成项目可直接编译通过。

#### Scenario: Core 项目包引用

- **WHEN** 生成 Core 项目
- **THEN** `.csproj` 引用 `Modulus.Sdk` 与 `Modulus.UI.Abstractions`（不依赖过时的外部 NuGet 包版本）

#### Scenario: Avalonia UI 项目包引用

- **WHEN** 生成 Avalonia UI 项目
- **THEN** `.csproj` 引用 `Modulus.Sdk`、`Modulus.UI.Abstractions`、`Modulus.UI.Avalonia`，并包含 `Avalonia` 依赖

#### Scenario: Blazor UI 项目包引用

- **WHEN** 生成 Blazor UI 项目
- **THEN** `.csproj` 引用 `Modulus.Sdk`、`Modulus.UI.Abstractions`、`Modulus.UI.Blazor`，并包含 `MudBlazor`、`Microsoft.AspNetCore.Components.Web` 依赖

#### Scenario: CLI generated projects compile without external Modulus NuGet feeds
- **WHEN** 用户通过 `modulus new` 生成模块项目
- **THEN** 生成的根目录包含 `Directory.Build.props`
- **AND** 该文件提供 `ModulusCliLibDir` 用于从 CLI 安装目录解析 `Modulus.*.dll`

### Requirement: Generated Project Compilability

系统 SHALL 确保生成的项目可直接编译通过。

#### Scenario: 生成的项目可编译

- **WHEN** 模块项目生成完成
- **THEN** 执行 `dotnet build` 应成功
- **AND** 无编译错误

#### Scenario: 生成的清单文件有效

- **WHEN** 模块项目生成完成
- **THEN** `extension.vsixmanifest` 包含有效 GUID
- **AND** 包含正确的 Asset 声明

