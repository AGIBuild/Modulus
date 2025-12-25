# Change: 新增 Host App 模板（modulus new avaloniaapp/blazorapp + Visual Studio 向导）并采用 shared policy 前缀规则

## Why

我们希望“插件式应用（Host App）”的创建体验与模块创建一样标准化、可重复、可升级：
- 用户可以通过 `modulus new avaloniaapp` / `modulus new blazorapp` 一键创建可运行的插件式应用项目
- 用户也可以在 Visual Studio 中通过项目模板向导创建同类项目

要做到这一点，Host 项目必须满足以下约束，否则模板会不可维护或升级成本极高：
- Host 的运行时组合逻辑必须收敛到 **Host SDK**（composition layer），避免复制仓库内 `Modulus.Host.*` 源码
- shared assemblies 的策略必须可维护：Avalonia / MAUI / MudBlazor 等框架程序集族不应靠“枚举超长名单”，而应支持 **prefix/pattern** 规则
- packaging（`modulus pack` / `nuke pack-module`）必须复用同一 shared policy，避免“运行时认为 shared，但打包仍带进去”的分叉

## What Changes

- **ADDED** `host-app-template` capability（OpenSpec），定义：
  - CLI：`modulus new avaloniaapp` / `modulus new blazorapp` 生成 Host App
  - Visual Studio：提供向导式 Host App 模板（Avalonia / Blazor Hybrid(MAUI)）
  - 生成项目的 Host 设计契约：必须通过 Host SDK builder/options 完成运行时组合
- **ADDED/MODIFIED** `runtime` capability：
  - shared-assembly policy 支持 prefix/pattern 规则（方案 1）
  - diagnostics 能解释某程序集为何被判定为 shared（exact/prefix + 来源）
- **ADDED/MODIFIED** `module-packaging` capability：
  - packaging shared exclusion 复用 canonical policy（包含 prefix/pattern）
  - `modulus pack` 能根据模块清单 `InstallationTarget` 推导目标 Host 并选择/合并 policy

## Impact

- Affected specs:
  - **ADDED** `host-app-template`
  - `runtime`
  - `module-packaging`
- Dependencies:
  - 本 change 依赖 `add-host-sdk`：Host App 模板生成的项目必须引用 Host SDK（composition layer + 可选默认 Shell）


