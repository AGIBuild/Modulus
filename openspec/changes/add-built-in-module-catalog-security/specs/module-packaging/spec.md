# module-packaging Specification (delta)

## ADDED Requirements

### Requirement: User module anti-rollback stored in database
用户模块安装/更新 MUST 实施防回滚策略：系统 SHALL 在数据库中记录每个用户模块的最大已接受版本，并在安装时拒绝降级。

#### Scenario: Install higher version updates max accepted version
- **WHEN** 用户安装用户模块版本 `1.2.0`
- **AND** 数据库中该模块的 `MaxAcceptedVersion` 不存在或小于 `1.2.0`
- **THEN** 安装成功
- **AND** 数据库将 `MaxAcceptedVersion` 更新为 `1.2.0`。

#### Scenario: Install lower version is rejected as rollback
- **WHEN** 数据库中用户模块 `MaxAcceptedVersion` 为 `1.2.0`
- **AND** 用户尝试安装版本 `1.1.0`
- **THEN** 安装失败
- **AND** 输出错误：检测到回滚安装（downgrade）被拒绝。

## MODIFIED Requirements

### Requirement: Built-in module selection via host project references
构建系统 SHALL 允许 Host 通过项目引用选择“随应用发布”的内置模块集合，并生成不可篡改的 Built-in Module Catalog；运行时不将模块程序集加载进默认 ALC，且系统模块不依赖目录扫描发现。

#### Scenario: Host includes built-in modules without referencing output assembly
- **WHEN** Host 项目引用一个内置模块项目
- **THEN** 该引用使用 `ProjectReference` 并设置 `ReferenceOutputAssembly="false"`
- **AND** 构建输出将模块产物输出到 `artifacts/bin/Modules/{ModuleName}/`
- **AND** 发布时将该模块复制到 `{AppBaseDir}/Modules/{ModuleName}/`
- **AND** 构建阶段生成 Built-in Module Catalog（包含 ModuleId、相对路径与完整性信息）并编译进 Host
- **AND** Host 运行时仍由 `ModuleLoader` 在独立 ALC 中加载该模块（不经默认 ALC）。


