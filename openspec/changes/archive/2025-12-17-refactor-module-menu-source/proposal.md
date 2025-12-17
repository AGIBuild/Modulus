# Change: Refactor module menu source to code attributes

## Why
- 现有菜单来源分散（`bundled-modules.json` / `extension.vsixmanifest` / 代码属性），导致重复、难排错、Blazor 动态模块样式与路由不稳定。
- 期望对第三方开发者更友好：**菜单由代码属性声明**，更易维护与测试。

## What Changes
- **BREAKING**：移除 `extension.vsixmanifest` 中的 `Modulus.Menu` 声明，菜单不再来自 manifest。
- **BREAKING**：移除对 `bundled-modules.json` 的依赖；内置模块不再通过 JSON 列表“seed”。
- 菜单改为从模块入口类型（继承 `ModulusPackage`）的 host-specific 属性解析：
  - Blazor: `[BlazorMenu]`
  - Avalonia: `[AvaloniaMenu]`
- 菜单仍投影到数据库（install/update 时写入），渲染时从 DB 读取（保持运行时无反射）。
- 第三方 Blazor 模块样式：不保证 CSS isolation 生效，改为 **运行时样式注入机制**（模块提供 CSS 资源，Host 注入到 WebView）。
- 内置模块集成方式调整：通过 Host 的 `ProjectReference` 管理“随应用发布的模块集合”，但 **不引用输出程序集**（避免默认 ALC 载入破坏隔离）。
- **不兼容旧数据/旧模块**：不提供向后兼容逻辑；旧数据库与旧菜单来源不支持。

## Impact
- Affected specs:
  - `openspec/specs/runtime/spec.md`
  - `openspec/specs/module-packaging/spec.md`
  - `openspec/specs/shell-layout/spec.md`
- Affected code (implementation tasks will cover):
  - 安装与投影：`Modulus.Core.Installation` / `Modulus.Infrastructure.Data`
  - 运行时加载：`Modulus.Core.Runtime`
  - Blazor Host：路由与样式注入（MAUI WebView）
  - 模块模板：确保新模块默认使用菜单属性


