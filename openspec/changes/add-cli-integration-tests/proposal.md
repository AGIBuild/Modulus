# Change: Add CLI Integration Tests

## Why

当前 Modulus CLI 工具缺乏集成测试覆盖。在发布前无法自动验证 `new`、`build`、`pack`、`install`、`uninstall`、`list` 等命令的端到端工作流程是否正常。这增加了发布回归风险。

## What Changes

- 新增 `Modulus.Cli.IntegrationTests` 测试项目
- 实现 CLI 命令执行器 (`CliRunner`)
- 实现测试环境隔离机制 (`CliTestContext`)
- 覆盖所有 6 个 CLI 命令的集成测试
- 实现完整生命周期端到端测试 (`new → build → pack → install → list → uninstall`)
- 集成到 Nuke 构建系统 (`test-cli` target)

## Impact

- Affected specs: 新增 `cli-testing` capability
- Affected code:
  - `tests/Modulus.Cli.IntegrationTests/` (新项目)
  - `build/BuildTasks.cs` (新增 `TestCli` target)
  - `Modulus.sln` (添加测试项目引用)

