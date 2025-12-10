## Context

Modulus 框架支持模块化扩展，但当前构建流程将 Host 和模块分别输出到不同位置：
- Host: `artifacts/`
- Modules: `artifacts/Modules/`

在 DEBUG 模式下，Host 从解决方案根目录的 `artifacts/Modules/` 加载模块。但在 RELEASE 模式下，Host 期望从 `{AppBaseDir}/Modules/` 加载内置模块，该目录当前为空。

**目标**: 框架 Owner 可声明哪些模块作为内置模块发布，运行即包含。

## Goals / Non-Goals

**Goals**:
- 配置文件声明内置模块列表
- 构建时自动复制内置模块到 Host 输出目录
- 内置模块标记为系统模块（不可卸载）

**Non-Goals**:
- 模块版本锁定（使用当前构建版本）
- 内置模块的独立更新机制
- 内置模块的签名验证

## Decisions

### 1. 配置文件格式

**Decision**: 使用 `bundled-modules.json` 配置文件。

```json
{
  "$schema": "./bundled-modules.schema.json",
  "modules": [
    "EchoPlugin",
    "ComponentsDemo"
  ]
}
```

**位置**: 每个 Host 项目目录下（如 `src/Hosts/Modulus.Host.Avalonia/bundled-modules.json`）

**Alternatives considered**:
- MSBuild ItemGroup (`<BundledModule>`) - 需要复杂的 MSBuild 集成
- appsettings.json 配置节 - 运行时配置，不适合构建时使用

### 2. 构建流程

**Decision**: 新增 `BundleModules` Nuke 目标，在 `BuildModule` 后执行。

```
nuke build
    │
    ├── Restore
    ├── BuildApp        → artifacts/*.dll
    ├── BuildModule     → artifacts/Modules/{ModuleName}/
    └── BundleModules   → 复制到 artifacts/Modules/ (最终位置)
```

**BundleModules 逻辑**:
1. 读取目标 Host 的 `bundled-modules.json`
2. 对于每个声明的模块名：
   - 从 `artifacts/Modules/{ModuleName}/` 复制到 `artifacts/Modules/`
3. 模块已在正确位置，无需额外复制（当前构建输出已是 `artifacts/Modules/`）

**注意**: 由于当前 `BuildModule` 已将模块输出到 `artifacts/Modules/`，`BundleModules` 主要用于：
- 验证配置的模块确实存在
- 未来支持选择性打包（仅打包声明的模块）

### 3. 发布包结构

```
artifacts/
├── Modulus.Host.Avalonia.exe
├── Modulus.Host.Avalonia.dll
├── appsettings.json
├── ... (Host 依赖)
└── Modules/
    ├── EchoPlugin/
    │   ├── extension.vsixmanifest
    │   ├── EchoPlugin.Core.dll
    │   └── EchoPlugin.UI.Avalonia.dll
    └── ComponentsDemo/
        ├── extension.vsixmanifest
        └── *.dll
```

### 4. 运行时行为

**现有代码已支持** (`App.axaml.cs`):

```csharp
#if !DEBUG
// Production: Load from {AppBaseDir}/Modules/ 
var appModules = Path.Combine(AppContext.BaseDirectory, "Modules");
if (Directory.Exists(appModules))
{
    moduleDirectories.Add(new ModuleDirectory(appModules, IsSystem: true));
}
#endif
```

- 内置模块自动标记为 `IsSystem: true`
- 系统模块在 UI 中不显示卸载按钮
- `SystemModuleInstaller` 自动注册到数据库

### 5. 模块冲突处理

**场景**: 用户安装了与内置模块同 ID 的模块。

**Decision**: 系统模块优先，用户模块被忽略。

**原理**: 
- 系统模块路径 (`{AppBaseDir}/Modules/`) 先于用户路径 (`%APPDATA%/Modulus/Modules/`) 加载
- 相同 ID 的模块只加载第一个

## Risks / Trade-offs

| 风险 | 影响 | Mitigation |
|------|------|------------|
| 配置模块名拼写错误 | 构建时无模块 | BundleModules 验证并报错 |
| 内置模块与用户模块冲突 | 用户模块被忽略 | 文档说明优先级规则 |
| 发布包体积增大 | 分发文件变大 | 可接受，按需配置 |

## Open Questions

1. 是否需要支持条件打包（如仅 Release 配置打包）？
   - 暂定：不需要，DEBUG 和 RELEASE 共用配置

