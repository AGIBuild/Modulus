# navigation Specification

## Purpose
TBD - created by archiving change enhance-navigation-view. Update Purpose after archive.
## Requirements
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

### Requirement: Page Instance Lifecycle
The framework SHALL support configurable page instance modes that control whether views are reused or recreated on each navigation.

#### Scenario: Singleton mode reuses instance
- **GIVEN** a MenuItem configured with `InstanceMode = Singleton`
- **WHEN** user navigates to this item multiple times
- **THEN** the same view/viewmodel instance is displayed each time
- **AND** the view state is preserved between navigations

#### Scenario: Transient mode creates new instance
- **GIVEN** a MenuItem configured with `InstanceMode = Transient`
- **WHEN** user navigates to this item
- **THEN** a new view/viewmodel instance is created
- **AND** the previous instance is eligible for garbage collection

#### Scenario: ForceNewInstance option overrides lifecycle
- **WHEN** navigation is triggered with `NavigationOptions { ForceNewInstance = true }`
- **THEN** a new instance is created regardless of the MenuItem's InstanceMode setting

#### Scenario: Navigation parameters are passed
- **WHEN** navigation includes parameters in `NavigationOptions.Parameters`
- **THEN** the target viewmodel receives these parameters
- **AND** can use them for initialization

### Requirement: Collapsible Navigation Panel
Each host SHALL provide a collapsible navigation panel that can toggle between expanded and collapsed (icon-only) modes.

#### Scenario: Toggle collapse via button (Avalonia)
- **WHEN** user clicks the navigation toggle button in the title bar
- **THEN** the navigation panel toggles between expanded and collapsed states
- **AND** a smooth animation accompanies the transition

#### Scenario: Toggle collapse via button (Blazor)
- **WHEN** user clicks the menu button in the app bar
- **THEN** the navigation drawer toggles between expanded and mini modes
- **AND** the transition is animated

#### Scenario: Collapsed mode shows icons only
- **WHEN** the navigation panel is in collapsed state
- **THEN** only menu item icons are visible
- **AND** display names are hidden

#### Scenario: Tooltip on collapsed items
- **WHEN** the navigation panel is collapsed
- **AND** user hovers over a menu item icon
- **THEN** a tooltip displays the item's DisplayName

### Requirement: Keyboard Navigation
The navigation component SHALL support full keyboard navigation for accessibility and power users.

#### Scenario: Arrow key navigation
- **WHEN** the navigation list has focus
- **AND** user presses Arrow Up or Arrow Down
- **THEN** the selection moves to the previous or next item respectively

#### Scenario: Activate item with keyboard
- **WHEN** a menu item is selected
- **AND** user presses Enter or Space
- **THEN** the item is activated (navigation occurs)

#### Scenario: Expand group with keyboard
- **WHEN** a group item is selected
- **AND** user presses Arrow Right
- **THEN** the group expands to show children

#### Scenario: Collapse group with keyboard
- **WHEN** inside an expanded group
- **AND** user presses Arrow Left
- **THEN** the group collapses
- **OR** selection moves to parent item

#### Scenario: Escape collapses all groups
- **WHEN** user presses Escape in the navigation
- **THEN** all expanded groups collapse

### Requirement: Menu Item Badges
Menu items SHALL support badge indicators for displaying counts or status.

#### Scenario: Badge displays count
- **GIVEN** a MenuItem with `BadgeCount = 5`
- **WHEN** the navigation renders this item
- **THEN** a badge showing "5" appears next to the item

#### Scenario: Badge with custom color
- **GIVEN** a MenuItem with `BadgeCount = 3` and `BadgeColor = "error"`
- **WHEN** the navigation renders this item
- **THEN** the badge uses the error/red color scheme

#### Scenario: Badge hidden when null or zero
- **GIVEN** a MenuItem with `BadgeCount = null` or `BadgeCount = 0`
- **WHEN** the navigation renders this item
- **THEN** no badge is displayed

### Requirement: Disabled Menu Items
Menu items SHALL support a disabled state that prevents interaction while remaining visible.

#### Scenario: Disabled item is visually distinct
- **GIVEN** a MenuItem with `IsEnabled = false`
- **WHEN** the navigation renders this item
- **THEN** the item appears grayed out or with reduced opacity

#### Scenario: Disabled item blocks navigation
- **GIVEN** a MenuItem with `IsEnabled = false`
- **WHEN** user clicks on this item
- **THEN** no navigation occurs
- **AND** the item does not respond to hover states

#### Scenario: Disabled item blocks keyboard activation
- **GIVEN** a MenuItem with `IsEnabled = false`
- **WHEN** user selects this item and presses Enter
- **THEN** no navigation occurs

### Requirement: Hierarchical Menu Items
The navigation component SHALL support parent-child menu item relationships for grouping related items.

#### Scenario: Group item displays children
- **GIVEN** a MenuItem with `Children` containing sub-items
- **WHEN** user clicks the group item
- **THEN** the group expands to reveal child items

#### Scenario: Group toggle independent of navigation
- **GIVEN** a group MenuItem without a NavigationKey
- **WHEN** user clicks the group
- **THEN** the group toggles expanded/collapsed
- **AND** no navigation occurs

#### Scenario: Group with navigation key
- **GIVEN** a group MenuItem with both Children and a NavigationKey
- **WHEN** user clicks the group label
- **THEN** navigation occurs to the group's target
- **AND** click on expand icon toggles children visibility

#### Scenario: Child items indent
- **WHEN** children of a group are displayed
- **THEN** child items are visually indented relative to parent

### Requirement: Context Menu Actions
Menu items SHALL support right-click context menus with custom actions.

#### Scenario: Right-click shows context menu
- **GIVEN** a MenuItem with `ContextActions` defined
- **WHEN** user right-clicks on the item
- **THEN** a context menu appears with the defined actions

#### Scenario: Context action executes
- **WHEN** user clicks an action in the context menu
- **THEN** the action's Execute callback is invoked with the MenuItem
- **AND** the context menu closes

#### Scenario: No context menu when no actions
- **GIVEN** a MenuItem with `ContextActions = null` or empty
- **WHEN** user right-clicks on the item
- **THEN** no context menu appears (or browser default if Blazor)

### Requirement: Collapse Animation
The navigation panel collapse/expand transitions SHALL be animated for smooth user experience.

#### Scenario: Expand animation
- **WHEN** navigation panel transitions from collapsed to expanded
- **THEN** the panel width animates smoothly over approximately 200-300ms
- **AND** content fades in as space becomes available

#### Scenario: Collapse animation
- **WHEN** navigation panel transitions from expanded to collapsed
- **THEN** the panel width animates smoothly over approximately 200-300ms
- **AND** text content fades out before width reduces

### Requirement: Default Home Navigation

应用启动后，导航服务 SHALL 自动导航到 Home 主页模块，作为用户看到的第一个视图。

#### Scenario: Avalonia host navigates to Home on startup

- **WHEN** Avalonia Host 应用启动完成
- **AND** 所有模块初始化完毕
- **THEN** 自动导航到 `HomeViewModel`
- **AND** Home 主页视图显示在内容区域

#### Scenario: Blazor host routes to Home on startup

- **WHEN** Blazor Host 应用启动
- **AND** 用户访问根路径 `/`
- **THEN** 路由重定向到 `/home`
- **AND** Home 主页组件渲染

#### Scenario: Home menu item appears first in navigation

- **GIVEN** Home 模块已安装并注册
- **WHEN** 导航菜单渲染
- **THEN** Home 菜单项显示在主菜单列表的第一位（Order=1）
- **AND** 显示 Home 图标和 "Home" 或 "主页" 文字

### Requirement: Home Module Statistics Display

Home 主页 SHALL 展示框架和模块的统计信息，帮助用户了解当前系统状态。

#### Scenario: Display installed modules count

- **WHEN** Home 主页加载
- **THEN** 显示已安装模块的总数
- **AND** 统计数据从 `IModuleRepository` 获取

#### Scenario: Display running modules count

- **WHEN** Home 主页加载
- **THEN** 显示当前运行中模块的数量
- **AND** 运行中模块以视觉高亮方式在轨道区域展示

#### Scenario: Module card shows running status indicator

- **WHEN** Home 主页显示模块卡片
- **AND** 模块处于运行中状态
- **THEN** 卡片右上角显示绿色状态指示灯
- **AND** 指示灯有轻微闪烁动画

#### Scenario: Module card shows stopped status indicator

- **WHEN** Home 主页显示模块卡片
- **AND** 模块处于已停止状态
- **THEN** 卡片右上角显示灰色状态指示灯

#### Scenario: Display framework version

- **WHEN** Home 主页加载
- **THEN** 显示当前 Modulus 框架版本号

### Requirement: Home Module Visual Style

Home 主页 SHALL 采用"轨道控制中心"视觉风格，体现模块化框架的核心概念。

#### Scenario: Starfield background renders

- **WHEN** Home 主页显示
- **THEN** 背景显示深空渐变色
- **AND** 有细腻的星点粒子效果（可通过动画实现）

#### Scenario: Central logo with pulse animation

- **WHEN** Home 主页显示
- **THEN** 中央显示 Modulus Logo
- **AND** Logo 有发光脉冲动画效果

#### Scenario: Module cards float around core

- **WHEN** Home 主页显示
- **AND** 有已安装的模块
- **THEN** 模块以卡片形式围绕中央 Logo 展示
- **AND** 卡片有轻微的漂浮动画效果

#### Scenario: Feature cards grid

- **WHEN** Home 主页显示
- **THEN** 下方显示框架核心特性的卡片网格
- **AND** 包含多宿主架构、热重载、VS兼容、AI就绪等特性

#### Scenario: CLI quick start section

- **WHEN** Home 主页显示
- **THEN** 底部显示终端风格的 CLI 快速入门命令
- **AND** 展示 `modulus new`, `modulus build`, `modulus install` 等命令示例

### Requirement: Quick Create Module Action

Home 主页 SHALL 提供"快速创建模块"入口，方便用户快速开始开发新模块。

#### Scenario: Quick create button displayed

- **WHEN** Home 主页显示
- **THEN** CLI 命令区域旁显示"创建模块"按钮
- **AND** 按钮样式与整体视觉风格一致

#### Scenario: Quick create action triggered

- **WHEN** 用户点击"创建模块"按钮
- **THEN** 打开模块创建向导对话框
- **OR** 跳转到模块开发文档页面（如果向导未实现）

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

