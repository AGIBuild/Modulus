# Change: Enhance NavigationView with Collapse Mode and Full Desktop Navigation Features

## Why
The current NavigationView components in both Avalonia and Blazor hosts lack essential desktop navigation features. Users cannot collapse the navigation panel to save screen space, there is no navigation interception mechanism, and common features like badges, keyboard navigation, and hierarchical menus are missing.

## What Changes
- Add collapsible navigation panel support for Avalonia (manual toggle button)
- Add icon-only mode with tooltips when collapsed
- Introduce `INavigationService` abstraction with NavigateFrom/NavigateTo interception
- Add page instance lifecycle control (singleton vs transient per navigation)
- Add collapse/expand animations
- Add menu item grouping/hierarchy (sub-menus)
- Add keyboard navigation support (Tab, Arrow keys, Enter)
- Add badge/notification indicators on menu items
- Add disabled state for menu items
- Add context menu support (right-click)
- Optimize Blazor NavDrawer mini-variant mode

## Impact
- Affected specs: `shell-layout`, `navigation` (new)
- Affected code:
  - `src/Modulus.UI.Abstractions/` - New interfaces (INavigationService, enhanced MenuItem)
  - `src/Hosts/Modulus.Host.Avalonia/Components/` - NavigationView, SideNav enhancements
  - `src/Hosts/Modulus.Host.Blazor/Components/Layout/` - NavDrawer, NavMenu enhancements
  - `src/Hosts/Modulus.Host.Avalonia/MainWindow.axaml` - SplitView toggle integration
  - `src/Hosts/Modulus.Host.Avalonia/Shell/ViewModels/ShellViewModel.cs` - Navigation service integration

