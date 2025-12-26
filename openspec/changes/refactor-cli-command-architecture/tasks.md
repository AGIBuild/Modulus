## 1. Spec
- [x] 1.1 创建 `refactor-cli-command-architecture` change（proposal/tasks/design）
- [x] 1.2 增加 `cli-testing` delta spec（MODIFIED requirements）
- [x] 1.3 `openspec validate refactor-cli-command-architecture --strict` 通过

## 2. Implementation
- [x] 2.1 增加 CLI 架构基座（handlers + DI 组装 + 抽象接口）
- [x] 2.2 迁移 `new/build/pack` 到 handler 架构并保持行为不变
- [x] 2.3 迁移 `install/uninstall/list` 命令入口，使其与 handler 统一（保留现有 handler 能力）
- [x] 2.4 将 `Modulus.Cli.IntegrationTests` 调整为 in-process 执行主路径（覆盖率可统计）
- [x] 2.5 补齐单元/集成测试分层与关键用例
- [x] 2.6 覆盖率 gate：`Modulus.Cli\` 行覆盖率 >= 95%
- [x] 2.7 全量 `dotnet test Modulus.sln -c Release` 通过


