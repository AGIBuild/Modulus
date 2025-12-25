## Context

当前仓库已经有可运行的 Host（`Modulus.Host.Avalonia` / `Modulus.Host.Blazor`），但它们仍是仓库内的一方应用。为了让第三方应用“像创建模块一样”创建插件式应用，我们需要把 Host 项目的设计收敛为可模板化的形态：
- Host 的运行时组合（DB、模块目录、安装/投影、模块加载、Host 绑定）必须通过 **Host SDK** 完成，避免用户复制 Host 源码
- shared assemblies 策略必须可维护：对框架程序集族（Avalonia/MAUI/MudBlazor 等）使用 prefix/pattern 规则而不是长名单
- 打包与加载必须一致：packaging 不能维护另一套“共享剔除前缀表”

## Goals / Non-Goals

**Goals:**
- 提供 CLI Host App 模板：`modulus new avaloniaapp` / `modulus new blazorapp`
- 提供 Visual Studio 向导模板：创建同等结构与行为的 Host App 项目
- 明确 Host App 项目设计契约：Host 只负责 UI 与宿主入口，运行时组合统一走 Host SDK
- shared policy 支持 prefix/pattern，并可被 runtime 与 packaging 共同使用

**Non-Goals:**
- 不在本 change 内实现 Host SDK（由 `add-host-sdk` 负责）
- 不在本 change 内要求模块模板切换到 NuGet 版本策略（模块模板目前仍通过 `ModulusCliLibDir`）
- 不在本 change 内改动 Host Id 命名体系（是否引入 `Agibuild.Modulus.Host.*` 由独立 change 决定）

## Decisions

### Decision 1: CLI surface = `modulus new <template>` with `avaloniaapp` / `blazorapp`

**选择**：
- 在 `modulus new` 下新增模板标识符：
  - `avaloniaapp`：生成 Avalonia Host App
  - `blazorapp`：生成 Blazor Hybrid(MAUI) Host App
- 保持现有模块模板不变：`module-avalonia` / `module-blazor`

**约束**：
- `modulus new --list` 必须列出所有可用模板（至少包含上述 4 个）
- `modulus new` 的默认模板仍为模块模板（避免破坏已有用户习惯）

### Decision 2: Host App generated project MUST be Host-SDK-driven (composition)

**选择**：Host App 模板生成的项目必须满足：
- Host 入口仅负责：
  - 构建配置与日志
  - 选择 Host 类型（Avalonia / Blazor Hybrid）
  - 调用 Host SDK builder/options 创建 `IModulusApplication`
- 模块目录、DB 路径、安装/投影、模块加载、Host 绑定等逻辑必须由 Host SDK（composition layer）提供

**目的**：保证模板产物升级路径清晰（升级 Host SDK 即升级组合能力），且应用不会被仓库内 Host 源码绑死。

### Decision 3: Canonical shared policy supports prefix/pattern rules (方案 1)

**选择**：canonical shared-assembly policy 由两类规则组成：
- **Exact names**：程序集简单名（不含扩展名/版本）
- **Prefixes/Patterns**：前缀匹配规则（例如 `Avalonia`、`Microsoft.Maui.`、`MudBlazor`）

**配置建议**（实现阶段落地）：
- `Modulus:Runtime:SharedAssemblies`：exact names 列表
- `Modulus:Runtime:SharedAssemblyPrefixes`：prefix/pattern 列表（以“前缀匹配”为语义）

### Decision 4: Packaging derives policy from InstallationTarget host set

**选择**：
- packaging（`modulus pack` / `nuke pack-module`）在打包时读取模块清单 `Installation/InstallationTarget`
- 根据目标 Host 集合选择对应的 shared policy presets，并取并集（例如模块同时支持 Avalonia+Blazor，则取两者 policy 并集）
- 再叠加 host config / domain metadata 的 exact entries（以 canonical policy 为准）

**目的**：让模块打包行为与运行时加载一致，并在“多 Host 兼容模块”情况下保持安全（宁可剔除更多 shared assemblies，也不要打进去导致重复加载）。

## Risks / Trade-offs

| 风险 | 影响 | 缓解 |
|------|------|------|
| 模板与 Host SDK 演进不同步 | 生成项目失效/难升级 | 模板强制只调用 Host SDK 的稳定入口；为模板生成添加可编译验收 |
| prefix/pattern 规则过宽 | 误判 shared，导致模块无法加载私有依赖 | 规则仅用于“框架程序集族”；输出 diagnostics 显示命中规则与来源 |
| 多 Host 并集剔除过多 | 包体更小但可能缺少某些 dll | 优先保证不重复加载；缺失由依赖恢复/构建输出确保 |

## Open Questions

- `blazorapp` 是否明确指向 “Blazor Hybrid(MAUI)”（当前仓库现状），还是未来也支持纯 Blazor Server/WASM Host？（本 change 以 Hybrid(MAUI) 为准）


