## MODIFIED Requirements
### Requirement: Menu Projection
Menu entries SHALL be projected to the database at install/update time and read from the database at render time.

#### Scenario: Install or update module projects menus to database
- **WHEN** 模块被安装或更新
- **THEN** 安装器解析模块入口类型上的模块级菜单属性（Blazor: `[BlazorMenu]`，Avalonia: `[AvaloniaMenu]`）
- **AND** 安装器以 metadata-only 方式解析模块 UI 程序集中的 View 级菜单属性（新增）
- **AND** 将菜单写入数据库表 `Menus`，并构建父子层级（父为模块级菜单，子为 View 级菜单，使用 `ParentId`）

#### Scenario: Single-view module ignores view-level menus
- **GIVEN** 模块 UI 程序集中可识别的 View 数量为 1
- **AND** 该 View 声明了 View 级菜单元数据
- **WHEN** 安装器投影菜单到数据库
- **THEN** 数据库中仅写入模块级菜单项（`ParentId = null`）
- **AND** 不写入该 View 的二级菜单项（避免重复）

#### Scenario: Multi-view module projects view-level menus as children
- **GIVEN** 模块 UI 程序集中可识别的 View 数量大于 1
- **WHEN** 安装器投影菜单到数据库
- **THEN** 数据库中写入 1 条模块级菜单项（`ParentId = null`）
- **AND** 为每个 View 写入 1 条二级菜单项（`ParentId = <模块级菜单Id>`）

#### Scenario: Render menus reads from database only
- **WHEN** Shell 渲染导航菜单
- **THEN** 从数据库读取 `Menus` 与 `Modules`（仅 `IsEnabled=true` 且状态可加载）
- **AND** 运行时将 DB 菜单组装为树结构注册到 `IMenuRegistry`
- **AND** 渲染过程不进行任何 DLL 动态解析（不反射、不 metadata 扫描、不读取菜单属性）

### Requirement: Installation without executing module code
模块安装/更新 MUST 不执行第三方模块代码；菜单与元数据解析 MUST 使用 metadata-only 方式完成。

#### Scenario: Metadata-only view menu parsing
- **WHEN** 安装器需要读取 View 级菜单声明
- **THEN** 使用 `System.Reflection.Metadata` 解析程序集 custom attributes
- **AND** 不创建任何可执行模块实例
- **AND** 不触发模块程序集的静态初始化


