## ADDED Requirements

### Requirement: Built-in module selection via host project references
构建系统 SHALL 允许 Host 通过项目引用选择“随应用发布”的内置模块集合，同时运行时不将模块程序集加载进默认 ALC。

#### Scenario: Host includes built-in modules without referencing output assembly
- **WHEN** Host 项目引用一个内置模块项目
- **THEN** 该引用使用 `ProjectReference` 并设置 `ReferenceOutputAssembly="false"`
- **AND** 构建输出将模块产物复制到 `artifacts/bin/Modules/{ModuleName}/`
- **AND** Host 运行时仍由 `ModuleLoader` 从 `Modules/` 目录加载该模块

### Requirement: Module package does not include bundled-modules.json
模块打包与发布流程 MUST 不依赖 `bundled-modules.json`。

#### Scenario: Packing does not generate or consume bundled list
- **WHEN** 执行模块打包/Host 发布
- **THEN** 不生成 `bundled-modules.json`
- **AND** 不在运行时读取 `bundled-modules.json`


