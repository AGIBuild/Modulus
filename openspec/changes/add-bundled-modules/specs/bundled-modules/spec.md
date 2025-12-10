## ADDED Requirements

### Requirement: Strongly-typed migration infrastructure
迁移 SHALL 使用强类型接口和扩展方法，避免硬编码表名/列名。

#### Scenario: Seed data interfaces defined
- **WHEN** 定义模块种子数据
- **THEN** 实现 `IModuleSeedData` 接口
- **AND** 实现 `IMenuSeedData` 接口
- **AND** 接口属性与实体属性一一对应

#### Scenario: Migration builder extensions
- **WHEN** 迁移中插入模块数据
- **THEN** 使用 `migrationBuilder.InsertModule(IModuleSeedData)` 扩展方法
- **AND** 使用 `migrationBuilder.InsertMenu(IMenuSeedData)` 扩展方法
- **AND** 扩展方法内部使用 `nameof()` 获取列名

### Requirement: Module data migration via EF Core
内置模块数据 SHALL 通过 EF Core 迁移管理，使用强类型扩展方法。

#### Scenario: Module data in migration
- **WHEN** EF 迁移包含模块种子数据
- **THEN** 使用 `migrationBuilder.InsertModule()` 插入模块记录
- **AND** 使用 `migrationBuilder.InsertMenu()` 插入菜单记录
- **AND** `Down()` 方法使用 `migrationBuilder.DeleteModule()` 删除数据

#### Scenario: Migration applies on startup
- **WHEN** 应用启动执行 `MigrateAsync()`
- **THEN** 未应用的模块数据迁移自动执行
- **AND** 模块记录写入数据库

### Requirement: CLI module migration generator
CLI SHALL 提供 `add-module-migration` 命令自动生成模块数据迁移。

#### Scenario: Generate migration for new module
- **WHEN** 执行 `modulus add-module-migration`
- **AND** `src/Modules/EchoPlugin/` 存在且未被迁移
- **THEN** 生成迁移文件 `{timestamp}_SeedModule_AddEchoPlugin.cs`
- **AND** 迁移包含 `InsertData` 调用

#### Scenario: Generate migration for updated module
- **WHEN** 模块版本从 `1.0.0` 更新到 `1.1.0`
- **AND** 执行 `modulus add-module-migration`
- **THEN** 生成迁移文件包含 `UpdateData` 调用

#### Scenario: Generate migration for removed module
- **WHEN** 模块目录被删除
- **AND** 执行 `modulus add-module-migration`
- **THEN** 生成迁移文件包含 `DeleteData` 调用

#### Scenario: Auto-generated migration filename
- **WHEN** 生成迁移
- **THEN** 文件名格式为 `{timestamp}_SeedModule_{Action}.cs`
- **AND** 无需用户手动指定名称

### Requirement: Simplified startup flow
运行时启动 SHALL 直接从数据库加载模块，内置模块无需目录扫描。

#### Scenario: Release mode startup
- **WHEN** 应用以 RELEASE 模式启动
- **THEN** 执行 `MigrateAsync()` 应用迁移
- **AND** 不扫描 `{AppBaseDir}/Modules/` 目录
- **AND** 仅扫描用户模块目录（如有）
- **AND** 从数据库查询 `IsEnabled=true` 的模块加载

#### Scenario: Debug mode startup
- **WHEN** 应用以 DEBUG 模式启动
- **THEN** 保留 `artifacts/Modules/` 目录扫描
- **AND** 便于开发调试

#### Scenario: User-installed modules
- **WHEN** 用户通过 UI 安装模块
- **THEN** 使用现有 `InstallFromPathAsync()` 流程
- **AND** 模块写入用户目录和数据库

### Requirement: Migration consolidation
现有迁移 SHALL 合并为单一 `InitialCreate`，提供干净起点。

#### Scenario: Squash existing migrations
- **WHEN** 执行迁移合并
- **THEN** 删除现有 8 个迁移文件
- **AND** 生成新的 `InitialCreate` 包含完整 schema
- **AND** `InitialCreate` 包含 Host 内置模块种子数据

#### Scenario: Fresh database setup
- **WHEN** 新环境首次运行
- **THEN** `InitialCreate` 迁移创建完整 schema
- **AND** Host 内置模块自动注册
