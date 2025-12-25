## Context

Modulus 的 Host 需要完成两件事：
- **宿主组合（Composition）**：配置 DB、模块目录、日志、安装/投影、模块加载与延迟激活，并将 Host 的服务容器与模块容器绑定（当前以 `ModulusApplicationFactory` 为核心）
- **UI Shell**：导航/菜单/主题/视图承载（Avalonia 与 Blazor Hybrid(MAUI) 各自有实现）

目前仓库已经有可工作的 runtime 组合与 shared assemblies 诊断（`SharedAssemblyCatalog` / `SharedAssemblyDiagnosticsService`），但第三方应用要复用这些能力仍需要复制 Host 源码；同时 `modulus pack` 的共享程序集剔除策略与 runtime 的 shared 判定并非同一来源，存在长期漂移风险。

本变更选择方案 A：把宿主组合能力做成 **Host SDK（组合层 + 可选默认 Shell）**，并把 shared-assembly 策略收敛成一条主线。

## Goals / Non-Goals

**Goals:**
- 定义 Host SDK 的边界：对外提供稳定的宿主组合 API（围绕 `ModulusApplicationFactory`），避免复制 Host 源码
- 定义 Host SDK 的公共 API 形态（builder/options + 显式扩展点），并约束稳定性边界（最小 public surface）
- 定义 canonical shared-assembly policy，使 runtime shared resolution 与 packaging shared exclusion 使用同一份规则与诊断模型
- 保持 Avalonia / Blazor Hybrid(MAUI) 两条 Host 路线都可用，并明确 MAUI 工具链与跨平台构建约束

**Non-Goals:**
- 不在本 change 内切换模块模板到 NuGet 版本策略（当前 `modulus new` 通过 `ModulusCliLibDir` 引用 CLI 随附 dll）
- 不在本 change 内引入新的模块清单格式（继续使用 `extension.vsixmanifest`）
- 不在本 change 内变更 Host Id 命名（如 `Agibuild.Modulus.Host.*`），该方向由独立 change 处理

## Decisions

### Decision 1: Host SDK = Composition layer + Optional default shells

**选择**：Host SDK 明确分为两层：
- **Composition layer（必须）**：提供“创建/初始化 `IModulusApplication`”的稳定入口，复用 `ModulusApplicationFactory` 的能力，并将 Host 需要的关键参数（host id/version、模块目录、DB 路径、配置、日志）收口为 options/builder。
- **Default shells（可选）**：提供可开箱即用的 Avalonia / Blazor Hybrid(MAUI) 默认 Shell，但允许应用替换 UI 实现。

**候选包（示例命名，最终以仓库命名规范为准）**：
- `Agibuild.Modulus.Host.Abstractions`（Shared）：Host SDK 的契约（options、builder、扩展点接口）
- `Agibuild.Modulus.Host.Runtime`（Shared）：宿主组合层（基于 `ModulusApplicationFactory`）
- `Agibuild.Modulus.Host.Avalonia`（Shared，可选）：默认 Avalonia Shell
- `Agibuild.Modulus.Host.Blazor`（Shared，可选）：默认 Blazor Hybrid(MAUI) Shell

**边界原则**：
- Host SDK 对外只暴露 builder/options 与显式扩展点，内部实现类型保持 `internal`。
- 模块与 Host 的交互优先落在 `Modulus.Sdk` / `Modulus.UI.Abstractions`（以及必要的 Host Abstractions），避免依赖 Shell 具体实现。

### Decision 2: Canonical shared-assembly policy uses exact names (no prefixes in this change)

**问题**：当前 runtime 的 shared 判定基于 `SharedAssemblyCatalog`（domain metadata + `Modulus:Runtime:SharedAssemblies` + diagnostics），而 `modulus pack` 通过硬编码前缀列表剔除共享程序集；两者容易分叉。

**选择**：定义 canonical shared-assembly policy，并在本 change 内将其约束为：
- **策略主体是“程序集简单名（simple name）集合”**（不包含扩展名/版本）
- **来源**：
  - domain metadata（`[AssemblyDomain]` / `ModulusAssemblyDomain`）
  - host configuration（`Modulus:Runtime:SharedAssemblies`）
  - diagnostics 记录每条 entry 的来源与冲突
- **消费者**：
  - runtime（`ModuleLoadContext` shared resolution）
  - packaging（`modulus pack` / `nuke pack-module` shared exclusion）

> 本 change **不引入** prefix/pattern 规则扩展（例如 `Avalonia*`、`System.*`）。若未来确有必要，可另起 change 扩展 `SharedAssemblyOptions` 与 `SharedAssemblyCatalog`。

### Decision 3: Compatibility = InstallationTarget SemVer Range + Host-provided version

**选择**：
- 模块与 Host 的兼容性继续由 `extension.vsixmanifest` 的 `InstallationTarget/@Version` 表达（NuGet `VersionRange` 语法）。
- Host SDK MUST 要求宿主在启动时提供一个可被 `NuGetVersion` 解析的 Host 版本（当前仓库通过程序集版本完成，并在 runtime 中做 range 校验与诊断）。
- 本 change 不定义“release train”作为模板/包引用策略（因为模板目前不走 NuGet）；只规范“Host 版本可诊断、可校验”。

### Decision 4: MAUI toolchain constraints remain explicit and separable

**选择**：
- Blazor Host 路线明确为 **Blazor Hybrid(MAUI)**，并要求构建目标可分离，避免无 MAUI 工具链的平台被全仓阻塞。
- Host SDK 的默认 Shell 若依赖 MAUI，则必须有清晰的构建条件与 CI agent 策略（实现阶段落地）。

## Risks / Trade-offs

| 风险 | 影响 | 缓解 |
|------|------|------|
| Host SDK API 暴露过多 | 升级困难、生态被锁死 | 最小 public surface；只暴露 builder/options 与显式扩展点；实现类型 `internal` |
| shared policy 与 packaging 仍分叉 | 类型不相等、资源加载失败、难排查 | canonical policy 单一来源；packaging 复用同一策略与 diagnostics |
| MAUI 工具链复杂 | CI/开发门槛高，易阻塞 | 构建目标可分离；明确构建条件与 CI agent（实现阶段） |
| Host 版本不可解析/不可诊断 | range 校验失效、排障困难 | Host SDK 强制 host version 供给；失败时输出可操作诊断 |

## Migration Plan

1. 本 change：引入 `host-sdk` spec 与 runtime/module-packaging 的 shared policy deltas
2. 实现阶段：新增 Host SDK（composition layer），并把现有 Host 迁移为“使用 SDK 的参考实现”
3. 实现阶段：让 `modulus pack` / `nuke pack-module` 复用 canonical shared policy（消除硬编码分叉）

## Open Questions

- Host SDK 的默认 Shell 是否要保证“完全开箱即用”还是仅提供参考实现？（倾向：默认实现可用，但允许替换）
- Host Id 命名（`Modulus.Host.*` vs `Agibuild.Modulus.Host.*`）如何与独立的重命名 change 协调？（本 change 不变更 Host Id）


