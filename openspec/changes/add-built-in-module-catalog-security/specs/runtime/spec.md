# runtime Specification (delta)

## ADDED Requirements

### Requirement: Built-in module allowlist via host-compiled catalog
运行时 MUST 仅将 Host 编译进程序集的 Built-in Module Catalog 作为“系统模块”来源，不得通过扫描 `{AppBaseDir}/Modules/` 自动发现系统模块，以防投放/注入。

#### Scenario: Foreign module dropped into app Modules directory is ignored
- **WHEN** 用户向 `{AppBaseDir}/Modules/` 投放一个包含 `extension.vsixmanifest` 的目录
- **AND** 该模块不在 Built-in Module Catalog allowlist 中
- **THEN** 启动同步不会将其写入数据库
- **AND** 运行时不会加载该模块
- **AND** 导航菜单不会显示该模块条目。

#### Scenario: Built-in module list is stable and reviewable
- **WHEN** Host 项目通过 `ProjectReference` 引用内置模块项目并生成 Catalog
- **THEN** Built-in Module Catalog 明确列出系统模块集合（ModuleId、ModuleName、相对路径、完整性信息）
- **AND** 运行时以该 Catalog 作为唯一系统模块来源。

### Requirement: Built-in module integrity policy (Production vs Development)
运行时 MUST 对系统模块执行完整性校验以防“替换模块文件”；校验策略 MUST 受 `DOTNET_ENVIRONMENT` 控制：
- Production（非 Development）：强校验（SHA256）
- Development：弱校验（仅程序集名/强名称），且仍限定 `{AppBaseDir}/Modules/`

#### Scenario: Production integrity rejects tampered system module
- **WHEN** `DOTNET_ENVIRONMENT` 不是 `Development`
- **AND** 系统模块的 `extension.vsixmanifest` 或目标程序集文件 SHA256 与 Catalog 不匹配
- **THEN** 系统模块状态被标记为不可加载（例如 `Tampered` 或 `Incompatible`）
- **AND** 该模块不被加载
- **AND** 诊断日志明确指出不匹配文件与期望/实际 hash。

#### Scenario: Development integrity accepts rebuilt system module with correct identity
- **WHEN** `DOTNET_ENVIRONMENT=Development`
- **AND** 系统模块位于 `{AppBaseDir}/Modules/{ModuleName}/`
- **AND** 目标程序集的 `AssemblyName.Name` 与 Catalog 期望值一致
- **AND** 若程序集为强签名，则其 PublicKeyToken 与 Catalog 期望值一致
- **THEN** 完整性校验通过，模块可入库/可加载
- **AND** 不要求程序集 SHA256 与 Catalog 匹配。

#### Scenario: Development warns when strong-name is absent
- **WHEN** `DOTNET_ENVIRONMENT=Development`
- **AND** 目标程序集未强签名
- **THEN** 运行时仅基于 `AssemblyName.Name` 校验并允许加载
- **AND** 输出 warning 指明“强名称缺失，仅开发环境允许”。

### Requirement: System modules cannot be disabled or uninstalled
系统模块（`IsSystem=true`）必须被保护：运行时 MUST 拒绝禁用/卸载系统模块，并在启动时修复被篡改的状态。

#### Scenario: Disable requested for system module is rejected
- **WHEN** 用户请求禁用一个系统模块
- **THEN** 操作被拒绝并返回明确错误
- **AND** 模块保持 `IsEnabled=true`。

#### Scenario: Startup self-heals system module enabled flag
- **WHEN** 启动同步发现某系统模块在数据库中 `IsEnabled=false`
- **THEN** 运行时将其纠正为 `IsEnabled=true`
- **AND** 记录 warning 以便诊断。

## MODIFIED Requirements

### Requirement: Module discovery by directory scanning
运行时 SHALL 通过扫描用户模块目录发现用户模块包（以 `extension.vsixmanifest` 为入口），并在启动时执行 install/update 投影；系统模块 MUST 由 Host 编译进程序集的 Built-in Module Catalog 驱动，不得通过扫描 `{AppBaseDir}/Modules/` 自动发现。

#### Scenario: Built-in modules discovered from host catalog
- **WHEN** 应用启动
- **THEN** 读取 Host 内置的 Built-in Module Catalog
- **AND** 对 Catalog 中每个系统模块执行启动同步（缺失/变更/菜单缺失才 install/update 投影，方案 2B）
- **AND** 不扫描 `{AppBaseDir}/Modules/` 以发现系统模块。

#### Scenario: User modules discovered from user Modules directory
- **WHEN** 应用启动
- **THEN** 扫描 `%APPDATA%/Modulus/Modules/` 下的模块目录
- **AND** 对每个包含 `extension.vsixmanifest` 的模块执行 install/update（含菜单投影）。

#### Scenario: Startup loads from database only
- **WHEN** 应用启动加载模块
- **THEN** 直接从数据库查询已启用且 `State=Ready` 的模块并加载
- **AND** 导航菜单渲染仅从数据库读取。


