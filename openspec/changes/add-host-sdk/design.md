## Context

Modulus 以 `AssemblyLoadContext` 为核心实现模块隔离与卸载能力。其成功的关键在于：
- Host 与模块之间共享的“契约程序集”只能有 **一个副本**（Shared Domain）
- Host 提供的运行时环境（导航、菜单、服务、数据库、日志）需要可复用、可扩展，且不把第三方应用锁死在内部实现上
- 模块的兼容性必须可被机器验证（版本范围）并提供可诊断的失败信息

当前仓库已存在 Shared Domain 元数据与共享程序集目录（`AssemblyDomainInfo` / `SharedAssemblyCatalog` / `ModuleLoadContext`），但仍缺少“Host SDK 作为产品”的边界定义与版本策略约束。

本变更以 **MAUI Blazor Host** 为 Blazor 路线。

## Goals / Non-Goals

**Goals:**
- 定义 Host SDK 包结构与职责拆分（Avalonia 与 MAUI Blazor）
- 定义 Host SDK 的公共 API 边界（builder/options/扩展点），避免破坏性依赖
- 定义 Shared Domain 的“权威策略”，并明确 Host SDK 程序集属于 Shared Domain
- 定义版本策略（Host SDK ↔ Modulus.* core libs ↔ module manifests/templates）
- 定义可观测性与诊断输出，降低 ALC/版本问题排障成本

**Non-Goals:**
- 不在本 change 内实现 `modulus new app` 或 app 模板（后续 change 处理）
- 不在本 change 内引入新的模块清单格式（继续使用 `extension.vsixmanifest`）
- 不重写现有 runtime；仅在必要处补齐策略与扩展点

## Decisions

### Decision 1: Host SDK 的分层与包结构（契约优先）

**选择**：Host SDK 使用“契约层 + 组合层 + UI Host 层”三层拆分，且所有 Host SDK 程序集均声明为 Shared Domain。

**候选包（示例命名）**：
- `Agibuild.Modulus.Host.Abstractions`（Shared）：Host 契约（接口、options、扩展点）
- `Agibuild.Modulus.Host.Core`（Shared）：运行时组合（builder、默认服务注册、模块加载集成）
- `Agibuild.Modulus.Host.Avalonia`（Shared）：Avalonia Shell（默认 UI、资源、导航实现）
- `Agibuild.Modulus.Host.BlazorMaui`（Shared）：MAUI Blazor Shell（默认 UI、MudBlazor 依赖、宿主集成）

**约束**：
- 模块与 Host 的交互尽量落在 `Modulus.Sdk` / `Modulus.UI.Abstractions` / `Host.Abstractions`，避免直接依赖 Host 具体实现程序集。
- Host SDK 的 UI Host 层可以替换（应用可自定义 Shell/导航/菜单渲染），但默认实现可开箱即用。

### Decision 2: Shared Domain 策略必须“单一权威来源”

**问题**：当前“运行时共享判定”（`SharedAssemblyCatalog`）与“打包剔除策略”（CLI `PackCommand` 前缀过滤、Nuke `pack-module`）存在天然分叉风险。

**选择**：定义一个“Shared Assembly Policy”作为权威策略，并要求：
- 运行时共享判定与打包剔除都从同一份策略生成/读取
- 策略至少包含两类：
  - **显式程序集名**（用于高精度控制，来自 domain metadata + host config）
  - **前缀规则**（用于框架/生态程序集族，如 `Avalonia*`, `Microsoft.*`, `MudBlazor*`，避免枚举过长）

**落地建议**（实现阶段）：
- 扩展 `SharedAssemblyOptions` 支持 `Prefixes`
- `SharedAssemblyCatalog` 同时支持 Name 与 Prefix 判定
- CLI/Nuke 的“剔除共享程序集”逻辑复用同一策略（避免重复维护）

### Decision 3: 版本策略采用“Release Train + SemVer Range”

**选择**：
- Host SDK 与 `Agibuild.Modulus.*` 共享同一条 release train（同一 `Major.Minor`），在一个 train 内允许 patch 级别独立发布，但不得破坏兼容性。
- 模块兼容性由 `extension.vsixmanifest` 的 `InstallationTarget/@Version` 表达（NuGet SemVer range 语法）。
- 模板与 CLI 生成项目默认引用 **同一 train** 的依赖范围（避免浮动到下一 minor/major）。

**示例**：
- Host 版本 `1.4.2`
- 模块 `InstallationTarget Version="[1.4,1.5)"`（锁定 minor）
- 模板引用 `Agibuild.Modulus.Sdk` 版本 `1.4.*` 或 `[1.4.0,1.5.0)`（二选一；实现阶段择定一种并保持一致）

### Decision 4: MAUI Blazor Host 的构建与平台策略显式化

**选择**：
- `Agibuild.Modulus.Host.BlazorMaui` 视为“需要 MAUI 工具链”的产物；CI/本地开发必须明确前置条件（.NET MAUI workload + Xcode/Android 等）。
- 为避免在不具备 MAUI 工具链的平台上造成全仓 `restore/build` 失败，Host SDK 的构建目标需要可分离（例如：solution filter / nuke target 选择性构建 / 条件 TargetFrameworks）。

> 注意：这不是“绕开 MAUI”，而是把 MAUI 依赖显式化，避免把所有贡献者拖进工具链地狱。

## Risks / Trade-offs

| 风险 | 影响 | 缓解 |
|------|------|------|
| Host SDK API 暴露过多 | 升级困难、生态被锁死 | 契约层优先；实现细节 `internal`；仅 builder/options 暴露 |
| Shared Domain 策略分叉 | 类型不相等、资源加载失败、难排查 | 单一权威策略；运行时/打包共用；提供诊断服务 |
| MAUI Blazor 工具链复杂 | CI/开发门槛高，易阻塞 | 构建目标可分离；明确 CI agent；文档说明（后续 change） |
| 版本策略不严格 | 生态碎片化、模块加载失败 | 强制 manifest range 校验；模板使用同一 train；升级指引 |

## Migration Plan

1. 引入 `host-sdk` spec 与相关 spec deltas（本 change）
2. 后续 change：实现 Host SDK 包拆分与 builder API，并将现有 Host 迁移为“使用 SDK 的参考实现”
3. 后续 change：新增 app 模板（`modulus new app` / `dotnet new`）基于 Host SDK

## Open Questions

- 是否需要把 `Host.Abstractions` 作为模块可引用的“官方契约”之一？（倾向：是，但保持极小面）
- 模板版本策略选择 `1.4.*` 还是 `[1.4.0,1.5.0)`？（两者都可；前者更贴近 NuGet/SDK 风格，后者更严格）


