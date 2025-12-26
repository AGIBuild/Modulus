# Change: Refactor CLI Command Architecture for Testability & Coverage

## Why
当前 `Modulus.Cli` 的部分命令（尤其 `new/build/pack`）以“进程外执行 + `Console`/`Process`/`File` 直连”为主，导致集成测试必须起子进程运行 `modulus.dll`，覆盖率采集无法统计子进程代码，`Modulus.Cli` 覆盖率长期低于预期（当前基线约 13%）。

本变更通过统一的 **Command -> Handler** 架构与可替换依赖抽象，使关键命令可以 **in-process** 执行，从而提升测试稳定性、可诊断性与覆盖率，并为后续 CLI 迭代提供可持续的工程基座。

## What Changes
- 将 `Modulus.Cli` 所有命令收敛为“薄 Command + 厚 Handler”的结构：Command 负责解析参数与绑定；Handler 负责业务逻辑。
- 引入最小集合的可替换依赖抽象（`ICliConsole` / `IProcessRunner`），隔离外部副作用，支持测试注入与 deterministic 测试。
- 调整 `Modulus.Cli.IntegrationTests`：优先使用 **in-process** 执行路径（调用 handler/entrypoint），避免子进程导致覆盖率丢失。
- 增加覆盖率 gate：`Modulus.Cli\` 关键路径行覆盖率目标 **>= 95%**（不要求覆盖外部工具链自身，如 `dotnet build` 的真实编译行为）。
 - 为测试与自动化提供路径隔离：通过环境变量 `MODULUS_CLI_DATABASE_PATH` / `MODULUS_CLI_MODULES_DIR` 覆盖 CLI 使用的数据库与模块目录。

## Impact
- **Affected specs**: `cli-testing` (MODIFIED)
- **Affected code** (expected):
  - `src/Modulus.Cli/Program.cs`
  - `src/Modulus.Cli/Commands/*`
  - `src/Modulus.Cli/Commands/Handlers/*`
  - `tests/Modulus.Cli.IntegrationTests/*`
  - `build/*`（覆盖率采集与汇总脚本/配置）


