## MODIFIED Requirements
### Requirement: Navigation Service
The framework SHALL provide an `INavigationService` abstraction that enables programmatic navigation with interception capabilities across all hosts.

#### Scenario: Navigate to a registered view
- **WHEN** 模块调用 `NavigateToAsync(navigationKey)`
- **THEN** 导航服务基于稳定 `navigationKey` 解析目标（而不是运行时全域扫描 Type）
- **AND** 解析结果包含：所属模块、目标 View（或 Route）、目标 ViewModel、实例生命周期策略
- **AND** 内容区域显示对应 View

#### Scenario: Navigate with typed ViewModel
- **WHEN** 模块调用 `NavigateToAsync<TViewModel>()`
- **THEN** 导航服务通过注册表/约定映射解析 `TViewModel` 对应的 `navigationKey`
- **AND** 导航行为与 `NavigateToAsync(navigationKey)` 等价

#### Scenario: Navigation exposes current state
- **WHEN** 导航成功完成
- **THEN** `CurrentNavigationKey` 反映当前激活的 `navigationKey`
- **AND** `Navigated` event MUST 被触发，且包含 `FromKey/ToKey` 与可选的 `View/ViewModel` 实例

### Requirement: Navigation Guards
The framework SHALL support registration of navigation guards that can intercept and conditionally prevent navigation.

#### Scenario: Guard prevents navigation from current page
- **WHEN** 用户触发从 A 到 B 的导航
- **AND** 任一 guard 的 `CanNavigateFromAsync` 返回 false
- **THEN** 导航取消，当前视图保持不变

#### Scenario: Guard prevents navigation to target page
- **WHEN** 用户触发从 A 到 B 的导航
- **AND** 任一 guard 的 `CanNavigateToAsync` 返回 false
- **THEN** 导航取消，当前视图保持不变

## ADDED Requirements
### Requirement: ViewModel Navigation Lifecycle
The framework SHALL provide ViewModel-level navigation interception and lifecycle hooks that can be overridden without extra registration code.

#### Scenario: ViewModel can intercept NavigateFrom
- **GIVEN** 当前 ViewModel 支持导航拦截（通过继承基类或实现接口）
- **WHEN** 从当前页面导航到新页面
- **THEN** 框架调用 `CanNavigateFromAsync(context)`
- **AND** 若返回 false，则导航被取消

#### Scenario: ViewModel can intercept NavigateTo
- **GIVEN** 目标 ViewModel 支持导航拦截（通过继承基类或实现接口）
- **WHEN** 导航到目标页面
- **THEN** 框架调用 `CanNavigateToAsync(context)`
- **AND** 若返回 false，则导航被取消且不改变当前页面

#### Scenario: ViewModel receives lifecycle callbacks
- **WHEN** 导航从 A 切换到 B 成功完成
- **THEN** 框架调用 A 的 `OnNavigatedFromAsync(context)`
- **AND** 调用 B 的 `OnNavigatedToAsync(context)`（包含参数）

### Requirement: Hierarchical Menu Navigation
Navigation menus SHALL support a two-level hierarchy where module menus are top-level and view menus are second-level when the module contains multiple views.

#### Scenario: Single-view module shows only module menu
- **GIVEN** 某模块仅包含 1 个 View（可导航页面）
- **AND** 该 View 仍声明了 View 级菜单元数据
- **WHEN** 导航菜单渲染
- **THEN** 主导航仅显示模块级菜单项（名称来自模块级菜单声明）
- **AND** 不显示二级 View 菜单

#### Scenario: Multi-view module shows module menu and view children
- **GIVEN** 某模块包含多个 View（可导航页面）
- **WHEN** 导航菜单渲染
- **THEN** 主导航显示模块级菜单项（名称来自模块级菜单声明）
- **AND** 该菜单项包含二级子项，每个子项名称来自对应 View 的菜单声明

#### Scenario: Multi-view module parent menu expands only
- **GIVEN** 某模块包含多个 View（可导航页面）
- **WHEN** 用户点击模块级父菜单项
- **THEN** 菜单仅展开/收起以显示/隐藏子菜单
- **AND** 不触发导航（父菜单项不包含可导航的 `NavigationKey`）


