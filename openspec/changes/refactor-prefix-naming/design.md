## Context

Modulus 同时存在多类对外标识符：
- 代码层：`namespace` / `RootNamespace`
- 构建产物层：`AssemblyName`、dll 名称
- 分发层：`PackageId`
- 运行时协议层：`Host Id`（`extension.vsixmanifest` 的 `InstallationTarget/@Id`、`Asset/@TargetHost`）
- 工程层：Solution/Project 名称与路径

这些标识符在生态扩展（模板、模块、NuGet）上必须一致，否则会出现：
- 包名冲突（NuGet.org conflict）
- 模块 Host 校验失败（manifest target 不匹配）
- Shared Domain 规则失效导致 ALC 重复加载（类型不相等、资源加载失败）

本变更按你的要求：**不考虑兼容性**，一次性彻底切换到 `Agibuild.Modulus.*` 命名体系。

## Goals / Non-Goals

**Goals:**
- Solution/Project/Assembly/RootNamespace 全量加前缀：`Agibuild.Modulus`
- Host Id 全量切换：
  - `Agibuild.Modulus.Host.Avalonia`
  - `Agibuild.Modulus.Host.Blazor`
- 模块清单中所有 Host 相关引用全量切换（不提供旧值兼容）
- Shared Domain/打包剔除规则同步到新程序集名
- 模板生成项目默认命名与引用全部切换到新体系

**Non-Goals:**
- 不提供任何兼容映射或迁移脚本（包括 DB 记录/已安装模块/旧模板/旧 manifest）
- 不在本 change 内改变用户数据路径（如 `%APPDATA%/Modulus` 或默认数据库文件名）；若未来需要改另起 change

## Decisions

### Decision 1: Naming Convention

**选择**：所有一方工程（solution/project/assembly/root namespace）统一前缀 `Agibuild.Modulus`，后缀按职责分层：
- `Agibuild.Modulus.Core`
- `Agibuild.Modulus.Sdk`
- `Agibuild.Modulus.UI.Abstractions`
- `Agibuild.Modulus.UI.Avalonia`
- `Agibuild.Modulus.UI.Blazor`
- `Agibuild.Modulus.Host.Avalonia`
- `Agibuild.Modulus.Host.Blazor`
- `Agibuild.Modulus.Cli`
- `Agibuild.Modulus.Infrastructure.*`
- Tests：`Agibuild.Modulus.*.Tests`

### Decision 2: Host Id is part of the runtime protocol (and will BREAK)

**选择**：Host Id 作为运行时协议的一部分，直接切换为：
- `Agibuild.Modulus.Host.Avalonia`
- `Agibuild.Modulus.Host.Blazor`

并且：
- 不接受旧 Host Id
- 不提供旧→新映射
- 清单中 `InstallationTarget` 与 `Asset/@TargetHost` 必须使用新值

### Decision 3: Shared Domain / Packaging exclusion must be updated in lockstep

**选择**：重命名后，Shared Domain 清单与打包剔除规则必须同步切换到新程序集名，否则会导致：
- 模块打包携带了本应共享的 dll（运行时重复加载）
- 或运行时无法识别应共享的 Host/SDK dll（类型不相等）

本 change 的 spec 将明确这一点，避免“重命名后 shared policy 漏改”。

## Risks / Trade-offs

| 风险 | 影响 | 缓解 |
|------|------|------|
| 全量重命名改动面巨大 | 容易漏改导致 build/restore 失败 | 用清单化 tasks + 自动化检索（sln/csproj/manifest/templates） |
| 不做兼容导致旧模块全部不可用 | 生态断裂 | 这是明确决策；在 release note 中声明（不在本 change 范围） |
| Host Id/TargetHost 改动导致运行时加载失败 | 模块无法加载 | 通过 spec 强制并在实现阶段补齐诊断信息 |
| Shared Domain 漏改 | ALC 重复加载，隐性崩溃 | 强制共享策略与剔除规则同步更新并验收加载测试 |

## Migration Plan

1. 更新 specs（本 change）
2. 实现阶段按 tasks 清单执行全仓重命名
3. 通过 `dotnet restore/build/test` 与“模块加载验证”作为验收

## Open Questions

- 无（你已明确“不考虑兼容性”，并指定了前缀/Host Id/solution 名）


