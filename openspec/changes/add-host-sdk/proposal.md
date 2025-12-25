# Change: 引入 Host SDK（基于 ModulusApplicationFactory 的宿主组合层 + shared-assembly 策略收敛）

## Why

当前仓库的 Host（`Modulus.Host.Avalonia` / `Modulus.Host.Blazor`）仍是“参考实现 + 产品化壳”的混合体：第三方应用如果要构建自己的 Modulus 宿主，现实路径往往是复制/改造 Host 源码，导致：
- 宿主启动/运行时组合逻辑被复制（DB、模块目录、日志、安装/投影、模块加载初始化等）
- 对 Host 内部实现类型形成依赖，升级时容易被破坏性变更卡死

与此同时，模块隔离（ALC）对 **shared assemblies** 极其敏感：运行时的共享程序集判定（`SharedAssemblyCatalog` + `Modulus:Runtime:SharedAssemblies`）与打包时的剔除策略（`modulus pack` 的硬编码前缀列表）目前存在分叉风险，容易造成：
- `.modpkg` 携带了本应共享的程序集 → 运行时重复加载 → 类型不相等/资源加载失败
- 排障困难（“为什么同名程序集被加载了两份”）

因此需要把“宿主组合能力”产品化为 **Host SDK**，并明确一条单一的 shared-assembly 策略主线，让 runtime 与 packaging 不再各自维护一套规则。

## What Changes

- **ADDED** `host-sdk` capability（OpenSpec），定义：
  - Host SDK 的职责边界：以 `ModulusApplicationFactory` 为核心的“宿主组合层”（Composition）
  - Host SDK 的公共 API 边界：builder/options + 显式扩展点（避免依赖 Host 内部实现）
  - Host SDK 的 UI host 层为可选默认实现（Avalonia / Blazor Hybrid(MAUI)），允许应用替换 Shell
- **ADDED** requirements to `runtime` capability：
  - 定义“canonical shared-assembly policy”：共享程序集集合的权威来源与算法（domain metadata + host config + diagnostics）
  - 明确该策略需要同时服务于：运行时 shared resolution 与打包时 shared exclusion（避免分叉）
  - 明确 Host SDK 程序集必须处于 Shared domain，并在模块加载时被视为 shared
- **ADDED** requirements to `module-packaging` capability：
  - 明确 `modulus pack` 与 `nuke pack-module` 的共享程序集剔除必须由同一 canonical policy 驱动

> 注：本 change **不修改** `module-template`。当前 `modulus new` 生成项目通过 `ModulusCliLibDir` 引用 CLI 随附的 `Modulus.*.dll`，并不使用 NuGet 版本范围策略；该策略属于另一个方向的变更。

## Impact

- Affected specs:
  - `runtime`
  - `module-packaging`
  - **ADDED** `host-sdk`
- Affected code (预期实现时会涉及；proposal 阶段不改代码):
  - `src/Modulus.Core/Runtime/ModulusApplicationFactory.cs`（Host SDK composition 的核心落点）
  - `src/Modulus.Core/Hosting/ModulusHostBuilderExtensions.cs`（可复用的宿主集成入口）
  - `src/Modulus.Core/Architecture/*`（`SharedAssemblyCatalog` / diagnostics / canonical policy）
  - `src/Modulus.Cli/Commands/PackCommand.cs`（shared exclusion 与 runtime policy 收敛）
  - `src/Hosts/*`（Host 迁移为“使用 Host SDK 的参考实现”）


