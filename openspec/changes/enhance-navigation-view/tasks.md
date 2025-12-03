## 1. Core Abstractions (UI.Abstractions)
- [x] 1.1 Add `PageInstanceMode` enum (Default, Singleton, Transient)
- [x] 1.2 Add `MenuAction` class for context menu actions
- [x] 1.3 Extend `MenuItem` with IsEnabled, BadgeCount, BadgeColor, InstanceMode, Children, ContextActions
- [x] 1.4 Add `NavigationOptions` class with ForceNewInstance and Parameters
- [x] 1.5 Add `NavigationContext` class with FromKey, ToKey, Options
- [x] 1.6 Add `NavigationEventArgs` for Navigated event
- [x] 1.7 Add `INavigationGuard` interface with CanNavigateFromAsync/CanNavigateToAsync
- [x] 1.8 Add `INavigationService` interface with NavigateToAsync, guard registration, CurrentNavigationKey, Navigated event

## 2. Avalonia Host - Navigation Service
- [x] 2.1 Implement `NavigationService` class for Avalonia
- [x] 2.2 Integrate singleton/transient instance caching in NavigationService
- [x] 2.3 Register NavigationService in DI container
- [x] 2.4 Refactor ShellViewModel to use INavigationService instead of direct navigation

## 3. Avalonia Host - Collapse Mode
- [x] 3.1 Add `IsNavCollapsed` property to ShellViewModel (or create dedicated NavViewModel)
- [x] 3.2 Add toggle button to TitleBar component
- [x] 3.3 Bind SplitView.IsPaneOpen to IsNavCollapsed (inverted)
- [x] 3.4 Update NavigationView to detect collapsed state from parent SplitView
- [x] 3.5 Show icon-only mode when collapsed
- [x] 3.6 Add ToolTip to nav items displaying DisplayName when collapsed
- [x] 3.7 Add collapse/expand animation via SplitView transitions

## 4. Avalonia Host - Enhanced Features
- [x] 4.1 Add badge rendering to NavigationView item template
- [x] 4.2 Add disabled state styling to navigation items
- [ ] 4.3 Implement hierarchical menu item expansion/collapse
- [ ] 4.4 Add keyboard navigation handlers (Arrow, Enter, Space, Escape)
- [ ] 4.5 Implement context menu for nav items with ContextActions

## 5. Blazor Host - Navigation Service
- [x] 5.1 Implement `NavigationService` class for Blazor (using NavigationManager)
- [x] 5.2 Integrate singleton/transient instance caching
- [x] 5.3 Register NavigationService in DI container
- [x] 5.4 Update MainLayout to use INavigationService

## 6. Blazor Host - Mini Drawer Mode
- [x] 6.1 Change NavDrawer to use DrawerVariant.Mini with manual toggle
- [x] 6.2 Configure MiniWidth for icon-only display
- [x] 6.3 Add tooltip support for collapsed nav items
- [x] 6.4 Ensure smooth transition animation

## 7. Blazor Host - Enhanced Features
- [x] 7.1 Add MudBadge to NavMenu items for BadgeCount
- [x] 7.2 Add disabled state to MudNavLink items
- [x] 7.3 Implement hierarchical NavMenu with MudNavGroup
- [ ] 7.4 Add @onkeydown handlers for keyboard navigation
- [ ] 7.5 Implement context menu using MudMenu on right-click

## 8. Testing
- [ ] 8.1 Unit tests for INavigationService implementation (guard evaluation, lifecycle)
- [ ] 8.2 Unit tests for MenuItem enhanced properties
- [ ] 8.3 Integration tests for navigation flow with guards
