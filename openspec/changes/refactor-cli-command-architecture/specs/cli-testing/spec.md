## MODIFIED Requirements

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
- **WHEN** handler 需要使用 Console/IO/Process 等副作用能力
- **THEN** handler 通过可注入抽象接口访问这些能力
- **AND** 测试可替换为 fake 实现以实现 deterministic 行为


