# runtime Specification (Delta)

## ADDED Requirements

### Requirement: Canonical Shared Assembly Policy
The runtime SHALL define a single authoritative shared-assembly policy (based on exact assembly simple names) used for (1) module AssemblyLoadContext shared resolution and (2) packaging-time shared exclusions, and SHALL provide diagnostics for mismatches.

#### Scenario: Policy derived from domain metadata and host configuration
- **WHEN** Host 启动并初始化 shared assembly catalog
- **THEN** 共享程序集集合至少来自：
  - domain metadata（`[AssemblyDomain]` / `ModulusAssemblyDomain`）
  - host configuration（`Modulus:Runtime:SharedAssemblies`）
- **AND** 系统应记录来源（domain/config/manifest hint）以便诊断

#### Scenario: Packaging uses the same canonical policy
- **WHEN** 执行模块打包（CLI 或 Nuke）
- **THEN** 打包流程用于剔除共享程序集的规则必须与 runtime shared-assembly policy 一致
- **AND** 打包诊断应能输出“本次剔除的 shared assemblies 列表”以及其来源（domain/config）

### Requirement: Host SDK Assemblies MUST be Shared
Host SDK assemblies MUST be in Shared domain and MUST be treated as shared during module loading.

#### Scenario: Host SDK assemblies present in shared catalog
- **WHEN** Host 引用并加载 Host SDK 程序集
- **THEN** shared assembly catalog 应包含这些程序集（来自 domain metadata 或 host config）
- **AND** 若发现 Host SDK 程序集被标记为 Module-domain，应输出明确诊断


