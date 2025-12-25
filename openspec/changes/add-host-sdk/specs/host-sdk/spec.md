# host-sdk Specification (Delta)

## ADDED Requirements

### Requirement: Host SDK Package Layout
The system SHALL provide Host SDK packages that allow third-party applications to build a Modulus plugin host without copying repository host code, by reusing the canonical runtime composition centered on `ModulusApplicationFactory`.

#### Scenario: Avalonia host references Host SDK
- **WHEN** 应用选择 Avalonia 作为宿主
- **THEN** 应用仅需引用 Host SDK（composition layer + 可选 Avalonia 默认 Shell）即可完成宿主启动与模块运行时集成
- **AND** 应用不需要直接复制或修改 `Modulus.Host.Avalonia` 的源码（参考实现可作为示例）

#### Scenario: MAUI Blazor host references Host SDK
- **WHEN** 应用选择 Blazor Hybrid(MAUI) 作为宿主
- **THEN** 应用仅需引用 Host SDK（composition layer + 可选 Blazor Hybrid 默认 Shell）即可完成宿主启动与模块运行时集成
- **AND** Host SDK 明确其 MAUI 工具链前置要求与可分离构建策略（避免无 MAUI 平台被全仓阻塞）

### Requirement: Host SDK Public API Boundary
Host SDK SHALL expose a minimal, stable public API surface based on builder/options and explicit extension points, and SHALL keep internal implementation details non-public.

#### Scenario: App customizes services and branding via builder
- **WHEN** 应用使用 Host SDK 的 builder 配置 DI、配置源与品牌信息
- **THEN** 应用可以替换/扩展默认服务注册
- **AND** 应用可以覆盖默认的品牌资源（如名称、Logo、主题色）
- **AND** 不需要依赖 Host 内部实现类型（避免被破坏性变更锁死）

### Requirement: Host SDK Assemblies in Shared Domain
All Host SDK assemblies MUST be declared as Shared-domain assemblies and MUST be treated as shared by the module AssemblyLoadContext.

#### Scenario: Host SDK is shared across modules
- **WHEN** 模块运行于独立的 `ModuleLoadContext`
- **AND** 模块需要引用 Host SDK 的契约程序集（如 host abstractions）
- **THEN** 这些程序集应从默认上下文（Host shared context）解析
- **AND** 不允许模块加载 Host SDK 的私有副本（避免类型不相等）

### Requirement: Host Version & Compatibility via SemVer Range
Host SDK and the runtime SHALL enforce module compatibility via `InstallationTarget/@Version` SemVer ranges, and the host MUST provide a version that is parseable by the runtime.

#### Scenario: Module declares compatible host version range
- **WHEN** Host SDK 启动时提供 Host 版本
- **AND** 模块清单声明 `InstallationTarget Version="[1.0,2.0)"`
- **THEN** 模块加载通过版本校验
- **AND** 诊断信息应包含 Host 版本与模块声明范围

#### Scenario: Incompatible range blocks load with actionable diagnostics
- **WHEN** Host 版本不满足模块声明的版本范围
- **AND** 模块清单声明了 `InstallationTarget/@Version`
- **THEN** 模块加载被阻止
- **AND** 错误信息应提示用户升级 Host 或安装兼容版本的模块


