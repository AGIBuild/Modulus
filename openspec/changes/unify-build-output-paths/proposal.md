# 变更：统一构建输出路径

## 为什么
当前 IDE 调试使用 `bin/Debug` 输出，而 Nuke Build 使用 `artifacts/` 目录。这导致运行环境不一致：IDE 和命令行调试时模块路径不同，难以复现问题。另外 `nuke test` 当前因两个集成测试失败而无法完成。

## 变更内容
- **破坏性变更**: 修改所有项目的构建输出到 `artifacts/` 根目录
- 模块输出到 `artifacts/Modules/{ModuleName}/` 子目录
- 通过 `--configuration` 参数切换 Debug/Release 模式（参考现有实现）
- 统一 IDE 和命令行使用相同输出路径
- 修复 `nuke test` 使测试可以正常运行

## 输出路径规则
| 类型 | 输出路径 |
|------|---------|
| 宿主/SDK/Core 等 | `artifacts/` |
| 模块 | `artifacts/Modules/{ModuleName}/` |

## 影响范围
- 影响的规格: build-system (新增)
- 影响的代码:
  - `build/BuildTasks.cs` - Nuke 构建任务
  - `Directory.Build.props` - MSBuild 输出路径配置
  - `tests/Modulus.Hosts.Tests/ModulusApplicationIntegrationTests.cs` - 失败的集成测试
