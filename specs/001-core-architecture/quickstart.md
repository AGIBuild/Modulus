# Quickstart: Modulus 核心架构与双宿主运行时

**Feature**: `001-core-architecture`

本 Quickstart 面向框架维护者与模块 / 插件开发者，说明在新的核心架构下如何开始工作。

---

## 1. 解决方案结构预期

在仓库根目录下，确保存在并逐步完善以下结构（参考 `plan.md` 中的 Project Structure）：

```text
src/
├── Modulus.Core/
├── Modulus.UI.Abstractions/
├── Modulus.Sdk/
├── Hosts/
│   ├── Modulus.Host.Blazor/
│   └── Modulus.Host.Avalonia/
├── Modules/
│   ├── Modulus.Modules.Shell/
│   ├── Modulus.Modules.Logging/
│   └── Modulus.Modules.Samples/
└── Shared/
    └── Modulus.Shared.Infrastructure/

tests/
├── Modulus.Core.Tests/
├── Modulus.Hosts.Tests/
├── Modulus.Modules.Tests/
└── Modulus.Sdk.Tests/
```

---

## 2. 新建一个垂直切片模块的基本步骤

1. 在 `src/Modules/` 下创建新模块目录（例如 `Modulus.Modules.Calculator/`），并在解决方案中添加对应项目。  
2. 在该模块中拆分层次（如需要）：`Domain`, `Application`, `Infrastructure`, 以及可选的
   `UI.Blazor`, `UI.Avalonia` 项目，遵守金字塔分层依赖方向。  
3. 在 Domain / Application 中仅依赖 `Modulus.UI.Abstractions` 与 `Modulus.Sdk`，避免任何具体 UI 引用。  
4. 在 UI 子项目中根据宿主实现具体视图与绑定逻辑，通过 UI 抽象层与核心交互。  
5. 为模块编写 manifest（将在后续实现中标准化位置与格式），描述模块标识、版本、支持宿主与程序集列表。  
6. 在 `tests/Modulus.Modules.Tests/` 中为该模块添加单元测试与必要的集成测试。

---

## 3. 运行时与宿主验证路径

1. 启动 Web 风格宿主（Blazor Host），验证：
   - 宿主能够发现并加载核心模块与示例模块；
   - 示例模块在 UI 中可用（菜单 / 工具面板 / 命令等）。  
2. 启动 Avalonia 宿主，重复上述验证：
   - 使用相同的核心程序集；
   - 使用专门的 Avalonia UI 程序集；  
3. 使用测试项目（`Modulus.Hosts.Tests` 等）运行基础端到端测试，确保模块加载 / 卸载流程稳定。

---

## 4. AI 辅助开发的推荐路径

1. 在开始使用 AI 生成代码前，运行 `nuke StartAI` 生成最新的项目上下文，并将宪章与本特性相关文档
   （`spec.md`, `plan.md`, `data-model.md`, `quickstart.md`）一并提供给 AI。  
2. 在为新插件 / 模块生成代码时，优先让 AI 基于 SDK 基类（如未来的 `ToolPluginBase`、
   `DocumentPluginBase` 等）生成骨架代码，而非随意创建项目结构。  
3. 对 AI 生成的模块 / 插件，按照本 Quickstart 与宪章中的分层与宿主约束进行审查与调整。

---

## 5. 后续演进

- Phase 1 完成后，应有至少一个端到端示例模块在两种宿主下运行；
- Phase 2 将在此基础上加入更完善的插件热重载、签名与市场化能力，并可能拆出更多特性级 spec 与 plan。


