# Change: 添加模块项目模板

## Why

当前开发者创建新模块需要手动复制现有模块（如 EchoPlugin），然后逐一修改命名空间、GUID、清单文件等，过程繁琐且容易出错。需要提供项目模板，让开发者能够通过 **Visual Studio 向导** 或 **CLI 命令** 快速创建符合 Modulus 规范的模块项目结构。

## What Changes

- 创建 **Visual Studio 多项目模板**（`.vstemplate` 格式），支持 VS 向导创建
- 添加 `modulus new` **CLI 命令**，支持向导式创建模块项目
- 实现模块项目模板，基于 EchoPlugin 结构生成两层项目：
  - `{ModuleName}.Core` - 核心业务逻辑（UI 无关）
  - `{ModuleName}.UI.Avalonia` **或** `{ModuleName}.UI.Blazor` - 用户选择其一
  - `extension.vsixmanifest` - 模块清单文件
- 模板使用 **NuGet 包引用**（Modulus.Sdk、Modulus.UI.* 等发布到 NuGet）
- 生成的项目可以直接编译通过

## Impact

- Affected specs: 新增 `module-template` 规范
- Affected code:
  - `templates/VisualStudio/` - VS 项目模板（Avalonia 和 Blazor 两套）
  - `src/Modulus.Cli/Commands/NewCommand.cs` - CLI 创建命令
  - `src/Modulus.Cli/Templates/` - CLI 内嵌模板文件
  - `src/Modulus.Cli/Program.cs` - 注册新命令
- **前置条件**: Modulus.Sdk、Modulus.UI.Abstractions、Modulus.UI.Avalonia 需先发布到 NuGet

