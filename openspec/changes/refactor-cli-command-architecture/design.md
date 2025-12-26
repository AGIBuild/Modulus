## Context
`Modulus.Cli` 目前存在两类测试路径：
- `install/uninstall/list` 已经具备 handler 形态（`Commands/Handlers/*`），测试可以直接调用 handler 并注入 `ServiceProvider`，因此覆盖率可统计。
- `new/build/pack` 以进程执行为主（测试通过 `dotnet "<modulus.dll>" ...` 跑 CLI），导致覆盖率采集只覆盖测试进程，`Modulus.Cli` 关键文件几乎为 0%。

本变更的核心是统一架构与依赖注入方式，使所有命令均可 in-process 执行并被覆盖率工具准确统计。

## Goals / Non-Goals
- Goals
  - 统一 `Modulus.Cli` 为 **Command -> Handler** 架构。
  - 将外部副作用（Console/IO/Process）隔离到可替换抽象，支持测试注入与可重复执行。
  - 让 CLI 集成测试可 **in-process** 覆盖 `new/build/pack/install/uninstall/list` 的主路径。
  - 将 `Modulus.Cli\` 行覆盖率提升到 **>= 95%**（以关键路径为准）。
- Non-Goals
  - 不改变 CLI 的用户可见行为（参数、默认输出文案、目录结构）除非为修复 bug。
  - 不引入重量级命令行 UI 框架（例如迁移到 Spectre.Console）作为本变更前置条件。
  - 不在本变更内重写 `dotnet build` 本身的行为；只提供可测试的“进程执行适配层”。

## Decisions
- Decision: 命令逻辑全部下沉到 Handler
  - 每个命令对应一个 handler（例如 `NewHandler` / `BuildHandler` / `PackHandler` / `InstallHandler` / `UninstallHandler` / `ListHandler`）。
  - Command 层只做：`System.CommandLine` 选项定义、解析、调用 handler。

- Decision: 引入最小抽象层隔离副作用
  - 抽象接口（实际命名）：
    - `ICliConsole`：输出/输入、是否交互（替换 `Console.*`）。
    - `IProcessRunner`：进程执行（替换 `Process.Start`；测试可用 fake runner）。
  - 默认实现：
    - `SystemCliConsole`、`ProcessRunner`。
  - Handler 仅依赖接口；默认实现通过 DI 组装。
  - 路径隔离：
    - 测试通过环境变量 `MODULUS_CLI_DATABASE_PATH` / `MODULUS_CLI_MODULES_DIR` 覆盖 CLI 使用的数据库路径与模块目录（由 `CliPathOverrides` 读取）。

- Decision: in-process 测试优先
  - `Modulus.Cli.IntegrationTests` 默认通过 handler/entrypoint in-process 执行命令。
  - 仅在必须验证“CLI 打包产物可执行”时，保留少量子进程端到端测试，但不计入覆盖率 gate 的关键路径统计。

## Risks / Trade-offs
- 风险：重构触发行为漂移（输出/错误码/默认路径）
  - 缓解：以现有集成测试用例为回归基线；对输出保留关键断言（不做过度字符串耦合）。
- 风险：抽象层过多导致复杂度上升
  - 缓解：严格限制接口数量与职责；一类副作用只允许一个抽象入口。

## Migration Plan
- 先补齐 handler 架构与 DI 基座，再逐个迁移命令（`new`→`build`→`pack`→其余）。
- 同步迁移测试为 in-process，并以覆盖率报告作为 gate。

## Open Questions
- 是否需要将 `LocalStorage.GetUserRoot()` 增加可覆盖机制（环境变量/配置）来进一步提升可测试性？
  - 本变更已通过 CLI 层环境变量覆盖（`MODULUS_CLI_DATABASE_PATH` / `MODULUS_CLI_MODULES_DIR`）满足测试隔离需求，暂不在 Core 层引入额外全局开关。


