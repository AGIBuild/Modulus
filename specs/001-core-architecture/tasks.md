---

description: "Tasks for implementing Modulus 核心架构与双宿主运行时"
---

# Tasks: Modulus 核心架构与双宿主运行时

**Input**: Design documents from `specs/001-core-architecture/`  
**Prerequisites**: `plan.md` (required), `spec.md` (required), `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: 本特性涉及框架与运行时，建议为关键路径添加测试任务（单元测试 + 集成测试），但数量可随实现阶段调整。  
**Organization**: 任务按 User Story 分组，确保每一 Story 都可独立实现与验证。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可并行执行（不同文件、无依赖）
- **[Story]**: 任务归属的 User Story（US1, US2, US3）
- 描述中必须包含明确文件路径

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: 创建基础目录与项目结构，为后续实现提供落地点。

- [X] T001 创建基础目录结构 `src/` 与 `tests/`（如不存在）于仓库根目录  
      （修改路径：`D:\src\tools\Modulus\`）
- [X] T002 [P] 创建核心运行时项目 `src/Modulus.Core/Modulus.Core.csproj` 并将其加入 `Modulus.sln`  
      （修改文件：`Modulus.sln`）
- [X] T003 [P] 创建 UI 抽象层项目 `src/Modulus.UI.Abstractions/Modulus.UI.Abstractions.csproj` 并加入解决方案  
      （修改文件：`Modulus.sln`）
- [X] T004 [P] 创建 SDK 项目 `src/Modulus.Sdk/Modulus.Sdk.csproj` 并加入解决方案  
      （修改文件：`Modulus.sln`）
- [X] T005 [P] 在 `src/Hosts/` 下创建 Blazor 宿主项目 `Modulus.Host.Blazor/Modulus.Host.Blazor.csproj`  
      （修改文件：`Modulus.sln`）
- [X] T006 [P] 在 `src/Hosts/` 下创建 Avalonia 宿主项目 `Modulus.Host.Avalonia/Modulus.Host.Avalonia.csproj`  
      （修改文件：`Modulus.sln`）
- [X] T007 [P] 在 `src/Modules/` 下创建 Shell 模块项目 `Modulus.Modules.Shell/Modulus.Modules.Shell.csproj`  
      （修改文件：`Modulus.sln`）
- [X] T008 [P] 在 `src/Modules/` 下创建示例模块项目 `Modulus.Modules.Samples/Modulus.Modules.Samples.csproj`  
      （修改文件：`Modulus.sln`）
- [X] T009 [P] 创建测试项目 `tests/Modulus.Core.Tests/Modulus.Core.Tests.csproj` 并配置到 `Modulus.sln`  
- [X] T010 [P] 创建测试项目 `tests/Modulus.Hosts.Tests/Modulus.Hosts.Tests.csproj` 并配置到 `Modulus.sln`  
- [X] T011 [P] 创建测试项目 `tests/Modulus.Modules.Tests/Modulus.Modules.Tests.csproj` 并配置到 `Modulus.sln`  
- [X] T012 [P] 创建测试项目 `tests/Modulus.Sdk.Tests/Modulus.Sdk.Tests.csproj` 并配置到 `Modulus.sln`  

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: 完成所有 User Story 之前必须具备的核心运行时与架构基础。

**⚠️ CRITICAL**: 在本阶段完成前，不应开始任何具体 User Story 的业务实现。

- [X] T013 在 `src/Modulus.Core/` 中实现基础依赖注入与日志基础设施（使用 `Microsoft.Extensions.DependencyInjection` 和 logging）  
- [X] T014 在 `src/Modulus.Core/Runtime/` 下定义并实现核心实体：`Module`, `Host`, `PluginPackage`, `Manifest`, `RuntimeContext`（与 `data-model.md` 一致）  
- [X] T015 在 `src/Modulus.Core/Runtime/` 中实现基于 `AssemblyLoadContext` 的模块加载器（创建 / 卸载 ALC，加载模块程序集）  
- [X] T016 在 `src/Modulus.Core/Manifest/` 中实现 `.modpkg` 容器与 `manifest.json` 的解析逻辑（路径映射、支持宿主信息、依赖列表）  
- [X] T017 在 `src/Modulus.Core/Runtime/` 中集成 `MediatR`，配置模块级与跨模块请求 / 通知分发  
- [X] T018 在 `src/Modulus.UI.Abstractions/` 中定义 UI 抽象接口（`IUIFactory`, `IViewHost`, `INotificationService` 等）  
- [X] T019 在 `src/Modulus.Sdk/` 中引入基础 SDK 契约与空实现骨架（`ModuleBase`, `ToolPluginBase`, `DocumentPluginBase` 等）  
- [X] T020 在 `tests/Modulus.Core.Tests/` 中编写最小集成测试：验证加载一个 `.modpkg` 包能创建 `Module` 并注册到 `RuntimeContext`  
- [X] T021 [P] 在 `tests/Modulus.Sdk.Tests/` 中为 `ModuleBase` 与 `ToolPluginBase` 添加基本契约测试（生命周期调用顺序与必需回调）  
- [X] T022 检查并调整解决方案引用，确保遵守 `Presentation → UI Abstraction → Application → Domain → Infrastructure` 依赖金字塔（主要修改 `*.csproj`）  
- [X] T023 确认 `src/Modulus.Core/` 与 `src/Modulus.Sdk/` 不引用任何具体 UI 框架命名空间（Blazor / Avalonia），必要时通过分析与重构移除  

**Checkpoint**: 基础运行时、UI 抽象、SDK 骨架与分层依赖全部就绪，可以开始按 User Story 分阶段实现。

---

## Phase 3: User Story 1 - 一次开发，双宿主运行 (Priority: P1) 🎯 MVP

**Goal**: 实现一个示例垂直切片模块，使其在 Web 风格宿主与 Avalonia 宿主下共用同一套核心逻辑运行。

**Independent Test**: 构建一次示例模块的 Domain / Application 程序集，在不修改这些程序集的前提下，
分别运行 Blazor 宿主与 Avalonia 宿主，并验证示例功能在两者中行为一致。

### Implementation for User Story 1

- [X] T024 [P] [US1] 在 `src/Modules/Modulus.Modules.Samples/Domain/` 中实现示例模块的 Domain 模型与服务（例如计算器 / 简单工具）  
- [X] T025 [P] [US1] 在 `src/Modules/Modulus.Modules.Samples/Application/` 中实现用例与 Application 服务，依赖 Domain 模型与 `Modulus.UI.Abstractions`  
- [X] T026 [P] [US1] 在 `src/Modules/Modulus.Modules.Samples/UI.Blazor/` 下实现 Blazor 视图与适配层，通过 `IUIFactory` 与 Application 服务交互  
- [X] T027 [P] [US1] 在 `src/Modules/Modulus.Modules.Samples/UI.Avalonia/` 下实现 Avalonia 视图与适配层，通过 `IUIFactory` 与 Application 服务交互  
- [X] T028 [US1] 为示例模块编写 manifest 文件（例如 `src/Modules/Modulus.Modules.Samples/manifest.json`），填充模块标识、版本、支持宿主与程序集列表  
- [X] T029 [US1] 在 `src/Modulus.Core/Runtime/` 中接入示例模块的 manifest 与 `.modpkg` 打包，使宿主可发现并加载该模块  
- [X] T030 [US1] 在 `src/Hosts/Modulus.Host.Blazor/` 中添加示例模块入口（菜单 / 工具面板注册）并验证交互闭环  
- [X] T031 [US1] 在 `src/Hosts/Modulus.Host.Avalonia/` 中添加示例模块入口（窗口 / 面板注册）并验证交互闭环  
- [X] T032 [P] [US1] 在 `tests/Modulus.Hosts.Tests/` 中添加端到端测试：在两种宿主下分别加载并调用示例模块，验证行为一致  

**Checkpoint**: 示例模块在 Blazor 宿主与 Avalonia 宿主下均可用，且共享相同的核心业务程序集。

---

## Phase 4: User Story 2 - 运行时安全启用 / 禁用模块 (Priority: P1)

**Goal**: 支持在不重启应用的前提下安全启用、禁用与重新加载模块，并确保其它模块不受影响。

**Independent Test**: 在同一进程内重复对一个模块执行启用 / 禁用 / 重新加载操作多次，验证其它模块与宿主稳定性。

### Implementation for User Story 2

- [X] T033 [US2] 在 `src/Modulus.Core/Runtime/` 中扩展 `RuntimeContext` 与模块管理 API，支持 enable/disable/reload 操作  
- [X] T034 [US2] 在 `src/Modulus.Core/Runtime/` 中实现模块卸载时的清理逻辑（释放 ALC、注销 DI 注册等），避免资源泄漏  
- [X] T035 [US2] 在 `src/Hosts/` 各宿主中实现模块管理 UI（列表 / 状态 / 操作按钮），Shell 作为宿主内置组件  
- [X] T036 [P] [US2] 在 `src/Hosts/Modulus.Host.Blazor/` 中集成 Shell 模块的管理 UI（Modules 页面）  
- [X] T037 [P] [US2] 在 `src/Hosts/Modulus.Host.Avalonia/` 中集成 Shell 模块的管理 UI（ModuleListView）  
- [X] T038 [US2] 在 `tests/Modulus.Hosts.Tests/` 中添加端到端测试：对示例模块连续执行多次 enable/disable/reload，验证不崩溃且状态正确恢复  

**Checkpoint**: 模块管理 UI 与运行时 API 可协同完成模块启用 / 禁用 / 重新加载，并通过自动化测试验证稳定性。

---

## Phase 5: User Story 3 - 基于 SDK 的 AI 辅助插件开发 (Priority: P2)

**Goal**: 提供强类型 SDK 基类与最小示例，使 AI 可以基于这些基类生成可编译、可打包并可运行的插件。

**Independent Test**: 使用 SDK 基类为一个简单工具型插件生成代码（可由 AI 生成），在不调整整体结构的前提下完成编译、打包与加载。

### Implementation for User Story 3

- [X] T039 [US3] 在 `src/Modulus.Sdk/` 中完善 `ModuleBase`, `ToolPluginBase`, `DocumentPluginBase` 等基类的公共 API（生命周期、注册点、错误处理模式）  
- [X] T040 [US3] 在 `src/Modulus.Sdk/` 中添加用于生成 manifest 与 `.modpkg` 结构的辅助类型（例如 `PluginPackageBuilder`）  
- [X] T041 [US3] 在 `src/Modules/Modulus.Modules.Samples/` 下添加一个基于 SDK 的示例插件实现（例如 Echo 工具），严格遵循 SDK 模式  
      （实际位置：`src/Modules/EchoPlugin`）
- [X] T042 [P] [US3] 在 `tests/Modulus.Sdk.Tests/` 中添加契约测试：验证基于 SDK 的示例插件能够完成初始化与注册流程  
      （实际位置：`tests/Modulus.Modules.Tests/EchoPluginTests.cs`）
- [X] T043 [US3] 在 `specs/001-core-architecture/contracts/runtime-contracts.md` 中补充/更新与 SDK 相关的公共接口说明，使其与代码保持一致  
- [X] T044 [US3] 更新 `specs/001-core-architecture/quickstart.md`，加入“使用 SDK 创建第一个插件”的简要步骤  

**Checkpoint**: 存在至少一个通过 SDK 开发的示例插件，可以作为 AI 生成插件的参考模板，并通过测试验证。

---

## Phase N: Polish & Cross-Cutting Concerns

**Purpose**: 面向整体架构与开发体验的收尾与跨模块优化。

- [X] T045 [P] 完成对 `specs/001-core-architecture/` 下所有文档的最终对齐（spec, plan, data-model, quickstart, contracts）  
- [X] T046 [P] 在 `CONTRIBUTING.md` 与 `CONTRIBUTING.zh-CN.md` 中补充 Modulus 宪章与模块 / 宿主架构的简要说明  
- [X] T047 [P] 在 `README.md` 与 `README.zh-CN.md` 中加入对多宿主与插件化架构的简要介绍与链接  
- [X] T049 整体代码清理与重构（命名统一、命名空间与分层依赖检查、移除临时代码）  
- [X] T050 运行完整测试集（`dotnet test`），修复发现的问题并记录后续 Story（如需要拆分 v2 功能）  

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 无前置依赖，优先完成，用于搭建项目骨架。  
- **Foundational (Phase 2)**: 依赖 Phase 1，完成后才具备实现任意 User Story 的基础。  
- **User Stories (Phase 3–5)**: 都依赖 Foundational 阶段完成；US1/US2/US3 可部分并行，但建议按优先级顺序交付：  
  - US1（垂直切片示例）→ US2（运行时模块管理）→ US3（SDK 与 AI 插件）。  
- **Polish (Final Phase)**: 在计划交付的 User Story 全部完成后执行。

### User Story Dependencies

- **User Story 1 (P1)**: 仅依赖 Foundational，提供可运行的端到端示例模块，是整体架构的 MVP 验证。  
- **User Story 2 (P1)**: 依赖 US1 提供的示例模块（用于模块管理验证），也依赖 Foundational 的运行时 API。  
- **User Story 3 (P2)**: 依赖 Foundational 的 SDK 骨架与 US1/US2 的基础体验，用于强化 AI 与 SDK 的协同。

### Within Each User Story

- US1: 先实现核心 Domain / Application，再实现 UI 适配与 manifest，最后完成两种宿主的集成与端到端测试。  
- US2: 先扩展运行时 API，再实现 Shell 模块的管理 UI，最后在两个宿主中集成并通过自动化测试验证。  
- US3: 先稳定 SDK 基类，再添加示例插件与测试，最后更新文档与 Quickstart。

### Parallel Opportunities

- Setup 阶段中不同项目的创建任务可以并行（T002–T012）。  
- Foundational 阶段中 MediatR 集成、UI 抽象定义与 SDK 骨架实现可以在不互相阻塞的前提下并行。  
- US1 中 Blazor 与 Avalonia UI 适配实现（T026, T027）可以在核心逻辑稳定后并行推进。  
- US2 中 Blazor / Avalonia 的 Shell 集成（T036, T037）可以并行。  
- Polish 阶段中的文档与 AI 上下文更新任务（T045–T048）可以并行执行。

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. 完成 Phase 1: Setup（项目与目录结构搭建）。  
2. 完成 Phase 2: Foundational（核心运行时 / UI 抽象 / SDK 骨架）。  
3. 完成 Phase 3: User Story 1（示例模块 + 双宿主集成）。  
4. 在两种宿主下验证示例模块端到端运行，满足 SC-001。  
5. 视需要发布首个开发者预览版本。

### Incremental Delivery

1. 在 MVP 完成后，引入 User Story 2（模块启用 / 禁用 / 重新加载）并验证稳定性。  
2. 在核心稳定后引入 User Story 3（SDK 与 AI 插件），为后续插件生态奠定基础。  
3. 每完成一个 Story，即可单独进行演示与反馈收集。

### Parallel Team Strategy

在多人协作场景下：

1. 团队共同完成 Setup 与 Foundational 阶段。  
2. Foundational 完成后：
   - 开发者 A 负责 US1（示例模块与双宿主集成）；  
   - 开发者 B 负责 US2（运行时模块管理与 Shell 集成）；  
   - 开发者 C 负责 US3（SDK 与示例插件 + 文档）。  
3. 通过统一的测试与文档收敛，确保三个 Story 在合并后仍然符合宪章与架构约束。


