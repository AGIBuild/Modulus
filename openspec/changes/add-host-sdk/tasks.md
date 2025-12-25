## 1. Specification

- [x] 1.1 更新 `host-sdk` delta spec（定义 composition layer、公共 API 边界、可选默认 Shell、Shared domain、Host version 供给）
- [x] 1.2 更新 `runtime` delta spec：canonical shared-assembly policy（权威来源、诊断、供 runtime+packaging 复用）
- [x] 1.3 更新 `module-packaging` delta spec：packaging shared exclusion 与 runtime policy 对齐
- [x] 1.4 删除 `module-template` delta（本 change 不涉及模板版本策略；模板当前通过 `ModulusCliLibDir` 引用 CLI 随附 dll）

## 2. Implementation (后续在 proposal 批准后执行)

- [x] 2.1 新增 Host SDK 项目（Abstractions/Runtime + 可选 Shell），并声明 Shared Domain
- [x] 2.2 引入 Host SDK composition builder/options（围绕 `ModulusApplicationFactory`，最小 public surface）
- [x] 2.3 将现有 Host（Avalonia/Blazor Hybrid(MAUI)）迁移为“使用 Host SDK 的参考实现”
- [x] 2.4 统一 shared-assembly 策略：让 `modulus pack` 与 `nuke pack-module` 复用 runtime 的 canonical policy（移除硬编码前缀分叉）
- [x] 2.5 增强 diagnostics：packaging 输出“被剔除的 shared assemblies 列表”与来源，便于排障
- [x] 2.6 MAUI 构建目标分离：支持在无 MAUI 工具链的平台上不阻塞主流程（同时保持 MAUI 路线可构建）


