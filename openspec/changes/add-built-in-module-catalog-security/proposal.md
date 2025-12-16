# Change: Add built-in module catalog security and dev-mode integrity policy

## Why
- 自带模块与应用绑定，必须防止“投放/替换”模块文件带来的安全风险。
- 菜单渲染只读数据库，自带模块需要在启动时可靠入库、可诊断、可控。
- 开发阶段频繁编译会导致 hash 变化，需要一个明确的 Development 策略避免开发体验崩坏。
- 防回滚主要针对用户模块（用户目录可写），自带模块随应用更新不需要单独防回滚。

## What Changes
- **Built-in Module Catalog**：Host build-time 依据 `ProjectReference` 生成不可篡改的自带模块清单（编译进 Host）。
- **Integrity policy**：
  - Production：自带模块的 `extension.vsixmanifest` 与目标程序集文件 SHA256 必须与 Catalog 匹配，否则拒绝入库/加载。
  - Development（`DOTNET_ENVIRONMENT=Development`）：自带模块不做 SHA256 校验，仅校验程序集名/强名称（且仍要求位于 `{AppBaseDir}/Modules/`）。
- **System module enforcement**：自带模块 `IsSystem=true` 且 **不允许卸载/禁用**，任何操作请求直接拒绝并在启动时自修复。
- **Menu projection**：自带模块按方案 2B（缺失/变更/菜单缺失才投影）在启动时入库；导航渲染只读 DB。
- **User module anti-rollback**：仅对用户模块启用防回滚策略（保存到 DB 并在 install/update 时拒绝降级）。
- **No backward compatibility**：不兼容旧数据与旧逻辑；对旧数据库结构/旧菜单来源失败快并提示删除数据库。

## Impact
- Affected specs:
  - `openspec/specs/runtime/spec.md`
  - `openspec/specs/module-packaging/spec.md`
- Affected code (implementation stage):
  - Host build pipeline (MSBuild/Nuke) for catalog generation
  - `Modulus.Core.Runtime` startup sync & integrity gates
  - `Modulus.Core.Installation` for projection triggers
  - DB schema for user module anti-rollback


