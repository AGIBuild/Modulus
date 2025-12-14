# module-template Specification (Delta)

## MODIFIED Requirements

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
  - `Agibuild.Modulus.Sdk`
  - `Agibuild.Modulus.UI.Abstractions`

#### Scenario: Avalonia UI 项目包引用
- **WHEN** 生成 Avalonia UI 项目
- **THEN** `.csproj` 包含：
  - `Agibuild.Modulus.Sdk`
  - `Agibuild.Modulus.UI.Abstractions`
  - `Agibuild.Modulus.UI.Avalonia`
  - `Avalonia`

#### Scenario: Blazor UI 项目包引用
- **WHEN** 生成 Blazor UI 项目
- **THEN** `.csproj` 包含：
  - `Agibuild.Modulus.Sdk`
  - `Agibuild.Modulus.UI.Abstractions`
  - `MudBlazor`
  - `Microsoft.AspNetCore.Components.Web`


