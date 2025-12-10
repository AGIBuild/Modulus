## ADDED Requirements

### Requirement: Host version registration
Host SHALL 在启动时注册版本信息到 RuntimeContext。

#### Scenario: Avalonia Host registers version
- **WHEN** Avalonia Host 启动
- **THEN** `RuntimeContext.HostVersion` 设置为 Host 程序集版本
- **AND** 版本格式为 SemVer（如 `1.0.0`）

#### Scenario: Blazor Host registers version
- **WHEN** Blazor Host 启动
- **THEN** `RuntimeContext.HostVersion` 设置为 Host 程序集版本

### Requirement: InstallationTarget version validation
模块加载时 SHALL 验证 InstallationTarget 声明的版本范围与当前 Host 版本兼容。

#### Scenario: Version range satisfied allows load
- **WHEN** 模块声明 `InstallationTarget Id="Modulus.Host.Avalonia" Version="[1.0,2.0)"`
- **AND** Host 版本为 `1.5.0`
- **THEN** 版本验证通过，模块加载继续

#### Scenario: Version range not satisfied blocks load
- **WHEN** 模块声明 `InstallationTarget Version="[2.0,)"`
- **AND** Host 版本为 `1.5.0`
- **THEN** 验证失败，输出诊断信息
- **AND** 模块不被加载

#### Scenario: Missing version range skips validation
- **WHEN** 模块 `InstallationTarget` 未声明 `Version` 属性
- **THEN** 跳过版本验证，仅验证 Host Id 匹配

