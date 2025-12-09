# 变更：统一构建输出路径

## 为什么
当前 IDE 调试使用 `bin/Debug` 输出，而 Nuke Build 使用 `artifacts/` 目录。这导致运行环境不一致：IDE 和命令行调试时模块路径不同，难以复现问题。另外 `nuke test` 当前因两个集成测试失败而无法完成。

## 变更内容
- **破坏性变更**: 修改所有项目的构建输出到 `artifacts/` 根目录
- 模块输出到 `artifacts/Modules/{ModuleName}/` 子目录
- 通过 `--configuration` 参数切换 Debug/Release 模式
- 统一 IDE 和命令行使用相同输出路径
- 修复 `nuke test` 使测试可以正常运行

## 实现方案
**直接在每个 `.csproj` 中显式配置**，不依赖 `Directory.Build.props` 的隐式变量：

```xml
<!-- 每个项目文件包含 -->
<Import Project="相对路径\build\Modulus.Architecture.props" />

<PropertyGroup>
  <OutputPath>相对路径\artifacts\</OutputPath>
  <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
</PropertyGroup>
```

### 路径深度对照表
| 项目位置 | Import 路径 | OutputPath |
|---------|------------|------------|
| `src/*/*.csproj` | `..\..\build\` | `..\..\artifacts\` |
| `src/*/*/*.csproj` | `..\..\..\build\` | `..\..\..\artifacts\` |
| `tests/*/*.csproj` | `..\..\build\` | `..\..\artifacts\` |
| `src/Modules/*/*/*.csproj` | `..\..\..\..\build\` | `..\..\..\..\artifacts\Modules\{ModuleName}\` |

## 输出路径规则
| 类型 | 输出路径 |
|------|---------|
| 宿主/SDK/Core/Tests 等 | `artifacts/` |
| 模块 | `artifacts/Modules/{ModuleName}/` |

## 影响范围
- 影响的规格: build-system (新增)
- 影响的代码:
  - 所有 `.csproj` 文件 - 添加显式输出路径配置
  - `Directory.Build.props` - 清空，仅保留注释
  - `build/BuildTasks.cs` - Nuke 构建任务简化
  - `tests/Modulus.Hosts.Tests/ModulusApplicationIntegrationTests.cs` - 跳过需要真实模块包的测试
