## Context

Modulus 是一个模块化应用框架，当前使用自定义 JSON 格式的 `manifest.json` 描述模块元数据。为了便于 VS Extension 开发者迁移现有扩展，需要采用 VS Extension 的标准 `extension.vsixmanifest` XML 格式，并参考 VS Extension 的工作原理设计 Modulus 扩展系统。

**vsixmanifest schema 参考**: `http://schemas.microsoft.com/developer/vsx-schema/2011`

## Goals / Non-Goals

**Goals:**
- 完全匹配 vsixmanifest 2.0 schema 的字段结构和语义
- 通过入口点类型自动发现和加载程序集
- 设计清晰的扩展开发、打包、安装、加载流程

**Non-Goals:**
- 不支持旧版 manifest.json 格式 (无向后兼容)
- 不实现直接加载 VS Extension 的 VSIX 包
- 不实现自动转换工具 (可作为后续工作)

---

## 流程简化设计

### 当前流程问题分析

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           当前流程 (存在冗余)                                 │
├──────────────────────────────────────────────────────────────────────────────┤
│  安装阶段 (SystemModuleSeeder + ModuleInstallerService)                      │
│  ├─→ 读取 manifest.json                    ← 第 1 次读取                    │
│  ├─→ 验证 manifest                         ← 第 1 次验证                    │
│  ├─→ 创建临时 ModuleLoadContext                                             │
│  ├─→ 加载程序集到临时 ALC                  ← 第 1 次加载 (仅为扫描菜单)     │
│  ├─→ 扫描 IModule 和 Menu 特性             ← 第 1 次反射扫描                │
│  ├─→ 写入数据库                                                             │
│  └─→ 卸载临时 ALC                          ← 浪费的加载/卸载                │
│                                                                              │
│  加载阶段 (ModuleLoader.LoadAsync)                                           │
│  ├─→ 读取 manifest.json                    ← 第 2 次读取 (重复!)            │
│  ├─→ 验证 host 兼容性                      ← 第 2 次验证 (重复!)            │
│  ├─→ 创建正式 ModuleLoadContext                                             │
│  ├─→ 加载程序集                            ← 第 2 次加载                    │
│  ├─→ 扫描入口点                            ← 第 2 次反射扫描                │
│  └─→ 执行生命周期                                                           │
└──────────────────────────────────────────────────────────────────────────────┘
```

**问题汇总**:
| 操作 | 安装阶段 | 加载阶段 | 问题 |
|------|---------|---------|------|
| 读取 manifest | ✓ | ✓ | 重复 IO |
| 验证 manifest | ✓ | ✓ | 重复计算 |
| 加载程序集 | ✓ (临时) | ✓ (正式) | 两次反射开销 |
| 扫描菜单/入口点 | ✓ | ✓ | 重复反射 |

### 简化后流程

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           简化后流程 (无目录扫描)                             │
├──────────────────────────────────────────────────────────────────────────────┤
│  首次启动 (SystemModuleInstaller)                                            │
│  └─→ 安装内置系统模块 (硬编码路径列表)                                       │
│                                                                              │
│  安装阶段 (ModuleInstallerService) ← 显式触发，不自动扫描                    │
│  ├─→ 读取 extension.vsixmanifest   ← 唯一一次 manifest IO                   │
│  ├─→ 验证 manifest                 ← 唯一一次验证                           │
│  ├─→ 从 manifest Assets 提取菜单   ← XML 解析，无需反射                     │
│  ├─→ 计算 manifest hash                                                      │
│  └─→ 写入数据库 (含验证结果和 hash)                                          │
│                                                                              │
│  启动加载阶段 (ModuleLoader) ← 直接从数据库加载                              │
│  ├─→ 查询数据库已启用模块          ← 不扫描目录                             │
│  ├─→ 检查 State == Ready           ← 不重新验证                             │
│  ├─→ 按需读取 manifest (仅取 Assets)                                         │
│  ├─→ 创建 ModuleLoadContext                                                  │
│  ├─→ 加载程序集                    ← 唯一一次加载                           │
│  ├─→ 扫描入口点                    ← 唯一一次反射                           │
│  └─→ 执行生命周期                                                           │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 移除目录扫描的设计

**当前**: `DirectoryModuleProvider` 每次启动扫描目录
**简化后**: 移除目录扫描，模块通过显式安装

```
模块来源:
├── 系统模块 ─→ SystemModuleInstaller.EnsureBuiltInAsync() (首次启动)
├── 预装模块 ─→ 同上，硬编码在应用代码中
└── 用户模块 ─→ CLI: modulus install xxx.modpkg
               UI: 模块管理界面安装
```

**SystemModuleInstaller 示例**:

```csharp
public class SystemModuleInstaller
{
    private static readonly string[] BuiltInModulePaths = new[]
    {
        "System/ModuleManager",
        "System/Settings",
        // 预装模块
        "Modules/EchoPlugin",
        "Modules/SimpleNotes"
    };

    public async Task EnsureBuiltInAsync(string hostType, CancellationToken ct = default)
    {
        foreach (var relativePath in BuiltInModulePaths)
        {
            var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            if (!Directory.Exists(fullPath)) continue;

            var existing = await _moduleRepository.GetAsync(GetModuleId(fullPath), ct);
            if (existing == null || NeedsUpdate(existing, fullPath))
            {
                await _installer.InstallFromPathAsync(fullPath, isSystem: true, hostType, ct);
            }
        }
    }
}
```

**优势**:
- 启动更快 (无目录扫描 IO)
- 模块来源明确可控
- 与 VS Extension 行为一致

### 关键设计变更

#### 1. 菜单声明通过入口类型属性（不从 manifest 读取）

菜单必须通过 host-specific 模块入口类型的菜单属性声明；安装/更新时使用 metadata-only 解析并投影到数据库。

示例（Avalonia）：
```csharp
[AvaloniaMenu("echo", "Echo Tool", typeof(EchoViewModel), Icon = IconKind.Terminal, Order = 20)]
public sealed class EchoPluginAvaloniaModule : AvaloniaModuleBase { }
```

#### 2. 验证结果持久化

```sql
-- ModuleEntity 字段扩展
ALTER TABLE Modules ADD COLUMN ManifestHash TEXT;      -- manifest 文件 SHA256
ALTER TABLE Modules ADD COLUMN ValidatedAt DATETIME;  -- 验证时间戳
```

**加载时逻辑**:
```csharp
// 简化: 信任数据库状态，不重复验证
if (module.State != ModuleState.Ready)
{
    _logger.LogWarning("Skipping module {Id}: State={State}", module.Id, module.State);
    return null;
}

// 可选: 检测 manifest 变更 (开发模式)
if (isDevelopmentMode)
{
    var currentHash = ComputeHash(manifestPath);
    if (currentHash != module.ManifestHash)
    {
        // 重新验证并更新数据库
        await RevalidateAsync(module, manifestPath);
    }
}
```

#### 3. 分离关注点

| 组件 | 职责 | 变更 |
|------|------|------|
| `ManifestReader` | 解析 XML | JSON → XML |
| `ManifestValidator` | 验证 manifest | 只在安装时调用 |
| `ModuleInstallerService` | 安装模块 | 不再加载程序集 |
| `ModuleLoader` | 加载运行模块 | 不再验证，信任 DB |
| `DirectoryModuleProvider` | 发现模块 | 文件名变更 |

---

## 扩展完整工作流程

### 阶段 1: 开发阶段

#### 1.1 项目结构

```
MyExtension/
├── MyExtension.Core/                    # 核心逻辑 (host-agnostic)
│   ├── MyExtension.Core.csproj
│   └── MyExtensionModule.cs             # 入口点 (继承 ModulusPackage)
├── MyExtension.UI.Avalonia/             # Avalonia UI
│   ├── MyExtension.UI.Avalonia.csproj
│   └── MyExtensionAvaloniaModule.cs     # UI 入口点
├── MyExtension.UI.Blazor/               # Blazor UI
│   ├── MyExtension.UI.Blazor.csproj
│   └── MyExtensionBlazorModule.cs       # UI 入口点
└── extension.vsixmanifest               # 清单文件
```

#### 1.2 入口点类型定义 (渐进式迁移)

**策略**: 新增 `ModulusPackage` 基类，标记 `ModulusComponent` 为过时，实现向后兼容。

```csharp
namespace Modulus.Sdk;

/// <summary>
/// Base class for extension entry points. Similar to VS VsPackage.
/// The runtime discovers types inheriting from this class and uses them as entry points.
/// 
/// This is the recommended base class for new extensions.
/// </summary>
public abstract class ModulusPackage : ModulusComponent
{
    // Inherits all lifecycle methods from ModulusComponent
    // No additional members needed - this is primarily a naming/semantic change
}

/// <summary>
/// Legacy base class for module components.
/// </summary>
[Obsolete("Use ModulusPackage instead. ModulusComponent will be removed in v2.0.")]
public abstract class ModulusComponent : IModule
{
    public virtual void PreConfigureServices(IModuleLifecycleContext context) { }
    public virtual void ConfigureServices(IModuleLifecycleContext context) { }
    public virtual void PostConfigureServices(IModuleLifecycleContext context) { }
    public virtual Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken ct) 
        => Task.CompletedTask;
    public virtual Task OnApplicationShutdownAsync(IModuleInitializationContext context, CancellationToken ct) 
        => Task.CompletedTask;
}
```

**向后兼容说明**:
- 现有继承 `ModulusComponent` 的代码**继续工作**
- 编译时会产生 `[Obsolete]` 警告，提示迁移
- 运行时同时扫描 `ModulusPackage` 和 `ModulusComponent` 子类
- v2.0 移除 `ModulusComponent`，届时需要迁移

**新扩展示例**:

```csharp
// MyExtension.Core/MyExtensionPackage.cs (新代码推荐)
public class MyExtensionPackage : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddSingleton<IMyService, MyService>();
    }
}

// MyExtension.UI.Avalonia/MyExtensionAvaloniaPackage.cs
[DependsOn(typeof(MyExtensionPackage))]
public class MyExtensionAvaloniaPackage : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddTransient<MyExtensionView>();
    }
}
```

**现有代码 (继续工作，但有警告)**:

```csharp
// 现有代码不需要立即修改，只是编译时有 warning
public class EchoPluginModule : ModulusComponent  // ⚠️ Warning: Use ModulusPackage
{
    // ... 继续工作
}
```

#### 1.3 extension.vsixmanifest 格式

```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011">
  <Metadata>
    <Identity Id="a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d" 
              Version="1.0.0" 
              Language="en-US" 
              Publisher="Your Name" />
    <DisplayName>My Extension</DisplayName>
    <Description>A sample Modulus extension.</Description>
    <Icon>Resources\icon.png</Icon>
    <Tags>sample, demo</Tags>
    <MoreInfo>https://github.com/you/myextension</MoreInfo>
    <License>Resources\LICENSE.txt</License>
  </Metadata>
  
  <Installation>
    <InstallationTarget Id="Modulus.Host.Blazor" Version="[1.0,)" />
    <InstallationTarget Id="Modulus.Host.Avalonia" Version="[1.0,)" />
  </Installation>
  
  <Dependencies>
    <Dependency Id="other-extension-guid" DisplayName="Other Extension" Version="[1.0,)" />
  </Dependencies>
  
  <Assets>
    <!-- Core Package Entry Point -->
    <Asset Type="Modulus.Package" Path="MyExtension.Core.dll" />
    
    <!-- Host-Specific UI Package Entry Points -->
    <Asset Type="Modulus.Package" Path="MyExtension.UI.Blazor.dll" 
           TargetHost="Modulus.Host.Blazor" />
    <Asset Type="Modulus.Package" Path="MyExtension.UI.Avalonia.dll" 
           TargetHost="Modulus.Host.Avalonia" />
    
    <!-- Additional Assemblies (auto-loaded as dependencies) -->
    <Asset Type="Modulus.Assembly" Path="MyExtension.Common.dll" />
    
    <!-- Resources -->
    <Asset Type="Modulus.Icon" Path="Resources\icon.png" />
    <Asset Type="Modulus.License" Path="Resources\LICENSE.txt" />
  </Assets>
</PackageManifest>
```

**Asset Type 定义**:

| Asset Type | 含义 | 处理时机 |
|------------|------|---------|
| `Modulus.Package` | 包含入口点的程序集 | 运行时加载，扫描 `ModulusPackage` |
| `Modulus.Assembly` | 普通依赖程序集 | 运行时加载，不扫描 |
| `Modulus.Icon` | 扩展图标 | 显示在模块列表 |
| `Modulus.License` | 许可证文件 | 显示在模块详情 |
| `Modulus.Readme` | README 文件 | 显示在模块详情 |

**Menu declaration**:
- Menus are declared via `[BlazorMenu]` / `[AvaloniaMenu]` on the host-specific module entry type.
- Installation/update projects menus to DB using metadata-only parsing.

---

### 阶段 2: 打包阶段

#### 2.1 包格式: `.modpkg`

`.modpkg` 是一个 ZIP 压缩包，内部结构:

```
MyExtension.modpkg (ZIP)
├── extension.vsixmanifest               # 清单文件 (必须)
├── MyExtension.Core.dll                 # Core 程序集
├── MyExtension.UI.Blazor.dll            # Blazor UI 程序集
├── MyExtension.UI.Avalonia.dll          # Avalonia UI 程序集
├── MyExtension.Common.dll               # 共享依赖
├── Resources/
│   ├── icon.png
│   └── LICENSE.txt
└── README.md                            # 可选
```

#### 2.2 开发时目录结构 (解压后)

开发/调试时，可以直接使用解压后的文件夹结构:

```
Modules/
└── MyExtension/                         # 模块目录名 = manifest Identity.Id
    ├── extension.vsixmanifest
    ├── MyExtension.Core.dll
    ├── MyExtension.UI.Blazor.dll
    ├── MyExtension.UI.Avalonia.dll
    └── Resources/
        └── icon.png
```

#### 2.3 与 VS Extension 的差异说明

**VS Extension 安装后会生成额外文件**:

```
VS Extension 安装后目录:
├── extension.vsixmanifest    # 原始清单
├── MyExtension.dll
├── catalog.json              ← VS 生成，扩展目录索引
└── manifest.json             ← VS 生成，Marketplace 元数据
```

**Modulus 不生成额外文件**:

| 文件 | VS Extension | Modulus | 原因 |
|------|-------------|---------|------|
| `catalog.json` | VS 安装时生成 | 不需要 | 使用数据库索引 |
| `manifest.json` | VS 安装时生成 | 不需要 | 元数据存入数据库 |

**Modulus 的设计选择**:

1. **单一真相来源**: `extension.vsixmanifest` 是唯一的清单文件
2. **数据库索引**: 模块元数据存储在 `ModuleEntity` 表，无需文件索引
3. **目录一致性**: 开发目录 = 安装目录，减少不一致导致的问题
4. **简化部署**: 直接复制文件夹即可安装，无需生成额外文件

---

### 阶段 3: 安装阶段

#### 3.1 安装目录结构

```
{AppData}/Modulus/
├── modulus.db                           # SQLite 数据库 (元数据索引)
├── logs/                                # 日志目录
└── Modules/                             # 用户安装的模块
    ├── MyExtension/
    │   ├── extension.vsixmanifest       # 唯一清单文件
    │   ├── MyExtension.Core.dll
    │   └── ...                          # (无额外生成文件)
    └── AnotherExtension/
        └── ...
```

**系统模块目录** (应用内置):

```
{AppInstallDir}/
├── Modulus.Host.Avalonia.exe            # 主程序
├── System/                              # 系统模块 (不可卸载)
│   ├── ModuleManager/
│   │   └── extension.vsixmanifest
│   └── Settings/
│       └── extension.vsixmanifest
└── Modules/                             # 预装模块
    └── ...
```

#### 3.2 元数据管理策略

**与 VS Extension 的差异**:

VS Extension 依赖生成的 `catalog.json` 和 `manifest.json` 来索引扩展。
Modulus 使用 **数据库** 作为元数据存储和索引，不生成额外文件。

```
┌─────────────────────────────────────────────────────────────────┐
│                     元数据存储对比                               │
├─────────────────────────────────────────────────────────────────┤
│  VS Extension                    │  Modulus                     │
│  ─────────────────────────────   │  ────────────────────────    │
│  extension.vsixmanifest (原始)   │  extension.vsixmanifest      │
│  catalog.json (索引)             │  ModuleEntity 表 (数据库)    │
│  manifest.json (Marketplace)     │  (无，暂不支持在线更新)      │
└─────────────────────────────────────────────────────────────────┘
```

**数据库 `ModuleEntity` 存储的字段**:

| 来源 (vsixmanifest) | 数据库字段 | 说明 |
|---------------------|-----------|------|
| `Identity/@Id` | `Id` | 主键 |
| `Identity/@Version` | `Version` | 版本号 |
| `Identity/@Publisher` | `Author` | 发布者 (字段名保持兼容) |
| `DisplayName` | `Name` | 显示名称 |
| `Description` | `Description` | 描述 |
| (文件路径) | `Path` | manifest 文件路径 |
| (运行时状态) | `State`, `IsEnabled` | 模块状态 |

**优势**:

1. **查询高效**: 数据库索引比文件扫描快
2. **状态管理**: 可存储运行时状态 (启用/禁用/错误)
3. **原子操作**: 数据库事务保证一致性
4. **无冗余**: 不产生与 manifest 重复的 JSON 文件

#### 3.3 安装流程

```
用户安装 .modpkg
      │
      ▼
┌─────────────────────────────────────────────────┐
│ 1. 验证包完整性 (签名/哈希)                      │
├─────────────────────────────────────────────────┤
│ 2. 解析 extension.vsixmanifest                  │
│    - 检查 Identity.Id 唯一性                    │
│    - 验证 Version 语义版本                      │
│    - 验证 InstallationTarget 主机兼容性         │
├─────────────────────────────────────────────────┤
│ 3. 检查依赖                                      │
│    - Dependencies 中的其他扩展是否已安装         │
│    - 版本范围是否满足                           │
├─────────────────────────────────────────────────┤
│ 4. 解压到安装目录 (原样复制，不生成额外文件)     │
│    {AppData}/Modulus/Modules/{Identity.Id}/     │
├─────────────────────────────────────────────────┤
│ 5. 写入数据库 (元数据索引)                       │
│    - INSERT/UPDATE ModuleEntity                 │
│    - State = Ready, IsEnabled = true            │
└─────────────────────────────────────────────────┘
```

---

### 阶段 4: 启动加载阶段

#### 4.1 加载流程

```
Host 应用启动
      │
      ▼
┌─────────────────────────────────────────────────┐
│ 1. 初始化 RuntimeContext                         │
│    - 设置 HostType (Modulus.Host.Blazor/Avalonia)│
├─────────────────────────────────────────────────┤
│ 2. 数据库迁移 & 完整性检查                       │
│    - 检查已安装模块的 manifest 文件是否存在      │
│    - 标记缺失文件的模块为 MissingFiles           │
├─────────────────────────────────────────────────┤
│ 3. 查询已启用模块                               │
│    SELECT * FROM Modules                        │
│    WHERE IsEnabled = true AND State = Ready     │
├─────────────────────────────────────────────────┤
│ 4. 构建依赖图并排序                             │
│    - manifest Dependencies                       │
│    - [DependsOn] 属性                           │
│    - 拓扑排序确定加载顺序                       │
├─────────────────────────────────────────────────┤
│ 5. 按序加载每个模块                             │
│    │                                            │
│    ├─ 5.1 解析 extension.vsixmanifest           │
│    │                                            │
│    ├─ 5.2 验证 InstallationTarget               │
│    │       当前 Host 是否在列表中?              │
│    │                                            │
│    ├─ 5.3 创建 ModuleLoadContext (ALC)          │
│    │                                            │
│    ├─ 5.4 加载 Assets                           │
│    │       ├─ Modulus.Package (无 TargetHost)   │
│    │       │   → 加载 DLL，扫描 ModulusPackage  │
│    │       ├─ Modulus.Package (有 TargetHost)   │
│    │       │   → 仅加载匹配当前 Host 的         │
│    │       └─ Modulus.Assembly                  │
│    │           → 加载 DLL，不扫描               │
│    │                                            │
│    ├─ 5.5 发现入口点类型                        │
│    │       扫描所有 ModulusPackage 子类         │
│    │       按 [DependsOn] 拓扑排序              │
│    │                                            │
│    ├─ 5.6 执行生命周期                          │
│    │       ├─ PreConfigureServices()            │
│    │       ├─ ConfigureServices()               │
│    │       └─ PostConfigureServices()           │
│    │                                            │
│    └─ 5.7 标记模块状态 = Loaded                 │
│                                                 │
├─────────────────────────────────────────────────┤
│ 6. Host 绑定服务                                │
│    BindHostServices(IServiceProvider)           │
├─────────────────────────────────────────────────┤
│ 7. 激活模块                                      │
│    ├─ OnApplicationInitializationAsync()        │
│    └─ 标记模块状态 = Active                     │
└─────────────────────────────────────────────────┘
```

#### 4.2 入口点发现规则 (向后兼容)

1. 扫描 `Modulus.Package` 类型的 Asset 中的程序集
2. 查找所有继承以下类型的非抽象类:
   - `ModulusPackage` (推荐，新代码)
   - `ModulusComponent` (兼容，现有代码)
   - 实现 `IModule` 接口的类型
3. 按 `[DependsOn]` 属性构建依赖图
4. 拓扑排序后按序实例化和初始化

**注意**: 入口点发现逻辑保持不变，只是推荐新代码使用 `ModulusPackage`。

---

### 阶段 5: 卸载阶段

```
用户卸载模块
      │
      ▼
┌─────────────────────────────────────────────────┐
│ 1. 检查是否为系统模块 (IsSystem = true)          │
│    → 如果是，拒绝卸载                           │
├─────────────────────────────────────────────────┤
│ 2. 检查依赖关系                                 │
│    → 是否有其他模块依赖此模块?                  │
│    → 如果有，提示用户先卸载依赖模块             │
├─────────────────────────────────────────────────┤
│ 3. 执行卸载流程                                 │
│    ├─ OnApplicationShutdownAsync()              │
│    ├─ 释放 ServiceProvider                      │
│    ├─ 卸载 AssemblyLoadContext                  │
│    └─ 从数据库删除记录                          │
├─────────────────────────────────────────────────┤
│ 4. 删除文件                                      │
│    rm -rf {AppData}/Modulus/Modules/{Id}/       │
└─────────────────────────────────────────────────┘
```

---

## Decisions

### 1. 为什么用 `Modulus.Package` 而不是 `Modulus.Assembly.Core`?

- **语义清晰**: `Package` 表示"包含入口点的单元"，与 VS 的 VsPackage 概念一致
- **自动发现**: 运行时只扫描 `Modulus.Package` 类型的 Asset
- **显式声明**: 开发者必须明确哪些程序集包含入口点

### 2. 为什么保留 `TargetHost` 属性?

- Modulus 支持多个 Host (Blazor/Avalonia)
- 需要区分哪些 Package 适用于哪个 Host
- `TargetHost` 为空表示 host-agnostic (Core 逻辑)

### 3. Host ID 映射

| 原 Modulus | 新 vsixmanifest |
|------------|-----------------|
| `BlazorApp` | `Modulus.Host.Blazor` |
| `AvaloniaApp` | `Modulus.Host.Avalonia` |

### 4. 使用 LINQ to XML 解析

- 现代 API，类型安全
- 不需要 XSD 代码生成
- 易于扩展和维护

### 5. 不生成额外的索引文件 (与 VS 不同)

VS Extension 安装后会生成 `catalog.json` 和 `manifest.json` 用于索引和 Marketplace 集成。

**Modulus 选择不生成这些文件**:

| 考虑因素 | 决策 |
|---------|------|
| 索引需求 | 使用数据库 (`ModuleEntity`) 而非文件索引 |
| 目录一致性 | 开发目录 = 安装目录，减少调试困惑 |
| 简化部署 | 复制文件夹即安装，无需额外生成步骤 |
| Marketplace | 暂不支持在线 Marketplace，无需 `manifest.json` |

**好处**:
- `extension.vsixmanifest` 是唯一清单来源，无数据不一致风险
- 数据库查询比文件扫描更高效
- 开发者调试时目录结构清晰

**未来扩展**:
- 如需 Marketplace 支持，可在数据库增加字段或生成缓存文件
- 当前保持简单，按需扩展

---

## Risks / Trade-offs

| 风险 | 缓解措施 |
|-----|---------|
| manifest 格式 Breaking change | 同时更新所有示例模块；提供迁移文档 |
| XML 解析性能不如 JSON | 性能差异可忽略；manifest 文件小且仅启动时解析 |
| `ModulusComponent` 过时警告扰乱用户 | 清晰的迁移文档；v2.0 前充足的迁移时间 |
| 两个基类造成概念混乱 | 文档明确推荐 `ModulusPackage`；过渡期后移除 `ModulusComponent` |

---

## Migration Plan

1. 实现新的 XML 解析和数据模型
2. 添加 `ModulusPackage` 基类
3. 更新入口点发现逻辑
4. 更新所有验证逻辑
5. 迁移现有 3 个模块
6. 更新 Host ID 常量

---

## Example: Complete vsixmanifest

以下是 EchoPlugin 模块迁移后的完整示例:

```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011">
  <Metadata>
    <Identity Id="a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d" 
              Version="1.0.0" 
              Language="en-US" 
              Publisher="Modulus Team" />
    <DisplayName>Echo Plugin</DisplayName>
    <Description>A simple echo plugin to demonstrate SDK.</Description>
    <Tags>demo, echo, sample</Tags>
  </Metadata>
  
  <Installation>
    <InstallationTarget Id="Modulus.Host.Blazor" Version="[1.0,)" />
    <InstallationTarget Id="Modulus.Host.Avalonia" Version="[1.0,)" />
  </Installation>
  
  <Dependencies>
    <!-- No dependencies -->
  </Dependencies>
  
  <Assets>
    <!-- Core Package (host-agnostic) -->
    <Asset Type="Modulus.Package" Path="EchoPlugin.Core.dll" />
    
    <!-- Host-Specific UI Packages -->
    <Asset Type="Modulus.Package" Path="EchoPlugin.UI.Blazor.dll" 
           TargetHost="Modulus.Host.Blazor" />
    <Asset Type="Modulus.Package" Path="EchoPlugin.UI.Avalonia.dll" 
           TargetHost="Modulus.Host.Avalonia" />
    
    <!-- Menus are declared via [AvaloniaMenu]/[BlazorMenu] on host-specific entry types. -->
  </Assets>
</PackageManifest>
```

**对比: 迁移前后的菜单声明**

迁移前 (程序集特性):
```csharp
// EchoPlugin.UI.Avalonia/EchoPluginAvaloniaModule.cs
[AvaloniaMenu("Echo Tool", typeof(EchoViewModel), Icon = IconKind.Terminal, Order = 20)]
public class EchoPluginAvaloniaModule : AvaloniaModuleBase { }
```

迁移后 (入口类型菜单属性):
```csharp
// EchoPlugin.UI.Avalonia/EchoPluginAvaloniaModule.cs
[AvaloniaMenu("echo", "Echo Tool", typeof(EchoViewModel), Icon = IconKind.Terminal, Order = 20)]
public sealed class EchoPluginAvaloniaModule : AvaloniaModuleBase { }

// EchoPlugin.UI.Blazor/EchoPluginBlazorModule.cs
[BlazorMenu("echo", "Echo Tool", "/echo", Icon = IconKind.Terminal, Order = 20)]
public sealed class EchoPluginBlazorModule : ModulusPackage { }
```

---

## Open Questions

1. ~~是否需要支持 vsixmanifest 的签名功能？~~ → 暂不实现，作为后续工作
2. ~~`TargetHost` 属性是 Modulus 扩展，是否需要单独的 namespace？~~ → 使用 `Modulus` 前缀约定即可
