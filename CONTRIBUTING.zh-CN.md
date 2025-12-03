# 参与贡献 Modulus

感谢您对 Modulus 项目的关注！本指南将帮助您开始参与项目贡献。

## 架构概述

Modulus 是一个具有**多宿主架构**的模块化 .NET 应用框架：

### 核心原则

1. **UI 无关的核心层**: Domain 和 Application 代码不能依赖任何 UI 框架
2. **多宿主支持**: 相同的业务逻辑可在所有支持的宿主上运行 (Blazor、Avalonia 及未来的宿主)
3. **垂直切片模块**: 每个功能是一个独立的模块，包含自己的各层
4. **依赖金字塔**: Presentation → UI Abstraction → Application → Domain → Infrastructure

### 项目结构

```
src/
├── Modulus.Core/              # 运行时、ModuleLoader、DI、MediatR
├── Modulus.Sdk/               # SDK 基类 (ModuleBase, 属性)
├── Modulus.UI.Abstractions/   # UI 契约 (IMenuRegistry, IThemeService)
├── Hosts/
│   ├── Modulus.Host.Blazor/   # MAUI + MudBlazor
│   └── Modulus.Host.Avalonia/ # Avalonia UI
└── Modules/
    └── <ModuleName>/
        ├── <ModuleName>.Core/         # Domain + Application (UI 无关)
        ├── <ModuleName>.UI.Avalonia/  # Avalonia 视图
        └── <ModuleName>.UI.Blazor/    # Blazor 组件
```

### 模块开发

创建新模块时：

1. **Core 项目**: 包含 ViewModel 和业务逻辑，仅引用 `Modulus.Sdk` 和 `Modulus.UI.Abstractions`
2. **UI 项目**: 宿主特定的视图，引用 Core 项目和 UI 框架
3. **清单文件**: `manifest.json` 描述模块元数据和程序集映射
4. **属性**: 使用 `[Module]`、`[AvaloniaMenu]`、`[BlazorMenu]` 进行声明式注册

详细说明请参阅 [快速入门指南](./specs/001-core-architecture/quickstart.md)。

## 入门指南

1. Fork 本仓库
2. 克隆您的 fork: `git clone https://github.com/your-username/modulus.git`
3. 创建新分支: `git checkout -b feature/your-feature-name`
4. 进行修改
5. 运行测试: `dotnet test`
6. 提交更改: `git commit -m "Add feature"`
7. 推送到您的 fork: `git push origin feature/your-feature-name`
8. 创建 Pull Request

## 使用 AI 上下文与 GitHub Copilot

Modulus 提供了内置系统，为 GitHub Copilot 等 AI 工具引导项目上下文，使您更容易理解项目并获得符合项目规范的 AI 辅助。

### 使用 StartAI 命令

在开始使用 AI 辅助进行开发前，请运行:

```powershell
nuke StartAI
```

此命令将输出全面的项目上下文，您可以将其粘贴到 GitHub Copilot Chat 中，以引导其理解 Modulus 项目。

对于特定角色的上下文，使用 `--role` 参数:

```powershell
# 后端开发人员
nuke StartAI --role Backend

# 前端开发人员
nuke StartAI --role Frontend  

# 插件开发人员
nuke StartAI --role Plugin

# 文档贡献者
nuke StartAI --role Docs
```

### 更新 AI 清单

AI 清单可以通过代码库分析自动更新：

```powershell
# 使用自动检测更新 AI 清单
nuke SyncAIManifest

# 使用详细输出运行
nuke SyncAIManifest --verbose true
```

这将扫描代码库并用检测到的目录、命名约定和其他结构信息更新 AI 清单，同时保留手动编辑的部分。

### AI 使用指南

使用 GitHub Copilot 等 AI 工具辅助开发时：

1. 提交前必须先测试 AI 生成的代码，确保其按预期工作
2. 仔细检查 AI 生成的变更，确保它们遵循项目标准
3. 在进行 AI 建议的更改后运行相应的测试
4. 使用 `nuke StartAI` 确保 Copilot 获得最新的项目上下文
5. 所有代码必须按照 editorconfig 配置文件进行格式化
6. 在 AI agent 工作过程中，使用 `nuke commandName` 的方式执行 Nuke 任务，而不是通过 dotnet 命令编译项目传入参数的方式

### Copilot Chat 的快速参考命令

向 Copilot 提供上下文后，您可以在 Copilot Chat 中使用以下命令:

- `/sync` - 刷新项目上下文
- `/roadmap` - 查看项目路线图
- `/why <file>` - 获取特定文件目的的解释

## 文档标准

- 所有面向用户的文档都应有英文和中文版本
- 所有 Story 文档必须有双语版本（位于 `docs/en-US/stories/` 和 `docs/zh-CN/stories/`）
- 遵循 Story 命名约定: `S-XXXX-标题.md`
- 在 Story 文档中包含优先级和状态标签

## 代码风格指南

- 类名和公共成员使用 PascalCase
- 局部变量和参数使用 camelCase
- 私有字段前缀使用下划线 (`_privateField`)
- 为公共 API 添加 XML 文档注释
- 为所有新功能编写单元测试

## 构建和运行

### 快速开始
```bash
# 运行 Avalonia 宿主
dotnet run --project src/Hosts/Modulus.Host.Avalonia

# 运行 Blazor 宿主
dotnet run --project src/Hosts/Modulus.Host.Blazor

# 运行所有测试
dotnet test
```

### Nuke 构建系统
- 使用 Nuke 构建系统: `nuke --help` 查看可用目标
- 构建所有组件: `nuke build`
- 运行测试: `nuke test`
- 打包插件: `nuke plugin`

## Pull Request 流程

1. 确保您的代码遵循项目的风格指南
2. 根据需要更新文档
3. 为新功能包含测试
4. 提交前确保所有测试通过
5. 在 PR 描述中链接任何相关议题
6. 等待项目维护者的审核

## 需要帮助？

如果您有任何问题，请随时开 issue 或加入我们的社区渠道。
