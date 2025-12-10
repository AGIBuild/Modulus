## Context

Modulus 是一个模块化应用框架，模块通过 `AssemblyLoadContext` 实现隔离。当前开发流程中，模块构建后输出到 `artifacts/Modules/{ModuleName}/`，Host 从该目录加载模块。但缺乏标准化的打包和分发机制。

**现有构建流程**:
- `nuke build-module` - 构建模块到 `artifacts/Modules/{ModuleName}/`
- 输出包含：程序集 DLL、extension.vsixmanifest

**目标**:
- 提供 `.modpkg` 打包格式用于分发
- 提供 CLI 工具用于安装/卸载模块

## Goals / Non-Goals

**Goals**:
- 实现 `nuke pack-module` 构建目标
- 打包格式 self-contained（包含所有依赖 DLL）
- 实现 `modulus install/uninstall` CLI 命令
- 安装到用户模块目录 `%APPDATA%/Modulus/Modules/`

**Non-Goals**:
- 包签名和完整性校验（后续 change）
- 模块商店/仓库集成
- 自动更新机制
- GUI 安装界面

## Decisions

### 1. 打包格式

**Decision**: `.modpkg` 为 ZIP 格式，包含所有运行时依赖。

```
{ModuleName}-{Version}.modpkg (ZIP)
├── extension.vsixmanifest        # 清单文件 (必须)
├── {ModuleName}.Core.dll         # Core 程序集
├── {ModuleName}.UI.Avalonia.dll  # Avalonia UI 程序集
├── {ModuleName}.UI.Blazor.dll    # Blazor UI 程序集
├── ThirdParty.dll                # 第三方依赖
├── README.md                     # 可选
└── LICENSE.txt                   # 可选
```

**版本号来源**: 从 `extension.vsixmanifest` 的 `Identity/@Version` 读取。

**Alternatives considered**:
- NuGet 包格式 (.nupkg) - 过重，需要额外的 NuGet 基础设施
- 仅打包模块程序集（不含依赖）- 可能导致运行时依赖缺失

### 2. 依赖处理策略

**Decision**: 打包时包含所有 NuGet 依赖的 DLL。

**原理分析**:

| 程序集类型 | 打包时 | 运行时加载 |
|-----------|--------|-----------|
| 模块程序集 | 包含 | `ModuleLoadContext` 从模块目录加载 |
| NuGet 依赖 | 包含 | `ModuleLoadContext` 从模块目录加载（私有副本） |
| 共享程序集 | 不包含 | 从 Host Default ALC 加载 |

**共享程序集列表** (不应打包):
- `Modulus.Core.dll`
- `Modulus.Sdk.dll`
- `Modulus.UI.Abstractions.dll`
- `Modulus.UI.Avalonia.dll`
- `Modulus.UI.Blazor.dll`
- `System.*`, `Microsoft.Extensions.*` 等框架程序集

**实现**: 使用 `dotnet publish` 输出，然后过滤掉共享程序集。

### 3. 版本冲突处理

**Decision**: 依赖 AssemblyLoadContext 隔离机制，不引入额外的版本协调。

**现有机制已足够**:
1. 每个模块有独立的 `ModuleLoadContext`
2. 非共享程序集从模块目录加载私有副本
3. 共享程序集从 Host 加载（版本由 Host 决定）

**风险**: 如果模块依赖的共享程序集版本与 Host 不兼容，可能导致运行时错误。

**Mitigation**: 
- 清单中可声明 `InstallationTarget/@Version` 指定支持的 Host 版本范围
- 安装时验证 Host 版本兼容性

### 4. CLI 架构

**Decision**: 创建独立的 `Modulus.Cli` 控制台项目，复用核心安装服务写入数据库。

```
src/Modulus.Cli/
├── Modulus.Cli.csproj
├── Program.cs
├── Services/
│   └── CliServiceProvider.cs    # DI 容器配置
└── Commands/
    ├── InstallCommand.cs
    ├── UninstallCommand.cs
    └── ListCommand.cs
```

**项目依赖**:
- `Modulus.Core` - 安装服务、清单验证
- `Modulus.Infrastructure.Data` - 数据库访问
- `System.CommandLine` - CLI 框架

**命令设计**:

```bash
# 从 .modpkg 文件安装
modulus install ./MyModule-1.0.0.modpkg

# 从目录安装（开发用）
modulus install ./artifacts/Modules/MyModule/

# 卸载模块
modulus uninstall MyModule
# 或
modulus uninstall a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d

# 列出已安装模块
modulus list
```

**安装目录**: `%APPDATA%/Modulus/Modules/{ModuleId}/`
**数据库路径**: `%APPDATA%/Modulus/Modulus.db`（与 Host 共享）

### 5. 安装流程

```
用户执行 modulus install xxx.modpkg
        │
        ▼
┌─────────────────────────────┐
│ 1. 解压到临时目录            │
│ 2. 读取 extension.vsixmanifest │
│ 3. 验证清单格式              │
└─────────────────────────────┘
        │
        ▼
┌─────────────────────────────┐
│ 4. 检查目标目录是否已存在     │
│    - 已存在: 提示覆盖/取消   │
│    - 不存在: 继续            │
└─────────────────────────────┘
        │
        ▼
┌─────────────────────────────┐
│ 5. 复制文件到目标目录:        │
│    %APPDATA%/Modulus/Modules/{Id}/ │
│ 6. 清理临时目录              │
└─────────────────────────────┘
        │
        ▼
┌─────────────────────────────┐
│ 7. 写入数据库:               │
│    - 运行 EF migrations      │
│    - 调用 ModuleInstallerService │
│    - 写入 ModuleEntity       │
│    - 写入 MenuEntity         │
└─────────────────────────────┘
        │
        ▼
┌─────────────────────────────┐
│ 8. 输出安装成功信息          │
│    - 模块名称、版本、路径    │
│    - 提示重启 Host 加载模块  │
└─────────────────────────────┘
```

**数据库操作**:
- CLI 复用 `ModuleInstallerService.InstallFromPathAsync` 写入数据库
- 使用 `LocalStorage.GetDatabasePath("Modulus")` 获取数据库路径（与 Host 共享）
- 安装前自动运行 EF Core migrations 确保表结构最新
- Host 启动时检测模块已注册，直接加载无需重复注册

## Risks / Trade-offs

| 风险 | 影响 | Mitigation |
|------|------|------------|
| 打包体积大（包含所有依赖） | 分发文件较大 | 可接受，确保独立运行 |
| 共享程序集版本不兼容 | 运行时错误 | InstallationTarget 版本检查 |
| CLI 与 Host 数据库不同步 | 模块状态不一致 | Host 启动时扫描目录重新注册 |

### 6. 共享程序集版本兼容性

**Decision**: 通过 Host 版本绑定确保共享程序集兼容性。

**问题**: 共享程序集（`Modulus.*`）从 Host 加载，模块无法使用私有版本。如果模块开发时依赖的版本与 Host 提供的版本不兼容，会导致运行时错误。

**解决方案**: 增强 `InstallationTarget` 版本验证

| 组件 | 变更 |
|------|------|
| `RuntimeContext` | 新增 `HostVersion` 属性 |
| `DefaultManifestValidator` | 验证 `InstallationTarget/@Version` 范围 |
| Host 启动代码 | 注册 Host 版本到 RuntimeContext |
| CLI install | 输出版本兼容性警告 |

**版本绑定原理**:
- `Modulus.Host.Avalonia 1.0.0` 隐含提供 `Modulus.Core 1.0.x`, `Modulus.Sdk 1.0.x` 等
- 模块声明 `InstallationTarget Version="[1.0,2.0)"` 表示兼容 Host 1.x 系列
- Host 加载模块时验证版本范围，不满足则拒绝加载

**Semantic Versioning 约定**:
- Major: 框架 API 破坏性变更
- Minor: 新增 API，向后兼容
- Patch: Bug 修复

**Alternatives considered**:
- 独立声明每个共享程序集版本 - 过于复杂，manifest 冗长
- 运行时动态检测 - 延迟发现问题，用户体验差

---

## Open Questions

1. ~~是否需要在打包时生成依赖清单（deps.json）？~~ 
   - 决定：不需要，使用 `dotnet publish` 标准输出

2. ~~CLI 是否需要支持从 URL 安装？~~
   - 决定：后续 change，当前仅支持本地文件

3. 是否需要支持模块禁用而非卸载？
   - 暂定：CLI 仅支持 install/uninstall，禁用通过 Host UI 操作

