# Design: Built-in module catalog security and dev-mode integrity policy

## Scope
本设计覆盖：
- 自带模块（Built-in / System modules）如何由 Host 项目引用确定、随应用发布、启动时入库、并进行完整性校验以防替换。
- Development 环境下的完整性策略（仅校验程序集名/强名称，仍使用 `{AppBaseDir}/Modules/`）。
- 系统模块不可卸载/不可禁用的强制规则（多层防线 + 启动自修复）。
- 用户模块防回滚（仅用户模块），数据存 DB。

不在本设计范围：
- 自带模块独立更新、签名分发、模块市场、供应链签名验证。
- 兼容旧模块/旧数据库（本版本不兼容）。

## Definitions
- **Built-in module / System module**：随 Host 应用一起发布的模块集合，来源是 Host `.csproj` 的 `ProjectReference` 列表（build-time）。
- **User module**：安装在用户目录的模块集合（可卸载/可禁用）。
- **Catalog**：不可篡改的自带模块清单，编译进 Host 程序集，作为 allowlist。
- **Integrity check**：对模块包（manifest + 目标程序集）的校验，用于防止“替换模块文件”。
- **HostType**：例如 `Modulus.Host.Avalonia` / `Modulus.Host.Blazor`。
- **AppBaseDir**：`AppContext.BaseDirectory`。

## Goals
- **G1**：自带模块集合必须由 Host 项目引用确定，不允许通过扫描 `{AppBaseDir}/Modules/` 注入。
- **G2**：Production 环境下，自带模块必须通过强完整性校验（SHA256）才能入库并加载。
- **G3**：Development 环境下（`DOTNET_ENVIRONMENT=Development`），允许频繁编译导致 hash 变化，但仍要限制注入面（仅允许 allowlist + 程序集名/强名称校验 + 必须在 `{AppBaseDir}/Modules/`）。
- **G4**：系统模块不允许卸载、不允许禁用；即使 DB 被篡改也能自修复。
- **G5**：菜单渲染只来自 DB；模块属性只用于入库/安装/启动同步阶段投影。
- **G6**：防回滚仅针对用户模块，状态存 DB，install/update 时拒绝降级。

## Non-Goals
- **NG1**：不处理攻击者具备写入/替换 Host 程序集的场景（那属于 OS 权限与应用签名问题）。
- **NG2**：不支持“系统模块在用户目录 overlay 更新”的机制。

## Architecture

### A) Built-in Module Catalog
Catalog 由构建阶段生成并编译进 Host（建议在 Host 工程下生成 `BuiltInModuleCatalog.g.cs`）：

- **Input**：Host `.csproj` 中标记为 built-in 的 `ProjectReference`（推荐通过 item metadata，例如 `ModulusBuiltIn="true"`）。
- **Output**：`BuiltInModuleCatalog`（强类型）包含 entries：
  - `ModuleId`（GUID string）
  - `ModuleName`
  - `RelativePackageDir`：固定 `Modules/{ModuleName}/`
  - `ManifestSha256`（Production 用）
  - `Files`：每个文件 `RelativePath + Sha256 + AssemblyIdentity(可选)`
  - `Hosts`：该模块支持的 host（用于选择 UI 目标程序集）

**关键约束**：
- Runtime 绝不通过扫描 `{AppBaseDir}/Modules` 发现系统模块。
- 系统模块加载路径不信任 DB，只信 Catalog 的 `{AppBaseDir}/{RelativePackageDir}`。

### B) Integrity policy (Production vs Development)
依据 `DOTNET_ENVIRONMENT` 判定：
- `Development`：Dev policy
- 其它：Production policy

#### Production policy
- **必须校验**：
  - `extension.vsixmanifest` SHA256 == `Catalog.ManifestSha256`
  - 当前 HostType 需要加载的目标程序集（例如 `*.UI.Blazor.dll` 或 `*.UI.Avalonia.dll`）SHA256 == Catalog 记录
  - `*.Core.dll`（如果属于系统模块包的一部分且会被加载）SHA256 == Catalog 记录
- **失败处理**：
  - 不入库（或将模块标记为 `Tampered`）
  - 不加载
  - 记录可诊断日志

#### Development policy
为了不阻断开发迭代：
- **路径约束**：仍必须位于 `{AppBaseDir}/Modules/{ModuleName}/`（禁止从用户目录覆盖系统模块）。
- **程序集校验**：不做 SHA256；仅校验目标程序集的：
  - `AssemblyName.Name` 匹配 Catalog 期望值
  - StrongName 公钥（或 PublicKeyToken）匹配 Catalog 期望值（若程序集是强签名）
  - 若程序集未强签名，则仅做 `AssemblyName.Name` 校验并产生 warning（显式提示风险仅限开发环境）
- **manifest 校验**：
  - 校验 manifest 中的 `ModuleId` 必须等于 Catalog 的 `ModuleId`
  - 校验 `InstallationTarget` 包含当前 HostType（并校验 Version range 满足当前 HostVersion）
  - 不校验 manifest SHA256（避免改 manifest 时阻断开发）

> 以上满足你要求：Development 模式仅校验程序集名/强名称，且仍使用 `{AppBaseDir}/Modules/`。

### C) Startup sync (2B) and menu projection
启动链路（DB migrate 之后、模块加载之前）执行：

1. `SyncBuiltInModulesAsync(hostType)`
   - 遍历 Catalog entries（仅 allowlist）
   - 计算模块 package dir：`{AppBaseDir}/{RelativePackageDir}`
   - 依据环境执行完整性校验（Prod/Dev policy）
   - 通过校验后执行 2B 入库：
     - 触发条件：
       - DB 无该模块
       - `ManifestHash` !=（Production：Catalog manifest sha；Development：可跳过此项，仅在缺失时写）
       - 当前 hostType 的菜单记录缺失
     - 动作：
       - Upsert `ModuleEntity`：强制 `IsSystem=true`、`IsEnabled=true`、`Path` 写为相对路径（如 `Modules/{ModuleName}/` 或 manifest 相对路径）
       - `ReplaceModuleMenusAsync`：使用 metadata-only 读取菜单属性写库
2. `Integrity check`（若系统已有 checker）：
   - 对系统模块：额外验证 `DB.Path` 不参与路径决策；仅用于展示/诊断
3. 加载阶段：
   - 从 DB 查询 `IsEnabled=true && State=Ready` 的模块
   - 对 `IsSystem=true`：加载路径来自 Catalog

### D) System module enforcement
系统模块强制规则：
- 禁止卸载（uninstall）
- 禁止禁用（disable）
- 启动自修复：若 DB 中系统模块 `IsEnabled=false` 或 `IsSystem=false` → 纠正并记录 warning

强制点（多层）：
- `IModuleInstallerService` / uninstall service：拒绝
- `IModuleRepository` 写入路径：系统模块写入时强制字段
- CLI 命令层：拒绝并返回明确错误
- UI 操作入口：隐藏或置灰（但不能只靠 UI）

### E) User module anti-rollback (DB-backed)
仅针对用户模块（用户目录可写）：
- DB 存储 per-module 的最大已接受版本（推荐 SemVer 比较）：
  - `UserModulePolicyEntity`：`ModuleId`, `MaxAcceptedVersion`, `UpdatedAt`
- 安装/更新时：
  - 若 `incomingVersion < MaxAcceptedVersion` → 拒绝（默认）
  - 若安装成功且版本更高 → 更新 `MaxAcceptedVersion`
- 可选：提供 `--force` 仅用于开发/测试（默认生产环境关闭，或需要显式配置开启）

## Operational notes
- 对系统模块，“防替换”依赖应用安装目录写权限与 Production 校验共同生效。
- Development 政策必须显式由 `DOTNET_ENVIRONMENT=Development` 开启，避免误入生产。

## Open Questions
- 系统模块 DLL 是否全部强签名？若不是，Development 下强名称校验只能部分生效，需要定义 warning 策略。
- User module 的版本比较使用何种 SemVer 实现与规范（NuGet.Versioning 推荐）。


