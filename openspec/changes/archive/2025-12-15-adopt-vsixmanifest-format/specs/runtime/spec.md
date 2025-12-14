## MODIFIED Requirements

### Requirement: Manifest validation for host, deps, and integrity
模块清单 (extension.vsixmanifest) MUST 使用 VS Extension vsixmanifest 2.0 XML 格式，并验证必填字段、主机兼容性、依赖语义和完整性。

#### Scenario: Valid vsixmanifest structure required
- **WHEN** 加载模块时解析 `extension.vsixmanifest` 文件
- **THEN** 验证 XML 根元素为 `PackageManifest`
- **AND** 验证 `Version` 属性为 `2.0.0`
- **AND** 验证 `xmlns` 为 `http://schemas.microsoft.com/developer/vsx-schema/2011`

#### Scenario: Required Identity fields validated
- **WHEN** 解析 `Metadata/Identity` 元素
- **THEN** 验证 `Id` 属性存在且非空
- **AND** 验证 `Version` 属性为有效的 SemVer 格式
- **AND** 验证 `Publisher` 属性存在
- **AND** 失败时返回诊断信息指明缺失字段

#### Scenario: Unsupported host is rejected
- **WHEN** `Installation/InstallationTarget` 不包含当前主机 ID
- **THEN** 加载失败，诊断信息说明主机不匹配

#### Scenario: Dependency or version constraint failure blocks load
- **WHEN** 声明的依赖缺失或版本不满足 SemVer range
- **THEN** 加载失败，诊断信息指明缺失/无效的依赖

#### Scenario: Missing Modulus.Package Asset blocks load
- **WHEN** `Assets` 中不包含 `Modulus.Package` 类型的 Asset
- **THEN** 验证失败，诊断信息指明缺少入口点程序集

#### Scenario: Missing host UI Package blocks load
- **WHEN** 当前 Host 需要 UI 但 `Assets` 中不包含匹配 `TargetHost` 的 `Modulus.Package`
- **THEN** 验证失败，诊断信息指明主机和缺失的 UI Package

#### Scenario: Integrity checks enforced when provided
- **WHEN** manifest 或程序集的哈希/签名存在时
- **THEN** 加载器验证它们并在不匹配时拒绝模块

## ADDED Requirements

### Requirement: ModulusPackage entry point discovery
运行时 MUST 通过扫描 `Modulus.Package` 类型 Asset 中的入口点类来发现模块，同时保持对 `ModulusComponent` 的向后兼容。

#### Scenario: Package entry point discovered from Asset
- **WHEN** 加载 `Modulus.Package` 类型的 Asset 程序集
- **THEN** 扫描程序集中所有继承 `ModulusPackage` 或 `ModulusComponent` 的非抽象类型
- **AND** 按 `[DependsOn]` 属性构建依赖图
- **AND** 拓扑排序后按序实例化

#### Scenario: TargetHost filters Package loading
- **WHEN** Asset 声明了 `TargetHost` 属性
- **AND** `TargetHost` 不匹配当前 Host
- **THEN** 跳过该 Asset 的加载

#### Scenario: Assembly Asset loaded without entry point scan
- **WHEN** 加载 `Modulus.Assembly` 类型的 Asset
- **THEN** 仅加载程序集到 ALC
- **AND** 不扫描入口点类型

#### Scenario: Backward compatible with ModulusComponent
- **WHEN** 程序集中存在 `ModulusComponent` 子类 (未继承 `ModulusPackage`)
- **THEN** 仍然被发现和加载 (向后兼容)
- **AND** 编译时产生 `[Obsolete]` 警告提示迁移

### Requirement: vsixmanifest Asset Type conventions
模块程序集 MUST 通过 vsixmanifest `Assets` 元素声明，使用 Modulus 定义的 Asset Type 约定。

#### Scenario: Modulus.Package Asset declares entry point
- **WHEN** 模块包含入口点程序集
- **THEN** 使用 `<Asset Type="Modulus.Package" Path="*.dll" />` 声明
- **AND** 加载器扫描该程序集中的 `ModulusPackage` 子类

#### Scenario: Host-specific Package with TargetHost
- **WHEN** 模块包含 host-specific UI 入口点
- **THEN** 使用 `<Asset Type="Modulus.Package" Path="*.dll" TargetHost="Modulus.Host.*" />` 声明
- **AND** 加载器仅加载匹配当前主机的 Package

#### Scenario: Dependency Assembly without entry point
- **WHEN** 模块包含依赖程序集但无入口点
- **THEN** 使用 `<Asset Type="Modulus.Assembly" Path="*.dll" />` 声明
- **AND** 加载器加载但不扫描入口点

#### Scenario: Icon asset for module display
- **WHEN** 模块提供图标文件
- **THEN** 使用 `<Asset Type="Modulus.Icon" Path="icon.png" />` 声明
- **AND** 图标用于模块列表和详情页显示

### Requirement: InstallationTarget host mapping
模块支持的主机 MUST 通过 `Installation/InstallationTarget` 元素声明，使用 Modulus 定义的 Host ID。

#### Scenario: Blazor host target
- **WHEN** 模块支持 Blazor 主机
- **THEN** 声明 `<InstallationTarget Id="Modulus.Host.Blazor" Version="[1.0,)" />`

#### Scenario: Avalonia host target
- **WHEN** 模块支持 Avalonia 主机
- **THEN** 声明 `<InstallationTarget Id="Modulus.Host.Avalonia" Version="[1.0,)" />`

#### Scenario: Version range validation
- **WHEN** `InstallationTarget/@Version` 包含版本范围
- **THEN** 使用 NuGet SemVer range 语法验证 (如 `[1.0,)`, `[1.0,2.0)`)

### Requirement: Extension lifecycle with ModulusPackage
`ModulusPackage` 入口点 MUST 遵循完整的模块生命周期，包括服务配置和初始化阶段。

#### Scenario: Package lifecycle execution order
- **WHEN** 模块包含多个 `ModulusPackage` 入口点
- **THEN** 按 `[DependsOn]` 拓扑顺序执行:
  1. `PreConfigureServices()`
  2. `ConfigureServices()`
  3. `PostConfigureServices()`
- **AND** Host 绑定后执行 `OnApplicationInitializationAsync()`

#### Scenario: Package shutdown in reverse order
- **WHEN** 模块被卸载
- **THEN** 按拓扑逆序执行 `OnApplicationShutdownAsync()`
- **AND** 释放模块 ServiceProvider
- **AND** 卸载 AssemblyLoadContext

### Requirement: Menu declaration in manifest Assets
模块菜单 MUST 通过 vsixmanifest `Assets` 元素中的 `Modulus.Menu` 类型声明，安装时从 XML 解析而非程序集扫描。

#### Scenario: Menu extracted from manifest during installation
- **WHEN** 安装模块时解析 `extension.vsixmanifest`
- **THEN** 从 `Assets` 中提取所有 `Type="Modulus.Menu"` 的元素
- **AND** 将菜单信息写入数据库 `MenuEntity`
- **AND** 不加载程序集到临时 ALC

#### Scenario: Menu Asset with TargetHost filter
- **WHEN** `Modulus.Menu` Asset 声明了 `TargetHost` 属性
- **THEN** 仅在匹配的 Host 上显示该菜单
- **AND** 其他 Host 忽略该菜单声明

#### Scenario: Menu Asset attributes
- **WHEN** 声明 `Modulus.Menu` Asset
- **THEN** MUST 包含 `Id`, `DisplayName`, `Route` 属性
- **AND** MAY 包含 `Icon`, `Order`, `Location`, `TargetHost` 属性

### Requirement: Installation without assembly loading
模块安装 MUST 仅解析 manifest，不加载程序集，以简化安装流程和减少资源开销。

#### Scenario: Install parses manifest only
- **WHEN** 安装模块
- **THEN** 读取并验证 `extension.vsixmanifest`
- **AND** 从 manifest 提取模块元数据和菜单信息
- **AND** 写入数据库
- **AND** 不创建 AssemblyLoadContext
- **AND** 不加载任何程序集

#### Scenario: Manifest hash stored for change detection
- **WHEN** 安装或更新模块
- **THEN** 计算 manifest 文件的 SHA256 hash
- **AND** 将 hash 存入 `ModuleEntity.ManifestHash`
- **AND** 记录验证时间到 `ModuleEntity.ValidatedAt`

### Requirement: Loading trusts database validation state
模块加载 MUST 信任数据库中的验证状态，不重复执行 manifest 验证，以优化启动性能。

#### Scenario: Load skips validation when state is Ready
- **WHEN** 加载已安装模块
- **AND** `ModuleEntity.State == Ready`
- **THEN** 直接加载程序集，不重新验证 manifest

#### Scenario: Load rejects module when state is not Ready
- **WHEN** 加载模块
- **AND** `ModuleEntity.State != Ready`
- **THEN** 跳过加载并记录警告日志

#### Scenario: Optional hash check in development mode
- **WHEN** 处于开发模式
- **AND** manifest 文件的 hash 与数据库记录不匹配
- **THEN** 重新验证 manifest 并更新数据库
- **AND** 如果验证失败，更新 State 为 Incompatible

### Requirement: Explicit module installation without directory scanning
模块 MUST 通过显式安装加入系统，运行时不扫描目录自动发现模块。

#### Scenario: Built-in modules installed on first launch
- **WHEN** 应用首次启动
- **AND** 数据库中不存在内置模块
- **THEN** `SystemModuleInstaller` 从硬编码路径列表安装内置模块
- **AND** 后续启动跳过已安装的模块

#### Scenario: User modules installed via CLI or UI
- **WHEN** 用户希望安装新模块
- **THEN** 通过 CLI 命令 (如 `modulus install xxx.modpkg`) 或 UI 操作安装
- **AND** 不支持将模块放入目录自动发现

#### Scenario: Startup loads from database only
- **WHEN** 应用启动
- **THEN** 直接从数据库查询已启用模块
- **AND** 不扫描任何目录
- **AND** 不调用 `IModuleProvider` (已移除)
