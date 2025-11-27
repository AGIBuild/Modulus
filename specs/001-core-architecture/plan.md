# Implementation Plan: Modulus 核心架构与双宿主运行时

**Branch**: `[001-core-architecture]` | **Date**: 2025-11-27 | **Spec**: `specs/001-core-architecture/spec.md`  
**Input**: Feature specification from `specs/001-core-architecture/spec.md`

**Note**: This plan is generated for the `/speckit.plan` workflow and aligned with the Modulus Constitution.

## Summary

本特性旨在为 Modulus 提供一个 UI 无关的核心架构与双宿主运行时，使模块能够以垂直切片方式开发，
在 Blazor 风格宿主与 Avalonia 原生宿主下复用同一套 Domain / Application 逻辑。
运行时将基于 `AssemblyLoadContext` 实现模块隔离与控制加载 / 卸载，通过 MediatR 处理模块间通信，
并通过统一的插件打包格式与 SDK 基类，为 AI 生成插件和人类开发者提供强类型扩展点。

Phase 1 (MVP) 聚焦于核心运行时、UI 抽象层、Web 风格宿主和最小可用 SDK；
Phase 2 (v1) 在此基础上补齐 Avalonia 宿主、完善 UI 抽象层、增强插件热重载与 SDK 能力。

## Technical Context

**Language/Version**: .NET 8 (LTS), C# (最新稳定版本；后续可评估升级到 .NET 9 Current)  
**Primary Dependencies**: `MediatR`, `Avalonia`, `Blazor` (Hybrid / WebView 宿主), `Microsoft.Extensions.DependencyInjection`, `System.Text.Json`  
**Storage**: N/A（本特性为框架级运行时与宿主，不绑定具体存储；业务模块可选择数据库 / 文件等）  
**Testing**: xUnit + 集成测试（基于宿主运行时的端到端测试）+ 针对 SDK 与 manifest 的契约测试  
**Target Platform**: Windows 10+ / 最新 macOS（支持 Avalonia 原生宿主）；后续可扩展到 Linux（Avalonia）  
**Project Type**: 桌面 / 工具框架（多项目解决方案，单仓库，多宿主 + 多模块）  
**Performance Goals**: 典型硬件上应用冷启动 < 3s，模块加载 / 卸载用户可感知延迟 < 2s；保持内存占用随模块数量线性可控  
**Constraints**: 核心层不得依赖具体 UI / Web 环境；插件加载 / 卸载需尽可能避免 `AssemblyLoadContext` 泄漏；必须符合宪章中金字塔分层与双宿主要求  
**Scale/Scope**: 初期包含 1–2 个内置模块 + 若干示例插件；解决方案预计包含若干 Core / Host / Modules / SDK 项目，后续可扩展到插件市场场景

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **UI-Agnostic Core**: Does the proposed feature keep Domain/Application code free of any
  concrete UI framework dependencies and use only `Modulus.UI.Abstractions` for UI contracts?
- **Dual-Engine Host Architecture**: Which hosts (Blazor, Avalonia) are targeted, and how will
  UI assemblies be separated from core logic so the same module can run under multiple hosts?
- **Vertical Slice Modularity**: Which module(s) own this feature as a vertical slice, and can
  they be loaded, tested, and versioned independently?
- **Pyramid Layering**: Do all dependencies follow
  Presentation → UI Abstraction → Application → Domain → Infrastructure, with no cross-layer
  shortcuts?
- **AI-Friendly Contracts & Plugin SDK**: What public contracts or SDK base types are required
  or changed, and how will they remain strongly typed and self-describing for AI and human
  authors?
- **Modern .NET & Technology Discipline**: Are the chosen runtime targets, MediatR usage, and
  portability requirements consistent with the Modulus Constitution?

Summarize any risks or intentional deviations here and link to the governance decision if
applicable.

本实现计划严格遵守宪章中关于 UI 无关核心、双宿主架构、垂直切片模块化与金字塔分层的约束：
核心项目仅依赖 `Modulus.UI.Abstractions`，宿主分别封装 Blazor 与 Avalonia 相关依赖；
模块以 `Modulus.Modules.<Name>.*` 命名的垂直切片组织，通过 DI 与 MediatR 解耦。

当前唯一需要在 Phase 0 研究阶段进一步细化的点是插件包的最终容器格式与签名方案
（例如 Zip‑based `.modpkg` vs 基于 NuGet 的打包变体），但这些选项在设计上均可保持
对宪章原则的兼容，不构成直接违反。

## Project Structure

### Documentation (this feature)

```text
specs/001-core-architecture/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── Modulus.Core/                 # 核心运行时与模块系统（ALC、模块发现、生命周期）
├── Modulus.UI.Abstractions/      # UI 抽象层接口与 DTO
├── Modulus.Sdk/                  # 模块 / 插件 SDK（基类、辅助类型）
├── Hosts/
│   ├── Modulus.Host.Blazor/      # Web 风格宿主（Blazor Hybrid / WebView 封装）
│   └── Modulus.Host.Avalonia/    # 原生桌面宿主（Avalonia）
├── Modules/
│   ├── Modulus.Modules.Shell/    # 核心 Shell / 菜单 / 宿主集成模块
│   ├── Modulus.Modules.Logging/  # 日志与诊断模块（示例）
│   └── Modulus.Modules.Samples/  # 示例模块集合（计算器等）
└── Shared/
    └── Modulus.Shared.Infrastructure/  # 共享基础设施实现（可选，避免业务逻辑泄漏）

tests/
├── Modulus.Core.Tests/           # 核心运行时与模块系统单测 / 集成测试
├── Modulus.Hosts.Tests/          # 宿主层集成测试（启动、模块加载、UI 钩子）
├── Modulus.Modules.Tests/        # 模块层单测 / 集成测试
└── Modulus.Sdk.Tests/            # SDK 契约与基类的契约测试
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

本计划采用“单仓库、多项目”的结构：以 `src/` 为根，按职责划分为 Core / UI.Abstractions / Hosts /
Modules / Sdk / Shared 若干项目目录，配套 `tests/` 目录按同样维度划分测试项目。
这种结构有利于：

- 清晰映射宪章中的分层与模块划分（例如 `Modulus.Modules.<Name>.*` 垂直切片）；
- 在同一解决方案中管理多个宿主与模块，方便跨项目引用与 CI 配置；
- 为后续 NuGet / `.modpkg` 打包提供稳定的项目边界。

备选方案包括：

- 以 `apps/` + `src/` 结构区分宿主应用与可复用库；
- 以 `packages/` 结构直接对标未来 NuGet 包划分；

当前选择上述 `src/` + `tests/` 结构作为 v1 基线，后续如需支持独立发布的宿主应用，
可以在根目录引入 `apps/` 目录承载打包与发布工程，而不改变 Core / Modules / Sdk 的结构。

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
