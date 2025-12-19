## Context
本变更以“模块为一级菜单，View 为二级菜单”为目标，并提供默认基础设施使第三方模块开发者无需编写额外注册代码：
- 菜单投影在安装/更新期完成，且 MUST 为 metadata-only（不执行模块代码）。
- 运行时导航基于稳定 Key 查表解析目标，避免 Avalonia 运行时全域类型扫描。
- ViewModel 可以通过继承基类（或实现接口）覆写 `NavigateFrom/NavigateTo` 拦截与生命周期回调。

## Goals / Non-Goals
- Goals:
  - 提供模块级菜单（父）与 View 级菜单（子）的层级模型（DB `ParentId`）。
  - 单 View 模块主导航只显示模块级菜单；多 View 模块显示模块级菜单 + 子 View 菜单。
  - ViewModel 可覆写导航拦截与导航生命周期，无需额外注册。
  - 模板默认包含 View 级菜单声明示例。
- Non-Goals:
  - 本变更不要求取消模块级菜单声明（仍作为父节点来源）。
  - 本变更不要求把所有菜单完全“零配置”（DisplayName/Icon/Order 仍建议显式声明）。

## Decisions
### Decision: 引入 View 级菜单 Attribute（Host-specific）
为保持安装期 metadata-only 可解析与 Host 行为一致，新增：
- `BlazorViewMenuAttribute`：声明 View（组件）在模块导航中的菜单信息（key/display/icon/order 等）。
- `AvaloniaViewMenuAttribute`：声明 ViewModel 在模块导航中的菜单信息（key/display/icon/order 等）。

说明：
- Blazor 的 route 从 `RouteAttribute`（由 `@page` 生成）获取；`BlazorViewMenuAttribute` 不负责提供 route，仅提供菜单元数据。
- Avalonia 的导航目标最终解析为 ViewModel 类型 + View 类型；`AvaloniaViewMenuAttribute` 放在 ViewModel 类型上可直接得到 ViewModel Type。

### Decision: 使用 DB `MenuEntity.ParentId` 形成层级菜单
安装期写入：
- 模块级菜单：`ParentId = null`
- View 级菜单：`ParentId = <模块级菜单.Id>`

运行时从 DB 读取后构建 `MenuItem.Children`，UI 直接使用树结构渲染。

### Decision: 单 View 折叠规则（View 菜单声明仍要求存在，但不生效）
- 模块内 View 数量 == 1：
  - 主导航仅显示模块级菜单（来自模块入口类型声明）
  - 忽略 View 级菜单（不写入 DB，或写入但运行时折叠；推荐安装期不写入，避免噪音）
- 模块内 View 数量 > 1：
  - 模块级菜单作为父节点显示（名字来自模块声明）
  - View 级菜单作为子节点显示（名字来自 View 声明）

### Decision: ViewModel 导航拦截与生命周期（可覆写）
在 `Modulus.UI.Abstractions` 引入：
- `ViewModelBase`（建议）：
  - `virtual Task<bool> CanNavigateFromAsync(NavigationContext context)` 默认 true
  - `virtual Task<bool> CanNavigateToAsync(NavigationContext context)` 默认 true
  - `virtual Task OnNavigatedFromAsync(NavigationContext context)` 默认 completed
  - `virtual Task OnNavigatedToAsync(NavigationContext context)` 默认 completed（包含参数）

并允许不继承场景：
- `INavigationParticipant`（可选接口）与基类语义一致

导航服务拦截顺序（建议）：
1) 全局 `INavigationGuard`：`CanNavigateFromAsync` / `CanNavigateToAsync`
2) 当前 ViewModel（如果支持）`CanNavigateFromAsync`
3) 目标 ViewModel（如果支持）`CanNavigateToAsync`
4) 发生切换后：当前 `OnNavigatedFromAsync` → 目标 `OnNavigatedToAsync`

### Decision: Avalonia View 使用 DI 构造（不提供无参构造）
为统一“View 决定绑定哪个 ViewModel”的约定，并减少模板/示例中的样板代码：
- View 类型 SHOULD 仅提供带依赖注入参数的构造函数（例如 `MyView(MyViewModel vm)`），不提供无参构造函数。
- View 构造函数内负责完成绑定（如 `DataContext = vm`）。
- 设计态（Designer）通过 `Design.IsDesignMode` 分支处理，保证 XAML 设计器可用：
  - 设计态可以创建一个轻量的 DesignTime ViewModel（或使用默认值），避免依赖运行时 DI/模块加载器。

### Decision: Module View / ViewModel MUST 由模块 CompositeServiceProvider 创建（Host 不直接创建模块类型）
为保证模块隔离与卸载安全，以及让模块对象可以同时依赖“模块服务 + Host 服务”：
- 导航创建模块 ViewModel 时 MUST 使用 `RuntimeModuleHandle.CompositeServiceProvider`（primary=模块，fallback=Host）。
- UI 工厂创建模块 View 时 MUST 使用同一个 `RuntimeModuleHandle.CompositeServiceProvider`，以支持 View 构造函数注入模块 ViewModel（以及其它模块服务）。
- Host 的根 `IServiceProvider` MUST NOT 直接构造模块类型（View/ViewModel/服务实现），避免跨 ALC 类型泄漏与卸载阻塞。
  - Host 仅应依赖共享域抽象（`Modulus.UI.Abstractions` 等）与稳定的 `navigationKey`。

## Migration Plan
- 先实现 DB 分层注册与 UI 渲染树结构（不会破坏现有单层菜单，只是添加 children）。
- 为兼容历史 DB 中 `ParentId=null` 的扁平结构，运行时可以：
  - 当所有项 `ParentId==null` 时仍按原逻辑显示（等价）。
- 模板更新只影响新创建模块，不影响现有模块。

## Open Questions
- 多 View 模块的父菜单点击行为：仅展开/收起（不触发导航；父节点 `NavigationKey` 为空，由 UI 负责切换展开态）
- View 级菜单 key 的规范：是否要求稳定且与 route/类型解耦？（推荐：`<viewKey>` 由开发者声明，DB id 仍由系统规则拼装）


