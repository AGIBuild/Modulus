# runtime Specification

## Purpose
运行时规范，定义模块加载、生命周期管理、清单验证和依赖解析。本规范明确隔离域、共享程序集解析、安装/卸载流程与诊断输出要求，以保证运行时行为一致、可观测且可维护。
## Requirements
### Requirement: Hybrid Module/Component Runtime
The runtime SHALL load modules (ModulusModule) as deployable units and resolve components (ModulusComponent) as code units with dependency ordering.

#### Scenario: Module load succeeds
- **WHEN** a module is enabled and its manifest exists
- **THEN** the runtime loads its assemblies in an isolated ALC (by default)
- **AND** discovers all `ModulusPackage` entry types
- **AND** builds a dependency graph from `[DependsOn]` (including cross-module dependencies)
- **AND** initializes components in topological order (`ConfigureServices` then `OnApplicationInitialization`).

#### Scenario: Missing files are flagged
- **WHEN** a module manifest file is missing at startup
- **THEN** the module state is set to `MissingFiles`
- **AND** the module is skipped from the load list.

### Requirement: Menu Projection
Menu entries SHALL be projected to the database at install/update time and read from the database at render time.

#### Scenario: Install or update module projects menus to database
- **WHEN** 模块被安装或更新
- **THEN** 安装器解析模块 host-specific 入口类型上的菜单属性（Blazor: `[BlazorMenu]`，Avalonia: `[AvaloniaMenu]`）
- **AND** 将菜单写入数据库表 `Menus`（`ReplaceModuleMenusAsync` 覆盖该模块现有菜单）
- **AND** 写入的菜单 ID 采用 `{ModuleId}.{HostType}.{Key}.{Index}` 规则（允许多条同 Key 菜单用于排错）

#### Scenario: Render menus reads from database only
- **WHEN** Shell 渲染导航菜单
- **THEN** 从数据库读取 `Menus` 与 `Modules`（仅 `IsEnabled=true` 且状态可加载）
- **AND** 注册到 `IMenuRegistry`
- **AND** 渲染过程不进行任何 DLL 动态解析（不反射、不 metadata 扫描、不读取菜单属性）

### Requirement: Detail Content Fallback
Module detail pages SHALL prefer README content and fall back to manifest description.

#### Scenario: README available
- **WHEN** `README.md` exists in the module folder
- **THEN** its Markdown content is used for the detail view.

#### Scenario: README missing
- **WHEN** no `README.md` exists
- **AND** `manifest.description` is present
- **THEN** the manifest description is shown.

#### Scenario: No content available
- **WHEN** neither README nor manifest description exists
- **THEN** show "No description provided."

### Requirement: Module State Management
Module state SHALL be persisted and enforced at startup.

#### Scenario: Startup integrity
- **WHEN** the application starts
- **THEN** the system migrates the DB
- **AND** seeds system modules (install/upgrade if missing or outdated)
- **AND** marks missing manifests as `MissingFiles`
- **AND** only modules with `State=Ready` and `IsEnabled=true` are loaded.

### Requirement: Standardized module lifecycle states and lazy activation
Runtime modules SHALL follow a Loaded/Active/Error state machine, gate initialization on host binding, and lazily activate detail views.

#### Scenario: Host not bound keeps module Loaded
- **WHEN** a module is discovered/installed and the host is not yet bound
- **THEN** the runtime loads manifest/menu metadata only
- **AND** marks the module state as `Loaded`
- **AND** defers module initialization, services, and detail view creation.

#### Scenario: Host binding activates eligible modules
- **WHEN** the host binds
- **THEN** the runtime initializes all Ready+enabled modules using the lifecycle pipeline
- **AND** updates their state to `Active` upon success
- **AND** records diagnostics for any module that fails to activate.

#### Scenario: Lazy detail load on first navigation
- **WHEN** a user first navigates to a module's detail view
- **THEN** the runtime asynchronously loads and renders the module detail/page
- **AND** supports cancellation/timeout for the detail load
- **AND** surfaces errors and sets the module state to `Error` if detail load fails.

#### Scenario: Initialization failure transitions to Error
- **WHEN** module initialization or detail load fails
- **THEN** the module state becomes `Error`
- **AND** diagnostics are recorded for recovery
- **AND** the module is not reused until a retry/repair is attempted.

### Requirement: Unified unload and cleanup pipeline
Runtime modules MUST expose and execute a standardized cleanup path on unload/disable to remove registrations and release resources.

#### Scenario: Unload cleans registrations and resources
- **WHEN** a non-system module is unloaded or disabled
- **THEN** the runtime invokes module-provided deregistration hooks for menus/navigation/messages/subscriptions
- **AND** disposes the module-scoped ServiceProvider, caches, and other scoped resources
- **AND** removes the module's UI/navigation projections from the host
- **AND** unloads the module AssemblyLoadContext and returns the module to the `Loaded` state (metadata only).

#### Scenario: System modules protected from unload
- **WHEN** an unload is requested for a system module
- **THEN** the runtime rejects the request with a clear diagnostic.

### Requirement: Runtime module lifecycle and cleanup
Runtime-loaded modules MUST execute full lifecycle with module-scoped DI and clean teardown.

#### Scenario: Load executes lifecycle with module services
- **WHEN** a module is loaded at runtime with a valid manifest and supported host
- **THEN** the loader builds a module ServiceCollection/ServiceProvider
- **AND** instantiates all `IModule` types in the package
- **AND** runs Pre/Configure/PostConfigureServices followed by OnApplicationInitializationAsync
- **AND** registers module UI/menu contributions and marks the module active

#### Scenario: Unload calls shutdown and removes registrations
- **WHEN** a loaded module is unloaded
- **THEN** OnApplicationShutdownAsync is invoked in reverse order
- **AND** module menus/navigation/views/services registered during load are deregistered
- **AND** the module ServiceProvider is disposed and its AssemblyLoadContext is unloaded

#### Scenario: System modules protected from unload
- **WHEN** an unload is requested for a system module
- **THEN** the operation is rejected with a clear error

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

### Requirement: Unified dependency graph with topo ordering
Module initialization order MUST use a unified dependency graph from manifest Dependencies and DependsOn attributes, with validation.

#### Scenario: Missing or cyclic dependency fails fast
- **WHEN** the graph contains a missing module or a cycle
- **THEN** loading/initialization is blocked and the error identifies the offending modules

#### Scenario: Modules initialize in dependency order
- **WHEN** modules have dependencies
- **THEN** initialization runs in topological order honoring both manifest and DependsOn links

### Requirement: Shared assembly resolution from domain metadata
Shared assembly allowlist MUST come from assembly-domain metadata/config, with diagnostics for mismatches.

#### Scenario: Shared assemblies resolved via domain metadata
- **WHEN** a module requests an assembly marked Shared by assembly-domain metadata
- **THEN** it is resolved from the host shared context instead of a private copy

#### Scenario: Misdeclared shared/module assembly surfaces diagnostics
- **WHEN** a Module-domain assembly is requested from the shared list or vice versa
- **THEN** the loader emits a diagnostic to help correct the domain assignment

### Requirement: Install-time host-aware manifest validation
Module installation SHALL validate host compatibility, UI assemblies for the target host, and dependency version ranges before a module can be enabled or loaded.

#### Scenario: Unsupported host blocks install
- **WHEN** the installer runs with `hostType` X
- **AND** `supportedHosts` does not include X
- **THEN** validation fails with a diagnostic explaining the mismatch
- **AND** the module is not marked Ready or enabled.

#### Scenario: Missing host UI assemblies blocks install
- **WHEN** the installer runs with `hostType` X
- **AND** `uiAssemblies` is missing or empty for X
- **THEN** validation fails with a diagnostic naming the host and missing UI assemblies
- **AND** the module remains disabled/not Ready.

#### Scenario: Invalid dependency ranges are rejected
- **WHEN** the manifest lists dependencies with an invalid semantic version range
- **THEN** validation fails with a diagnostic citing the offending dependency id and range
- **AND** the module is not installed as Ready.

#### Scenario: Valid manifest becomes installable
- **WHEN** supportedHosts includes the current host
- **AND** uiAssemblies provides entries for that host
- **AND** all dependency ranges parse successfully
- **THEN** installation records the module as Ready
- **AND** the module may be enabled/loaded.

### Requirement: Host version registration
Host SHALL 在启动时注册版本信息到 RuntimeContext。

#### Scenario: Avalonia Host registers version
- **WHEN** Avalonia Host 启动
- **THEN** `RuntimeContext.HostVersion` 设置为 Host 程序集版本
- **AND** 版本格式为 SemVer（如 `1.0.0`）

#### Scenario: Blazor Host registers version
- **WHEN** Blazor Host 启动
- **THEN** `RuntimeContext.HostVersion` 设置为 Host 程序集版本

### Requirement: InstallationTarget version validation
模块加载时 SHALL 验证 InstallationTarget 声明的版本范围与当前 Host 版本兼容。

#### Scenario: Version range satisfied allows load
- **WHEN** 模块声明 `InstallationTarget Id="Modulus.Host.Avalonia" Version="[1.0,2.0)"`
- **AND** Host 版本为 `1.5.0`
- **THEN** 版本验证通过，模块加载继续

#### Scenario: Version range not satisfied blocks load
- **WHEN** 模块声明 `InstallationTarget Version="[2.0,)"`
- **AND** Host 版本为 `1.5.0`
- **THEN** 验证失败，输出诊断信息
- **AND** 模块不被加载

#### Scenario: Missing version range skips validation
- **WHEN** 模块 `InstallationTarget` 未声明 `Version` 属性
- **THEN** 跳过版本验证，仅验证 Host Id 匹配

### Requirement: ModulusPackage entry point discovery
运行时 MUST 通过扫描 `Modulus.Package` 类型 Asset 中的入口点类来发现模块，并且仅支持 `ModulusPackage` 作为入口模型（不提供旧入口兼容）。

#### Scenario: Package entry point discovered from Asset
- **WHEN** 加载 `Modulus.Package` 类型的 Asset 程序集
- **THEN** 扫描程序集内所有继承 `ModulusPackage` 的非抽象类型
- **AND** 按 `[DependsOn]` 构建依赖图并拓扑排序
- **AND** 依序执行生命周期（Pre/Configure/PostConfigureServices，Host 绑定后执行 OnApplicationInitializationAsync）

#### Scenario: TargetHost filters Package loading
- **WHEN** Asset 声明了 `TargetHost`
- **AND** `TargetHost` 不匹配当前 Host
- **THEN** 跳过该 Asset 的加载与入口点扫描

#### Scenario: Legacy entry point is not supported
- **WHEN** 程序集中不存在 `ModulusPackage` 子类（仅存在旧入口模型）
- **THEN** 模块加载失败
- **AND** 诊断信息提示模块需要迁移到 `ModulusPackage`

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

### Requirement: Menu declaration in module entry attributes
模块菜单 MUST 通过 host-specific 模块入口类型上的菜单属性声明，安装/更新时从程序集元数据解析，而不是从 vsixmanifest Assets 读取。

#### Scenario: Menu extracted from module entry attributes during installation
- **WHEN** 安装或更新模块
- **THEN** 基于当前 Host 选择对应 UI 程序集（vsixmanifest `Assets` 中匹配 `TargetHost` 的 `Modulus.Package`）
- **AND** 在不执行模块代码的前提下解析入口类型菜单属性
- **AND** 将菜单写入数据库 `Menus`

#### Scenario: Menu attributes are host-specific
- **WHEN** 当前 Host 为 `Modulus.Host.Blazor`
- **THEN** 仅解析 `[BlazorMenu]`
- **AND** 忽略 `[AvaloniaMenu]`

#### Scenario: Multiple menus are allowed for diagnostics
- **WHEN** 同一入口类型声明了多条菜单属性
- **THEN** 全部投影到 DB 并在 UI 中显示（不做折叠）

### Requirement: Installation without executing module code
模块安装/更新 MUST 不执行第三方模块代码；菜单与元数据解析 MUST 使用 metadata-only 方式完成。

#### Scenario: Metadata-only attribute parsing
- **WHEN** 安装器需要读取菜单属性
- **THEN** 使用 metadata-only 解析（如 `System.Reflection.Metadata`）
- **AND** 不创建可执行的模块实例
- **AND** 不触发模块程序集的静态初始化

#### Scenario: Failure fast on invalid metadata
- **WHEN** 菜单属性缺失必填字段（如 `Route` / `DisplayName` / `Key`）
- **THEN** 安装失败并输出清晰诊断信息
- **AND** 不写入不完整菜单到数据库

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

### Requirement: Module discovery by directory scanning
运行时 SHALL 通过扫描系统模块目录与用户模块目录发现模块包（以 `extension.vsixmanifest` 为入口），并在启动时执行 install/update 投影。

#### Scenario: Built-in modules discovered from app Modules directory
- **WHEN** 应用启动
- **THEN** 扫描 `{AppBaseDir}/Modules/` 下的模块目录
- **AND** 对每个包含 `extension.vsixmanifest` 的模块执行 install/update（含菜单投影）

#### Scenario: User modules discovered from user Modules directory
- **WHEN** 应用启动
- **THEN** 扫描 `%APPDATA%/Modulus/Modules/` 下的模块目录
- **AND** 对每个包含 `extension.vsixmanifest` 的模块执行 install/update（含菜单投影）

#### Scenario: Disabled modules are not loaded but remain installed
- **WHEN** 数据库中模块 `IsEnabled=false`
- **THEN** 启动时不加载该模块程序集
- **AND** 模块文件仍保留在磁盘
- **AND** 用户将其重新启用后可再次加载

### Requirement: No legacy data or menu source compatibility
系统 MUST 不兼容旧数据库与旧菜单来源；检测到旧数据结构或旧来源标记时应失败快并指导清理。

#### Scenario: Legacy database detected
- **WHEN** 启动时检测到数据库包含旧菜单来源结构（例如来自 vsixmanifest `Modulus.Menu` 或 bundled 清单路径）
- **THEN** 启动失败
- **AND** 日志明确提示删除数据库文件后重启

