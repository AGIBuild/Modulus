# runtime Specification (Delta)

## ADDED Requirements

### Requirement: Canonical Shared Assembly Policy
The runtime SHALL define a single authoritative shared-assembly policy used for (1) module AssemblyLoadContext shared resolution and (2) packaging-time shared exclusions, and SHALL provide diagnostics for mismatches.

#### Scenario: Policy derived from domain metadata and host configuration
- **WHEN** Host 启动并初始化 shared assembly catalog
- **THEN** 共享程序集集合至少来自：
  - domain metadata（`[AssemblyDomain]` / `ModulusAssemblyDomain`）
  - host configuration（`Modulus:Runtime:SharedAssemblies`）
- **AND** 系统应记录来源（domain/config/manifest hint）以便诊断

#### Scenario: Prefix-based policy supported for framework families
- **WHEN** Host 需要将一组框架程序集视为共享（如 `Avalonia*`, `Microsoft.Maui.*`, `MudBlazor*`）
- **THEN** 系统应支持“前缀/模式”级别的共享策略，避免维护超长的显式程序集名列表
- **AND** 诊断输出应显示哪些程序集由前缀规则匹配

### Requirement: Host SDK Assemblies MUST be Shared
Host SDK assemblies MUST be in Shared domain and MUST be treated as shared during module loading.

#### Scenario: Host SDK assemblies present in shared catalog
- **WHEN** Host 引用并加载 Host SDK 程序集
- **THEN** shared assembly catalog 应包含这些程序集
- **AND** 若发现 Host SDK 程序集被标记为 Module-domain，应输出明确诊断


