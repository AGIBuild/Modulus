## Phase 1: SDK 和数据模型

### 1.1 SDK 入口点类型 (渐进式迁移)
- [ ] 1.1.1 创建 `ModulusPackage` 基类 (继承 `ModulusComponent`)
- [ ] 1.1.2 标记 `ModulusComponent` 为 `[Obsolete]`
- [ ] 1.1.3 更新 `AvaloniaModuleBase` 等 UI 基类的继承关系

### 1.2 Manifest 数据模型
- [ ] 1.2.1 创建 `VsixManifest` 根类
- [ ] 1.2.2 创建 `ManifestIdentity` 类 (Id, Version, Publisher, Language)
- [ ] 1.2.3 创建 `ManifestMetadata` 类 (DisplayName, Description, Icon, Tags 等)
- [ ] 1.2.4 创建 `InstallationTarget` 类 (Id, Version range)
- [ ] 1.2.5 创建 `ManifestDependency` 类 (Id, DisplayName, Version)
- [ ] 1.2.6 创建 `ManifestAsset` 类 (Type, Path, TargetHost, 菜单属性)

### 1.3 数据库模型更新
- [ ] 1.3.1 更新 `ModuleEntity` 字段 (Author → Publisher)
- [ ] 1.3.2 新增 `ManifestHash` 字段 (验证缓存)
- [ ] 1.3.3 新增 `ValidatedAt` 字段 (验证时间戳)
- [ ] 1.3.4 创建数据库迁移脚本

---

## Phase 2: XML 解析和验证

### 2.1 ManifestReader 重写
- [ ] 2.1.1 实现 LINQ to XML 解析 `extension.vsixmanifest`
- [ ] 2.1.2 解析 `Metadata/Identity` 元素
- [ ] 2.1.3 解析 `Installation/InstallationTarget` 列表
- [ ] 2.1.4 解析 `Dependencies/Dependency` 列表
- [ ] 2.1.5 解析 `Assets/Asset` 列表 (含 `Modulus.Menu` 类型)

### 2.2 ManifestValidator 重写
- [ ] 2.2.1 验证 XML schema 基本结构 (PackageManifest, Version, xmlns)
- [ ] 2.2.2 验证 Identity 必填字段 (Id, Version, Publisher)
- [ ] 2.2.3 验证 InstallationTarget host 兼容性
- [ ] 2.2.4 验证 Dependency version range (NuGet SemVer)
- [ ] 2.2.5 验证 Assets 完整性 (至少一个 Modulus.Package)
- [ ] 2.2.6 实现 manifest hash 计算

---

## Phase 3: 安装流程简化

### 3.1 ModuleInstallerService 重构
- [ ] 3.1.1 移除临时 ALC 创建和程序集加载逻辑
- [ ] 3.1.2 从 manifest Assets 解析菜单信息 (`Modulus.Menu`)
- [ ] 3.1.3 将 manifest hash 和验证时间写入数据库
- [ ] 3.1.4 更新 manifest 文件名引用 (`manifest.json` → `extension.vsixmanifest`)

### 3.2 菜单元数据提取
- [ ] 3.2.1 实现从 `Modulus.Menu` Asset 提取菜单元数据
- [ ] 3.2.2 移除 `ModuleMetadataScanner` 的程序集扫描逻辑
- [ ] 3.2.3 保留程序集扫描作为后备 (渐进迁移期间)

### 3.3 移除目录扫描，重构为显式安装
- [ ] 3.3.1 删除 `DirectoryModuleProvider.cs`
- [ ] 3.3.2 删除 `IModuleProvider.cs` 接口
- [ ] 3.3.3 重构 `SystemModuleSeeder` → `SystemModuleInstaller`
- [ ] 3.3.4 实现 `EnsureBuiltInAsync()` 方法 (硬编码内置模块路径)
- [ ] 3.3.5 更新 `ModulusApplicationFactory` 移除 `moduleProviders` 参数
- [ ] 3.3.6 更新 Host 启动代码调用 `SystemModuleInstaller`

---

## Phase 4: 加载流程优化

### 4.1 ModuleLoader 重构
- [ ] 4.1.1 移除重复的 manifest 验证逻辑
- [ ] 4.1.2 从数据库获取模块状态，信任验证结果
- [ ] 4.1.3 实现可选的 manifest hash 变更检测 (开发模式)
- [ ] 4.1.4 实现 Asset Type 过滤 (`Modulus.Package` vs `Modulus.Assembly`)
- [ ] 4.1.5 实现 `TargetHost` 过滤

### 4.2 入口点发现更新
- [ ] 4.2.1 更新入口点扫描：同时支持 `ModulusPackage` 和 `ModulusComponent`
- [ ] 4.2.2 更新依赖图构建逻辑

### 4.3 启动流程简化
- [ ] 4.3.1 更新 `ModulusApplicationFactory` 直接从数据库加载模块
- [ ] 4.3.2 移除启动时的目录扫描逻辑

---

## Phase 5: 常量和 Host ID

### 5.1 Host ID 映射
- [ ] 5.1.1 定义 `ModulusHostIds` 常量类
- [ ] 5.1.2 `BlazorApp` → `Modulus.Host.Blazor`
- [ ] 5.1.3 `AvaloniaApp` → `Modulus.Host.Avalonia`
- [ ] 5.1.4 更新 `RuntimeContext.HostType` 赋值逻辑

### 5.2 Asset Type 常量
- [ ] 5.2.1 定义 `ModulusAssetTypes` 常量类
- [ ] 5.2.2 `Modulus.Package`, `Modulus.Assembly`, `Modulus.Menu`, `Modulus.Icon` 等

---

## Phase 6: 现有模块迁移

### 6.1 Manifest 文件迁移
- [ ] 6.1.1 迁移 `EchoPlugin/manifest.json` → `extension.vsixmanifest`
- [ ] 6.1.2 迁移 `SimpleNotes/manifest.json` → `extension.vsixmanifest`
- [ ] 6.1.3 迁移 `ComponentsDemo/manifest.json` → `extension.vsixmanifest`

### 6.2 菜单声明迁移
- [ ] 6.2.1 将 `[AvaloniaMenu]` 特性信息转移到 manifest Assets
- [ ] 6.2.2 将 `[BlazorMenu]` 特性信息转移到 manifest Assets
- [ ] 6.2.3 保留原有特性作为后备 (渐进迁移)

> **注意**: 入口点类 (`XxxModule : ModulusComponent`) 不需要立即迁移。
> 现有代码继续工作，只会产生 `[Obsolete]` 编译警告。

---

## Phase 7: 文档和配置更新

### 7.1 项目文档
- [ ] 7.1.1 更新 `openspec/project.md` 中的 Domain Context
- [ ] 7.1.2 更新模块开发指南

### 7.2 清理
- [ ] 7.2.1 移除 `ModuleManifest.cs` (JSON 模型)
- [ ] 7.2.2 移除无用的 manifest 字段 (`manifestVersion`, `entryComponent`, `sharedAssemblyHints`)
