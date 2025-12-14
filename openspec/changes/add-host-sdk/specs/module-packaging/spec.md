# module-packaging Specification (Delta)

## ADDED Requirements

### Requirement: Shared Assembly Exclusion Policy Alignment
Module packaging MUST exclude shared assemblies using the same canonical shared-assembly policy as the runtime, to avoid divergence between packaging and loading behaviors.

#### Scenario: Pack excludes Host SDK assemblies and shared UI framework assemblies
- **WHEN** 执行模块打包（CLI 或 Nuke）
- **THEN** 包内不得包含 Host SDK 程序集（Shared domain）
- **AND** 包内不得包含 Host 已提供的共享 UI 框架程序集（如 Avalonia、MAUI/Blazor、MudBlazor）
- **AND** 若发现疑似共享程序集被打包，应输出可操作的诊断（提示调整策略或依赖）


