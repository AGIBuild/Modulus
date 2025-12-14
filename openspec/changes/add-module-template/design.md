# Design: 模块项目模板

## Context

Modulus 框架使用垂直切片架构，每个模块包含：
- Core 层（业务逻辑，UI 无关）
- UI.Avalonia 层（桌面 UI）或 UI.Blazor 层（Web UI）

开发者需要遵循特定的项目结构、命名空间约定、程序集域声明等。需要同时支持 **Visual Studio 向导** 和 **CLI 命令** 两种创建方式。

## Goals / Non-Goals

**Goals:**
- 提供 **Visual Studio 多项目模板**，支持 VS 向导创建模块
- 提供 `modulus new` **CLI 命令**创建模块
- 生成的代码可直接编译通过
- 使用 NuGet 包引用

**Non-Goals:**
- 不支持自定义模板（v1 scope）
- 不生成测试项目

## Decisions

### Decision 1: 双轨模板方案

| 方式 | 格式 | 适用场景 |
|------|------|----------|
| VS 项目模板 | `.vstemplate` | Visual Studio 用户，图形化向导 |
| CLI 命令 | 内嵌模板 | 命令行用户，CI/CD 场景 |

### Decision 2: VS 模板结构

使用 VS 多项目模板（Multi-Project Template）：

```
templates/VisualStudio/
├── ModulusModule.Avalonia/
│   ├── ModulusModule.Avalonia.vstemplate   # 主模板
│   ├── Core/
│   │   ├── Core.vstemplate
│   │   ├── $safeprojectname$Module.cs
│   │   ├── $safeprojectname$.Core.csproj
│   │   └── ViewModels/MainViewModel.cs
│   ├── UI.Avalonia/
│   │   ├── UI.Avalonia.vstemplate
│   │   ├── $safeprojectname$AvaloniaModule.cs
│   │   ├── $safeprojectname$.UI.Avalonia.csproj
│   │   ├── MainView.axaml
│   │   └── MainView.axaml.cs
│   └── extension.vsixmanifest
└── ModulusModule.Blazor/
    └── ... (类似结构)
```

VS 模板变量：
- `$safeprojectname$` - 项目名称
- `$guid1$` - 自动生成 GUID
- `$time$` - 创建时间

### Decision 3: CLI 模板结构

```
src/Modulus.Cli/Templates/
├── Core/
│   ├── __ModuleName__.Core.csproj.template
│   ├── __ModuleName__Module.cs.template
│   └── ViewModels/MainViewModel.cs.template
├── Avalonia/
│   ├── __ModuleName__.UI.Avalonia.csproj.template
│   ├── __ModuleName__AvaloniaModule.cs.template
│   ├── MainView.axaml.template
│   └── MainView.axaml.cs.template
├── Blazor/
│   └── ...
└── extension.vsixmanifest.template
```

CLI 模板变量（`{{VariableName}}`）：
- `{{ModuleName}}` - 模块名称
- `{{DisplayName}}` - 显示名称
- `{{Description}}` - 描述
- `{{Publisher}}` - 发布者
- `{{ModuleId}}` - GUID
- `{{Icon}}` - 图标
- `{{Order}}` - 排序

### Decision 4: 向导收集的信息

| 参数 | VS 模板 | CLI | 默认值 |
|------|---------|-----|--------|
| ModuleName | 项目名称对话框 | 位置参数 | (必需) |
| TargetHost | 选择不同模板 | `--target` | (必需) |
| DisplayName | - | `--display-name` | 同 ModuleName |
| Description | - | `--description` | "A Modulus module." |
| Publisher | - | `--publisher` | "Modulus Team" |

### Decision 5: NuGet 包引用

```xml
<!-- Core 项目 -->
<PackageReference Include="Modulus.Sdk" Version="1.0.0" />
<PackageReference Include="Modulus.UI.Abstractions" Version="1.0.0" />

<!-- Avalonia UI 项目 -->
<PackageReference Include="Modulus.UI.Avalonia" Version="1.0.0" />
<PackageReference Include="Avalonia" Version="11.3.9" />

<!-- Blazor UI 项目 -->
<PackageReference Include="MudBlazor" Version="8.15.0" />
```

### Decision 6: VS 模板安装方式

1. **手动安装**：将模板 `.zip` 复制到 `%USERPROFILE%\Documents\Visual Studio 2022\Templates\ProjectTemplates\`
2. **自动化脚本**：提供 PowerShell 脚本自动部署
3. **VSIX 扩展**（可选后续）：打包为 VS 扩展分发

### Decision 7: CLI 命令设计

```bash
# 向导模式
modulus new MyModule

# 批处理模式
modulus new MyModule --target avalonia
modulus new MyModule --target blazor

# 完整参数
modulus new MyModule --target avalonia --display-name "My Module" --output ./path
```

## Risks / Trade-offs

| 风险 | 缓解措施 |
|------|----------|
| VS/CLI 模板不同步 | 使用共享的模板定义，生成两套格式 |
| NuGet 包未发布 | 文档说明前置条件 |

## Open Questions

1. ~~是否需要 VSIX 扩展？~~ → v1 暂不需要，手动安装即可

