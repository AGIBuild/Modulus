# Change: 内置模块打包发布

## Why
框架需要将某些模块作为 Host 的内置部分发布。当前启动流程每次都执行"目录扫描 → manifest 解析 → 数据库比对"，即使模块没有变化。需要优化为：模块加载的唯一来源是数据库，内置模块通过 EF 迁移预置。

## What Changes
- 合并并清理现有数据迁移脚本，作为干净的起点
- 新增 CLI 命令 `modulus add-module-migration` 自动生成模块数据迁移
- 内置模块数据通过 EF 迁移管理（使用 `InsertData`/`UpdateData`/`DeleteData`）
- 简化启动流程：移除目录扫描，直接从数据库加载模块

## Impact
- Affected specs: 新增 `bundled-modules` capability
- Affected code:
  - `src/Shared/Modulus.Infrastructure.Data/Migrations/` - 合并现有迁移 + 新增模块数据迁移
  - `src/Modulus.Cli/` - 新增 `add-module-migration` 命令
  - `src/Modulus.Core/Runtime/ModulusApplicationFactory.cs` - 简化启动流程
  - `src/Modulus.Core/Installation/SystemModuleInstaller.cs` - 调整用途（仅用于用户安装）
