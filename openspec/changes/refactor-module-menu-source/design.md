## Context
本变更将模块菜单来源统一为**代码属性**，并移除 `bundled-modules.json` 依赖与 vsixmanifest 菜单声明。模块仍通过 `AssemblyLoadContext` 隔离加载。

## Goals / Non-Goals
- Goals
  - 菜单来源单一：以 host-specific 入口类型上的菜单属性为准。
  - 运行时渲染不做反射：菜单 install/update 时投影到 DB，渲染时 DB → `IMenuRegistry`。
  - 内置模块由 Host 项目引用决定“随应用发布哪些模块”，但不破坏 ALC 隔离。
  - 第三方 Blazor 模块样式支持：通过运行时注入 CSS。
  - 不兼容旧数据/旧模块：不提供迁移/兼容分支，失败即要求清理旧 DB/旧包。
- Non-Goals
  - 不支持用户自定义菜单（重命名/排序/隐藏）。
  - 不解决第三方模块的所有静态资源问题（本次仅定义 CSS 注入的最小闭环）。

## Decisions

### Decision: Menu source = module entry attributes (host-specific)
- Blazor 菜单从 `[BlazorMenu]` 读取。
- Avalonia 菜单从 `[AvaloniaMenu]` 读取。
- 只解析入口类型（继承 `ModulusPackage` 的 host-specific UI 程序集入口），不扫描所有类型，降低误报与成本。

### Decision: Install/update time projection, runtime read-from-DB
- install/update：解析菜单属性并写入 `Menus` 表（Replace by module id）。
- runtime render：从 DB 查询启用模块的菜单，注册到 `IMenuRegistry`。

### Decision: Third-party module attribute parsing is metadata-only
- 解析菜单属性不得执行第三方代码：
  - 不调用目标程序集中的任意方法/静态初始化。
  - 使用 `System.Reflection.Metadata`（或 `MetadataLoadContext`）读取 custom attributes。
- 原因：避免恶意/损坏模块导致 Host 启动崩溃；降低依赖解析复杂度。

### Decision: Built-in modules are selected via ProjectReference but not loaded into default ALC
- Host 对内置模块的项目引用用于：
  - 构建期确保模块产物生成并复制到随应用发布的 `Modules/` 目录。
  - 允许 CI/IDE 在编译期发现模块依赖关系。
- Host 不引用内置模块输出程序集（`ReferenceOutputAssembly="false"`），运行时仍由 `ModuleLoader` 从 `Modules/` 目录加载模块到独立 ALC。

### Decision: Enable/disable built-in modules
- 禁用仅影响 `Modules.IsEnabled`，不会影响 Host 启动。
- 再次启用：
  - 模块文件仍在应用发布目录 `Modules/`（禁用不删除文件），因此可立即加载（按现有启用逻辑：即时加载或下次启动加载）。
- install/update 覆盖写入菜单时必须保留 `IsEnabled`（避免版本更新把用户禁用状态重置）。

### Decision: No backward compatibility
- 不尝试兼容：
  - 旧的菜单来源（vsixmanifest `Modulus.Menu`、bundled-modules.json）。
  - 旧的模块入口/过时组件逻辑（例如历史兼容路径、旧 DB 结构）。
- DB：若检测到旧版本数据结构或旧来源标记，直接失败并提示删除数据库。

## Risks / Trade-offs
- 动态模块 CSS：运行时注入解决“样式缺失”，但无法利用 scoped css 的编译期隔离能力；需要约束命名空间与注入粒度。
- Metadata-only 解析：实现成本较高，需要完善测试覆盖以防解析回归。
- 去除 bundled-modules.json：需要明确内置模块的“发布清单”由构建系统保证（ProjectReference + copy outputs）。

## Migration Plan
- 本变更不提供数据迁移：
  - 用户必须删除旧数据库（如 `BlazorModulus.db` / `AvaloniaModulus.db`）。
  - 旧模块若未提供菜单属性，将无法生成菜单。

## Open Questions
- Blazor 样式注入的注入时机：按路由导航触发 vs 全局注入（建议：按模块/路由范围注入，避免污染）。
- 菜单属性的 Key 形式：是否增加 `Key` 字段以生成稳定 MenuId（推荐）。


