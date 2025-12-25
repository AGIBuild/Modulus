# module-packaging Specification (Delta)

## ADDED Requirements

### Requirement: Packaging Exclusion Supports Prefix Rules
Module packaging MUST exclude shared assemblies using the canonical shared-assembly policy, including prefix/pattern rules.

#### Scenario: Pack derives host policy from InstallationTarget
- **WHEN** 执行模块打包（CLI 或 Nuke）
- **AND** 模块清单包含 `Installation/InstallationTarget`
- **THEN** 打包流程根据 InstallationTarget 的 Host 集合选择/合并对应的 shared policy presets
- **AND** 使用该 policy（exact + prefixes）剔除共享程序集，避免重复加载

#### Scenario: Pack outputs excluded shared assemblies diagnostics
- **WHEN** 打包流程剔除共享程序集
- **THEN** 输出诊断信息包含：被剔除的程序集名列表
- **AND** 对于 prefix 命中的剔除项，能输出命中的 prefix 规则（可选在 verbose 模式）


