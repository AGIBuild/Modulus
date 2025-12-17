## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: No legacy data or menu source compatibility
系统 MUST 不兼容旧数据库与旧菜单来源；检测到旧数据结构或旧来源标记时应失败快并指导清理。

#### Scenario: Legacy database detected
- **WHEN** 启动时检测到数据库包含旧菜单来源结构（例如来自 vsixmanifest `Modulus.Menu` 或 bundled 清单路径）
- **THEN** 启动失败
- **AND** 日志明确提示删除数据库文件后重启


