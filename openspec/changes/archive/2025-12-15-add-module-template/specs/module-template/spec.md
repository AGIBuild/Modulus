## ADDED Requirements

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

系统 SHALL 提供 `modulus new` CLI 命令，用于创建 Modulus 模块。

#### Scenario: CLI 向导模式创建模块

- **WHEN** 用户执行 `modulus new MyModule`，且未提供 `--target` 参数
- **THEN** 系统进入交互式向导模式
- **AND** 提示用户选择目标 Host（Avalonia 或 Blazor）
- **AND** 提示用户输入可选信息（显示名称、描述等）

#### Scenario: CLI 批处理模式创建模块

- **WHEN** 用户执行 `modulus new MyModule --target avalonia`
- **THEN** 系统直接使用提供的参数值创建项目
- **AND** 未提供的可选参数使用默认值

### Requirement: Module Project Structure

系统 SHALL 根据目标 Host 生成对应的模块项目结构。

#### Scenario: Avalonia 模块结构

- **WHEN** 创建 Avalonia 模块，名称为 "MyModule"
- **THEN** 生成以下结构：
  ```
  MyModule/
  ├── MyModule.Core/
  │   ├── MyModule.Core.csproj
  │   ├── MyModuleModule.cs
  │   └── ViewModels/MainViewModel.cs
  ├── MyModule.UI.Avalonia/
  │   ├── MyModule.UI.Avalonia.csproj
  │   ├── MyModuleAvaloniaModule.cs
  │   ├── MainView.axaml
  │   └── MainView.axaml.cs
  └── extension.vsixmanifest
  ```

#### Scenario: Blazor 模块结构

- **WHEN** 创建 Blazor 模块，名称为 "MyModule"
- **THEN** 生成以下结构：
  ```
  MyModule/
  ├── MyModule.Core/
  │   ├── MyModule.Core.csproj
  │   ├── MyModuleModule.cs
  │   └── ViewModels/MainViewModel.cs
  ├── MyModule.UI.Blazor/
  │   ├── MyModule.UI.Blazor.csproj
  │   ├── MyModuleBlazorModule.cs
  │   ├── _Imports.razor
  │   └── MainView.razor
  └── extension.vsixmanifest
  ```

### Requirement: NuGet Package References

系统 SHALL 在生成的项目中使用 NuGet 包引用。

#### Scenario: Core 项目包引用

- **WHEN** 生成 Core 项目
- **THEN** `.csproj` 包含：
  - `Modulus.Sdk`
  - `Modulus.UI.Abstractions`

#### Scenario: Avalonia UI 项目包引用

- **WHEN** 生成 Avalonia UI 项目
- **THEN** `.csproj` 包含：
  - `Modulus.Sdk`
  - `Modulus.UI.Abstractions`
  - `Modulus.UI.Avalonia`
  - `Avalonia`

#### Scenario: Blazor UI 项目包引用

- **WHEN** 生成 Blazor UI 项目
- **THEN** `.csproj` 包含：
  - `Modulus.Sdk`
  - `Modulus.UI.Abstractions`
  - `MudBlazor`
  - `Microsoft.AspNetCore.Components.Web`

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

