# Research: Modulus 核心架构与双宿主运行时

**Feature**: `001-core-architecture`  
**Date**: 2025-11-27  

本研究文档用于在实现前澄清关键架构决策，解决规格中的 NEEDS CLARIFICATION，并为后续设计与实现提供依据。

---

## 1. 插件打包格式与签名方案

### Decision

采用自定义的 Zip‑based 容器格式（暂定扩展名为 `.modpkg`），内部使用清晰的目录结构与 `manifest.json`
描述模块与插件的元数据。  
签名方案优先通过标准的文件签名机制（如 Authenticode 或外部签名文件）集成，而不是把签名逻辑
硬编码在容器格式中。

### Rationale

- Zip 容器跨平台、易于在 .NET 中读写，生态成熟；
- 自定义扩展名 `.modpkg` 可以与普通 Zip 区分，便于工具与宿主发现；
- 使用 JSON manifest 便于 AI 与人类阅读和生成；
- 将签名视为独立 concern，允许未来支持多种签名与校验方案，而不锁定在单一实现上。

### Alternatives considered

1. **直接使用 NuGet 包 (`.nupkg`)**  
   - Pros: 与现有 .NET 生态高度兼容，工具链成熟。  
   - Cons: NuGet 语义偏向代码分发与依赖管理，不完全匹配运行时插件加载场景；
     同时会引入与普通依赖管理混淆的风险。

2. **裸目录结构（无容器，仅文件夹）**  
   - Pros: 开发调试简单，零额外封装。  
   - Cons: 发布与分发体验较差，难以进行完整性校验，且不利于跨平台复制与备份。

---

## 2. Blazor 宿主承载方式

### Decision

Phase 1 中，将 Blazor 宿主抽象为“基于 WebView 的桌面宿主”，在架构上不锁定具体实现
（如 .NET MAUI Hybrid 或 Photino），而是通过一层 Host 抽象封装。具体技术选型可以在
宿主项目中根据目标平台与维护成本做最终决定。

### Rationale

- 当前生态中，.NET MAUI 与 Photino 均可承载 Blazor UI，且各有优劣；
- 在架构层面对“Blazor 宿主”进行抽象，有利于后续根据实际经验切换或并存多种实现；
- 对模块与 SDK 而言，关键在于「存在一个 Web 风格宿主」，而不是具体用哪种技术堆栈。

### Alternatives considered

1. **强制使用 .NET MAUI Hybrid**  
   - Pros: 官方支持、集成度高，对移动平台扩展有潜力。  
   - Cons: 对桌面工具型场景（尤其是已有桌面环境）可能显得偏重。

2. **强制使用 Photino / WebWindow 等轻量宿主**  
   - Pros: 更贴近桌面工具需求，依赖更少，打包体积可能更小。  
   - Cons: 社区生态与长期维护情况需要评估。

---

## 3. 测试框架与测试策略

### Decision

采用 **xUnit** 作为主单元测试框架，配合：

- 核心运行时与模块系统的单元测试与集成测试（`Modulus.Core.Tests`）；
- 宿主层端到端测试（`Modulus.Hosts.Tests`），通过自动化启动宿主、加载示例模块；
- SDK 契约测试（`Modulus.Sdk.Tests`），确保基类行为与文档 / 示例一致。

### Rationale

- xUnit 在 .NET 社区使用广泛，生态与工具支持成熟；
- 现有许多开源项目使用 xUnit，方便贡献者迁移习惯；
- 与 Nuke 等构建工具集成简单。

### Alternatives considered

1. **NUnit**  
   - Pros: 历史悠久、功能丰富。  
   - Cons: 与当前社区主流趋势相比略弱，团队习惯需统一。

2. **MSTest**  
   - Pros: 与部分微软工具链天然集成。  
   - Cons: 社区示例与生态相对较少，不利于插件作者快速上手。

---

## 4. 未决问题与后续研究方向

- 插件签名的具体实现路径（使用哪种证书体系、如何在 CI/CD 中集成）；
- 是否需要支持进程外插件（Out-of-Process），以及如何与当前 ALC 模式共存；
- 针对大型插件市场场景的版本兼容策略与回滚策略。

这些问题不阻塞 Phase 1 / Phase 2 的基本架构设计，但需要在后续 Story / Spec 中单独展开。


