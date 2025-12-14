# Change: 引入 Host SDK（API 边界 + Shared Domain 清单 + 版本策略）

## Why

当前 Host（`Modulus.Host.Avalonia` / `Modulus.Host.Blazor`）更像“参考实现”，无法作为稳定可复用的 SDK 供第三方应用快速构建“插件式应用壳”。与此同时，模块隔离（ALC）对 **Shared Domain** 与 **版本兼容** 极其敏感：一旦 Host 与模块携带/加载了不同副本的 UI/SDK 程序集，会导致运行时类型不相等、资源加载失败、以及难以排查的崩溃。

我们需要把 Host 能力产品化为 **Host SDK**，并用 OpenSpec 明确三件事情：
- Host SDK 的 **公共 API 边界**（可扩展、可品牌化、可升级）
- 与 ALC 相关的 **Shared Domain 共享程序集清单/策略**（避免重复加载）
- Host↔Module↔SDK 的 **版本策略**（避免生态碎片化）

## What Changes

- 新增 `host-sdk` capability（OpenSpec），定义：
  - Host SDK 的包结构与职责拆分（Avalonia Host SDK、MAUI Blazor Host SDK）
  - Host SDK 的公共 API（builder/options/扩展点）与稳定性边界
  - 与模块交互的契约（不把模块绑死在 Host 实现细节上）
- **MODIFIED** `runtime` capability：
  - 明确 Shared Domain 策略来源与落地（domain metadata + host config +（必要时）前缀策略）
  - 明确 Host SDK 程序集必须进入 Shared Domain，并能被诊断/观测
- **MODIFIED** `module-packaging` capability：
  - 明确打包时剔除共享程序集必须与运行时共享策略一致（避免“打包规则”和“加载规则”分叉）
- **MODIFIED** `module-template` capability：
  - 明确模板的版本引用策略（避免浮动到不兼容版本）

## Impact

- Affected specs:
  - `runtime`
  - `module-packaging`
  - `module-template`
  - **ADDED** `host-sdk`
- Affected code (预期实现时会涉及，proposal 阶段不改代码):
  - `src/Modulus.Core/Architecture/*`（shared assembly catalog / diagnostics）
  - `src/Modulus.Core/Runtime/ModuleLoadContext.cs`
  - `src/Modulus.Core/Runtime/ModulusApplicationFactory.cs`
  - `src/Hosts/*`（Host 入口统一接入 Host SDK builder）
  - `src/Modulus.Cli/Commands/PackCommand.cs` 与 `nuke pack-module`（共享程序集剔除策略统一）


