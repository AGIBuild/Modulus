# Feature Specification: Modulus 核心架构与双宿主运行时

**Feature Branch**: `[001-core-architecture]`  
**Created**: 2025-11-27  
**Status**: Draft  
**Input**: User description: "Design the core architecture and dual-host runtime for the Modulus modular .NET desktop framework."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - 一次开发，双宿主运行 (Priority: P1)

作为模块 / 插件开发者，我可以只围绕 UI 无关的核心（Domain + Application）和统一 SDK 实现一个模块，
并在不修改业务代码的前提下，让同一套核心逻辑分别在 Web 风格宿主和原生桌面宿主下运行。

**Why this priority**: 这是 Modulus 的核心价值主张：在不同 UI 环境下复用一套业务逻辑，降低开发与维护成本。

**Independent Test**: 实现一个简单的垂直切片示例模块（例如计算器或日志查看器），验证其
Domain / Application 程序集在 Blazor 风格宿主与 Avalonia 宿主下完全相同，仅通过不同 UI 程序集
即可完成两端运行。

**Acceptance Scenarios**:

1. **Given** 一个包含 Domain / Application 程序集以及对应 UI 程序集的模块  
   **When** 将该模块加载到 Web 风格宿主中  
   **Then** 该功能可以在该宿主内完整使用，行为与预期一致，UI 流程可用。

2. **Given** 相同的核心程序集  
   **When** 将该模块加载到原生桌面宿主中  
   **Then** 在不修改 Domain / Application 代码的前提下，功能同样可用，行为等价（样式可以不同但体验应一致）。

---

### User Story 2 - 运行时安全启用 / 禁用模块 (Priority: P1)

作为框架维护者或高级用户，我可以在不重启整个应用的前提下，在运行时启用、禁用和重新加载模块 / 插件，
并保证其它已加载模块不会因此失效。

**Why this priority**: 运行时可插拔与安全边界是构建可扩展工具平台、支持热重载与快速迭代的基础能力。

**Independent Test**: 启动宿主加载一组模块，然后在同一进程内多次重复对某一个模块执行启用 / 禁用 / 重新加载操作，
验证其它模块始终正常工作、宿主稳定不崩溃。

**Acceptance Scenarios**:

1. **Given** 宿主当前已加载多个模块  
   **When** 通过运行时管理接口禁用或卸载某个模块  
   **Then** 该模块的 UI 与行为会被干净移除，其它模块继续正常工作且无异常。

2. **Given** 同一宿主会话  
   **When** 重新启用或重新加载之前被禁用的模块  
   **Then** 该模块重新出现，其初始化逻辑正常执行，不会破坏共享状态或其它模块。

---

### User Story 3 - 基于 SDK 的 AI 辅助插件开发 (Priority: P2)

作为使用 AI 助手的开发者（或 AI Agent 本身），我可以基于一组强类型的 SDK 基类与清晰契约，
自动生成一个简单插件或模块，使其在无需手工调整整体结构的情况下即可编译、打包并在运行时加载执行。

**Why this priority**: 对 AI 友好的契约能让 Modulus 成为快速生成工具与自动化插件的平台。

**Independent Test**: 使用官方 SDK 基类与示例模版，通过 AI 生成一个简单的 “echo” 或 “calculator” 插件，
然后按标准流程编译、打包并加载到运行时中完成端到端验证。

**Acceptance Scenarios**:

1. **Given** 官方 SDK 基类与示例模版  
   **When** AI Agent 按照模版生成一个新的插件实现  
   **Then** 插件在不修改结构的前提下可以成功编译，并能按定义的打包格式打包。

2. **Given** 生成的插件包  
   **When** 宿主通过标准发现机制加载该插件  
   **Then** 插件会出现在宿主 UI 中，可被调用，并按照契约定义的行为正确执行。

---

### Edge Cases

- 当插件或模块 manifest 缺失必需字段、字段无效或引用的程序集无法加载时，系统应该如何处理？
- 当某个模块在初始化阶段失败（例如启动代码抛异常），但其它模块已成功加载时，系统如何隔离与降级？
- 当某个模块被两个宿主请求加载，但其只提供了单一宿主的 UI 程序集时（例如仅提供 Blazor UI），系统应该如何降级或发出提示？

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: 框架必须提供一个模块运行时，用于发现、加载、启用、禁用和卸载模块 / 插件，
  在常见路径下不需要重启整个应用。

- **FR-002**: 各模块的 Domain 与 Application 层必须是 UI 无关的，只能依赖
  `Modulus.UI.Abstractions` 等 UI 抽象层契约，而不能直接引用具体 UI 框架（如 Blazor、Avalonia 等）。

- **FR-003**: 架构必须支持至少两种一等宿主类型：Web 风格宿主与原生桌面宿主，使相同的
  Domain / Application 程序集可以在两种宿主下运行。

- **FR-004**: 系统必须以垂直切片模块为主要交付单元，每个功能模块需要明确自身的
  Domain / Application / Infrastructure 以及（可选的）Presentation / UI 程序集。

- **FR-005**: 运行时必须通过 MediatR（或等价的强类型进程内消息机制）处理模块之间的通信，
  禁止通过直接依赖其它模块实现类的方式进行耦合。

- **FR-006**: 必须设计一种插件 / 模块打包格式（概念上类似 `.modpkg` 容器），其中包含：
  - 描述标识、版本、依赖、支持宿主等信息的 manifest；
  - 一个或多个核心程序集（Domain / Application / Infrastructure）；
  - 每种宿主类型对应的 0~N 个 UI 程序集。

- **FR-007**: 运行时必须支持基于 `AssemblyLoadContext`（或等价机制）的隔离，
  使模块 / 插件可以在一定程度上独立加载与卸载，并尽量减少对其它模块的影响。

- **FR-008**: 框架必须提供强类型的 SDK 基类与接口，用于模块与插件开发
  （例如工具型插件、文档型插件、模块基类等），在其中编码推荐的初始化、生命周期和 UI 注册模式。

- **FR-009**: 在 **Phase 1 (MVP)** 范围内，架构至少需要交付：
  - 核心运行时与模块系统；
  - UI 抽象层；
  - 一种 Web 风格宿主（如 Blazor-based）及至少一个端到端示例模块；
  - 一套最小可用的插件 SDK，使简单插件可以被 AI 辅助生成。

- **FR-010**: 在 **Phase 2 (v1)** 中，架构需要在 Phase 1 基础上：
  - 增加第二种宿主类型（如 Avalonia 原生宿主）；
  - 将 UI 抽象层完善为可在两种宿主间一致工作的契约；
  - 支持在进程内的插件热重载 / 卸载（在合理约束前提下）；
  - 扩展 SDK 能力以支持更复杂的插件形式。

- **FR-011**: 打包与 manifest 模型必须从设计上支持版本化，以便将来增加新字段或能力时，
  不会破坏现有插件。

- **FR-012**: 架构必须明确划分宿主与模块的职责边界：宿主负责外部环境（窗口、导航、菜单等），
  模块负责业务逻辑、UI 契约与插件行为。

- **FR-013**: 必须确定插件包的具体容器格式与签名方案（例如 Zip‑based `.modpkg`、NuGet 变体等），
  并说明如何校验插件的来源可信与内容完整。
  - **FR-013a**: System MUST support a concrete packaging and signing strategy for plugins.
    [NEEDS CLARIFICATION: Choose final container format and signing approach.]

### Key Entities *(include if feature involves data)*

- **Module（模块）**: 表示一个垂直切片功能，可能包含 Domain、Application、Infrastructure 以及可选的
  Presentation / UI 程序集，具有唯一标识、版本和支持宿主等元数据。

- **Host（宿主）**: 提供环境相关能力（例如窗口、导航、顶层菜单）的外壳应用，根据模块元数据加载模块。
  至少包括 Web 风格宿主与原生桌面宿主两种。

- **PluginPackage（插件包）**: 一个可部署与发现的制品（例如 `.modpkg`），内部包含 manifest、
  核心程序集、可选的各宿主 UI 程序集以及资源文件，是外部插件的发布单位。

- **UIAbstractionContract（UI 抽象契约）**: 一组接口与 DTO（例如 View 工厂、View 宿主、消息契约等），
  用于描述模块如何表达 UI 意图，而不直接依赖具体 UI 框架。

- **SDKBaseType（SDK 基类）**: 面向模块与插件开发提供的强类型基类与辅助类型，
  在其中固化推荐模式，简化人类与 AI 的开发体验。

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 至少有一个示例垂直切片模块可以在仅构建一次
  Domain / Application / Infrastructure 程序集的前提下，分别在两种宿主下成功运行，
  且这两种运行方式之间不需要修改这些核心程序集。

- **SC-002**: 在一轮测试会话中，对同一个模块执行启用、禁用与重新加载操作至少 100 次，
  无需重启应用，且其它已加载模块始终保持正常工作。

- **SC-003**: 至少有一个通过 AI 辅助、基于官方 SDK 基类自动生成的简单插件可以：
  - 在不调整整体结构的前提下成功编译；
  - 按指定打包格式打包；
  - 被运行时加载并完成端到端调用。

- **SC-004**: 在目标环境的典型硬件配置下，加载或卸载单个模块的用户可感知延迟在合理范围内
  （例如常规场景下可在 2 秒内完成，使用户感觉“几乎即时”）。

## Architecture & Modulus Constraints *(mandatory for this repository)*

### Module & Host Mapping

- Owning module(s) (vertical slice):
  - `Modulus.Core`（核心运行时与模块系统）
  - `Modulus.UI.Abstractions`（UI 抽象契约）
  - `Modulus.Host.Blazor`（Web 风格宿主模块）
  - `Modulus.Host.Avalonia`（原生桌面宿主模块）
  - `Modulus.Sdk`（模块 / 插件 SDK）

- Target host(s):
  - Web-style host（基于 Blazor 的宿主）
  - Native desktop host（基于 Avalonia 的宿主）

- UI assemblies involved (if any):
  - 宿主级 UI 程序集（如 `Modulus.Host.Blazor.UI`, `Modulus.Host.Avalonia.UI`）
  - 示例模块 UI 程序集（如 `ExampleModule.UI.Blazor.dll`, `ExampleModule.UI.Avalonia.dll`）

### Layering & Dependencies

- Layers touched by this feature:
  - Presentation
  - UI Abstraction
  - Application
  - Domain
  - Infrastructure

- Any required cross-module communication (via MediatR or explicit interfaces):
  - 模块发现、加载 / 卸载与状态通知通过 MediatR 请求 / 通知完成；
  - 宿主向模块发送与导航、窗口 / 工具窗口创建、环境事件相关的消息；
  - 模块通过 UI 抽象层发布 UI 意图（如打开视图、显示通知），而不是直接操作具体 UI 框架。

- Confirm there are no planned violations of the dependency pyramid:
  - Domain / Application 项目不得引用宿主特定 UI 框架或环境特定 API；
  - Presentation 与宿主项目可以依赖 UI 框架，但只能通过 UI 抽象层契约、Application 服务与 MediatR
    与核心逻辑交互；
  - 若存在任何计划性例外，必须记录在架构说明中，并关联到宪章治理决策。

### Public Contracts & SDK Impact

- New or changed public contracts / DTOs:
  - 模块与插件 manifest 模型（标识、版本、能力、支持宿主等）；
  - UI 抽象接口（如 View 工厂、View 宿主、消息契约）；
  - 模块生命周期契约（初始化、启动、停止、释放等）。

- New or updated SDK base types (if any):
  - 垂直切片模块基类；
  - 工具 / 文档类插件基类，用于在两个宿主下进行 UI 集成；
  - 打包与 manifest 辅助类型，统一插件自描述方式。

- Backward compatibility and migration notes:
  - 本规格定义了 Modulus 核心架构与双宿主运行时的初始基线；
  - 公共契约与 manifest 必须支持版本化，以便未来引入
    进程外插件、更多宿主类型等能力时不破坏现有 v1 模块 / 插件；
  - 任何后续破坏性变更必须遵循 Modulus 宪章中的版本管理与迁移策略要求，
    并同步更新面向 AI 的 SDK 文档与示例。