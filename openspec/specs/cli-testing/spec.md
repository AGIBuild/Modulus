# cli-testing Specification

## Purpose
CLI 集成测试规范，定义 CLI 命令的测试环境隔离、数据库一致性和全生命周期验证。本规范同时规定测试目录布局、数据清理策略与失败诊断输出要求，以确保测试可重复与可追溯。
## Requirements
### Requirement: CLI Test Environment Isolation

CLI 集成测试 SHALL 在隔离环境中运行，不影响真实用户数据。

#### Scenario: Each test uses isolated directories

- **WHEN** 运行 CLI 集成测试
- **THEN** 测试使用独立的临时工作目录
- **AND** 测试使用独立的 SQLite 数据库文件
- **AND** 测试使用独立的模块安装目录
- **AND** 测试完成后清理所有临时文件

#### Scenario: CliServiceProvider supports parameterized paths

- **WHEN** 调用 `CliServiceProvider.Build(databasePath: path, modulesDirectory: path)`
- **THEN** CLI 服务使用指定的数据库路径
- **AND** CLI 服务使用指定的模块目录

#### Scenario: CLI supports path overrides via environment variables

- **WHEN** 设置环境变量 `MODULUS_CLI_DATABASE_PATH` 与 `MODULUS_CLI_MODULES_DIR`
- **THEN** CLI 使用该数据库路径与模块目录执行 `install/uninstall/list`
- **AND** 测试可通过设置这些变量实现 in-process 隔离而不写入真实用户目录

### Requirement: Database Schema Consistency

测试数据库 schema SHALL 与 Host 应用数据库 schema 保持一致。

#### Scenario: Test database uses same migrations

- **WHEN** 测试初始化数据库
- **THEN** 运行与 Host 相同的 EF Core Migrations
- **AND** 使用相同的 `ModulusDbContext` 定义
- **AND** 数据库 schema 与 Host 完全一致

### Requirement: CLI Command Tests

CLI 集成测试 SHALL 覆盖所有 CLI 命令的基本功能，并优先使用 in-process 执行路径以确保覆盖率可统计与执行稳定。

#### Scenario: Commands can be executed in-process for coverage

- **WHEN** 运行 `Modulus.Cli.IntegrationTests`
- **THEN** `new/build/pack/install/uninstall/list` 的主路径通过 in-process 执行（handler/entrypoint 调用）
- **AND** 不依赖子进程执行 `modulus.dll` 来覆盖核心逻辑

#### Scenario: Process-based end-to-end tests are allowed but minimal

- **WHEN** 需要验证 CLI 产物可执行（例如 `modulus.dll` 可被 `dotnet` 启动）
- **THEN** 允许保留少量进程外端到端测试
- **AND** 这些测试不作为覆盖率 gate 的主要来源

#### Scenario: New command creates module
- **WHEN** 执行 `modulus new -n TestModule --force`
- **THEN** 创建包含正确结构的模块目录
- **AND** 生成 `.sln`、`.csproj`、`extension.vsixmanifest` 等文件

#### Scenario: Build command compiles module
- **WHEN** 在模块目录执行 `modulus build`
- **THEN** 编译成功并生成 DLL 文件
- **AND** 返回退出码 0

#### Scenario: Pack command creates package
- **WHEN** 在模块目录执行 `modulus pack`
- **THEN** 生成 `.modpkg` 文件
- **AND** 包内包含 `extension.vsixmanifest` 和模块 DLL

#### Scenario: Install command registers module
- **WHEN** 执行 `modulus install <path>.modpkg`
- **THEN** 解压包到模块目录
- **AND** 在数据库中注册模块
- **AND** `modulus list` 显示已安装模块

#### Scenario: Uninstall command removes module
- **WHEN** 执行 `modulus uninstall <name> --force`
- **THEN** 从数据库中删除模块记录
- **AND** `modulus list` 不再显示该模块

#### Scenario: List command shows installed modules
- **WHEN** 已安装模块后执行 `modulus list`
- **THEN** 显示模块名称和版本
- **WHEN** 执行 `modulus list --verbose`
- **THEN** 额外显示模块 ID、发布者等详细信息

### Requirement: CLI Test Coverage Gate

关键组件 `Modulus.Cli\` 行覆盖率 MUST 达到 95% 以上。

#### Scenario: Coverage gate fails when below threshold

- **WHEN** 运行覆盖率汇总脚本（Cobertura）
- **THEN** 如果 `Modulus.Cli\` 行覆盖率 < 95%
- **AND** 构建/CI 失败并输出最低覆盖文件列表

### Requirement: CLI Command Handler Architecture

CLI 命令实现 SHALL 采用 “薄 Command + 厚 Handler” 的结构以支持可测试性与依赖注入。

#### Scenario: Command delegates to handler

- **WHEN** 用户执行任意 CLI 命令（例如 `modulus pack`）
- **THEN** Command 层仅负责参数解析与调用
- **AND** 核心业务逻辑位于对应 handler 类中

#### Scenario: Handlers depend on injectable abstractions

- **WHEN** handler 需要使用 Console/Process 等副作用能力
- **THEN** handler 通过可注入抽象接口访问这些能力（例如 `ICliConsole`, `IProcessRunner`）
- **AND** 测试可替换为 fake 实现以实现 deterministic 行为

### Requirement: Module Load Verification

CLI 集成测试 SHALL 验证生成的模块能被 `ModuleLoader` 正确加载。

#### Scenario: Generated module can be loaded

- **WHEN** 使用 `modulus new` 创建模块并编译
- **THEN** 模块能被 `ModuleLoader` 成功加载
- **AND** 返回正确的模块描述符（ID、版本、支持的 Host）

#### Scenario: Module load does not require database

- **WHEN** 使用 `ModuleLoader` 验证模块加载
- **THEN** 不需要数据库连接
- **AND** 使用内存中的 `RuntimeContext` 和 `SharedAssemblyCatalog`

### Requirement: Full Lifecycle Test

CLI 集成测试 SHALL 验证完整的模块开发生命周期。

#### Scenario: Complete workflow succeeds

- **WHEN** 依次执行 new → build → pack → install → list → uninstall → list
- **THEN** 每个步骤返回成功退出码
- **AND** 安装后 list 显示模块
- **AND** 卸载后 list 显示空列表

### Requirement: Nuke Build Integration

CLI 集成测试 SHALL 集成到 Nuke 构建系统。

#### Scenario: TestCli target runs CLI tests

- **WHEN** 执行 `nuke test-cli`
- **THEN** 运行所有 CLI 集成测试
- **AND** 测试失败时构建失败

#### Scenario: PublishCli requires tests to pass

- **WHEN** 执行 `nuke publish-cli`
- **THEN** 先运行 CLI 集成测试
- **AND** 测试通过后才执行发布
- **AND** 测试失败时发布中止

#### Scenario: PackCli requires tests to pass

- **WHEN** 执行 `nuke pack-cli`
- **THEN** 先运行 CLI 集成测试
- **AND** 测试通过后才执行打包
- **AND** 测试失败时打包中止

