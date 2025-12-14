## 1. Specification

- [ ] 1.1 添加 `host-sdk` delta spec（定义包结构、API 边界、Shared Domain、版本策略、MAUI Blazor 约束）
- [ ] 1.2 更新 `runtime` delta spec：Shared Assembly Policy（权威来源、诊断、Host SDK 纳入共享域）
- [ ] 1.3 更新 `module-packaging` delta spec：打包剔除共享程序集与运行时策略一致
- [ ] 1.4 更新 `module-template` delta spec：模板版本引用策略（同一 release train）

## 2. Implementation (后续在 proposal 批准后执行)

- [ ] 2.1 新增/拆分 Host SDK 项目（Abstractions/Core/Avalonia/BlazorMaui），并声明 Shared Domain
- [ ] 2.2 引入 Host SDK builder API（最小 public surface + options + extensibility）
- [ ] 2.3 统一 Shared Assembly Policy（运行时 + CLI/Nuke 打包复用）
- [ ] 2.4 更新现有 Host（Avalonia/Blazor）迁移到 Host SDK（参考实现）
- [ ] 2.5 增强诊断：输出共享程序集快照与 mismatch，提供可视化/日志
- [ ] 2.6 CI/构建目标分离：支持在无 MAUI 工具链的平台上不阻塞主流程（同时保持 MAUI 路线可构建）


