# Change: 采用 VS Extension vsixmanifest 格式并简化模块流程

## Why

当前 Modulus 使用自定义的 `manifest.json` (JSON) 格式定义模块元数据，且安装/加载流程存在冗余（manifest 重复读取、程序集重复加载、验证重复执行）。

本提案将：
1. 采用 VS Extension 的 `extension.vsixmanifest` (XML) 格式，便于 VS 扩展开发者迁移
2. 简化安装流程，去除安装时的程序集加载
3. 优化加载流程，去除重复验证

## What Changes

### Part 1: Manifest 格式变更
- **BREAKING**: 将 `manifest.json` 替换为 `extension.vsixmanifest` (XML 格式)
- **BREAKING**: 数据模型完全对齐 vsixmanifest 2.0 schema
- Host ID 映射: `BlazorApp` → `Modulus.Host.Blazor`, `AvaloniaApp` → `Modulus.Host.Avalonia`

### Part 2: 入口点机制变更 (渐进式迁移)
- 新增 `ModulusPackage` 基类 (继承 `ModulusComponent`)
- 标记 `ModulusComponent` 为 `[Obsolete]`
- 运行时同时扫描两种基类 (向后兼容)

### Part 3: 菜单声明通过入口类型属性
- 菜单通过 host-specific 模块入口类型上的菜单属性声明（安装/更新时 metadata-only 解析并投影到 DB）

### Part 4: 流程简化
- **移除目录扫描**: 删除 `DirectoryModuleProvider` 和 `IModuleProvider`
- **新增显式安装**: 系统模块通过 `SystemModuleInstaller` 硬编码安装
- 安装阶段：不再创建临时 ALC 加载程序集
- 加载阶段：不再重复验证 manifest，信任数据库状态
- 新增 manifest hash 用于变更检测

### Asset Type 约定
| Type | 用途 |
|------|------|
| `Modulus.Package` | 包含入口点的程序集 |
| `Modulus.Assembly` | 普通依赖程序集 |
| `Modulus.Icon` | 图标资源 |

### 移除的字段
- `manifestVersion` → `PackageManifest/@Version`
- `entryComponent` → Asset 扫描
- `coreAssemblies` / `uiAssemblies` → `Assets`
- `sharedAssemblyHints` → 运行时配置

## Impact

- Affected specs: `runtime`
- Affected code:
  - `src/Modulus.Sdk/`
    - 新增 `ModulusPackage`
    - 重写 `ModuleManifest.cs` → `VsixManifest.cs`
  - `src/Modulus.Core/Manifest/`
    - 重写 `ManifestReader.cs` (XML)
    - 重写 `DefaultManifestValidator.cs`
  - `src/Modulus.Core/Runtime/`
    - 简化 `ModuleLoader.cs`
    - **删除** `DirectoryModuleProvider.cs`
    - **删除** `IModuleProvider.cs`
    - 简化 `ModulusApplicationFactory.cs` (移除目录扫描)
  - `src/Modulus.Core/Installation/`
    - 简化 `ModuleInstallerService.cs` (去除程序集加载)
    - **重构** `SystemModuleSeeder.cs` → `SystemModuleInstaller.cs`
  - `src/Shared/Modulus.Infrastructure.Data/`
    - 更新 `ModuleEntity.cs` (新增 hash 字段)
    - 数据库迁移
  - `src/Modules/*/`
    - `manifest.json` → `extension.vsixmanifest` (3 个模块)
    - 菜单特性 → manifest Assets
