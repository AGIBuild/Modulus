# runtime Specification (Delta)

## ADDED Requirements

### Requirement: Shared Assembly Policy Supports Prefix Rules
The runtime SHALL support prefix/pattern rules in the shared-assembly policy, in addition to exact assembly simple names.

#### Scenario: Prefix rules mark framework families as shared
- **WHEN** Host 配置了 `Modulus:Runtime:SharedAssemblyPrefixes`（例如 `Avalonia` 或 `Microsoft.Maui.`）
- **THEN** 模块加载时对命中前缀规则的程序集视为 shared（从默认上下文解析）
- **AND** 不要求用户枚举所有框架程序集名

#### Scenario: Diagnostics explain why an assembly is shared
- **WHEN** shared-assembly diagnostics 输出某个程序集条目
- **THEN** 诊断信息能说明该条目来自：domain/config
- **AND** 若由 prefix 规则命中，诊断信息能显示命中的 prefix 规则


