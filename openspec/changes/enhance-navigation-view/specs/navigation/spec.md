## ADDED Requirements

### Requirement: Navigation Service
The framework SHALL provide an `INavigationService` abstraction that enables programmatic navigation with interception capabilities across all hosts.

#### Scenario: Navigate to a registered view
- **WHEN** a module calls `NavigateToAsync(navigationKey)`
- **THEN** the navigation service resolves the target view/viewmodel
- **AND** the content area displays the corresponding view

#### Scenario: Navigate with typed ViewModel
- **WHEN** a module calls `NavigateToAsync<TViewModel>()`
- **THEN** the navigation service resolves TViewModel
- **AND** creates or retrieves the view instance based on lifecycle settings
- **AND** the content area displays the view

#### Scenario: Navigation exposes current state
- **WHEN** navigation completes successfully
- **THEN** `CurrentNavigationKey` reflects the active navigation target
- **AND** the `Navigated` event is raised with navigation details

### Requirement: Navigation Guards
The framework SHALL support registration of navigation guards that can intercept and conditionally prevent navigation.

#### Scenario: Guard prevents navigation from current page
- **WHEN** user triggers navigation to a new page
- **AND** a registered guard's `CanNavigateFromAsync` returns false
- **THEN** the navigation is cancelled
- **AND** the current view remains displayed
- **AND** no `Navigated` event is raised

#### Scenario: Guard prevents navigation to target page
- **WHEN** user triggers navigation to a new page
- **AND** a registered guard's `CanNavigateToAsync` returns false
- **THEN** the navigation is cancelled
- **AND** the current view remains displayed

#### Scenario: Multiple guards are evaluated
- **WHEN** multiple guards are registered
- **THEN** all guards are evaluated sequentially
- **AND** navigation proceeds only if all guards return true

#### Scenario: Guard receives navigation context
- **WHEN** a guard is invoked
- **THEN** it receives `NavigationContext` containing source key, target key, and options
- **AND** can make decisions based on this context

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

