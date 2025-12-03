## 1. Avalonia Host Components

- [x] 1.1 Create `Shell/Components/` directory structure
- [x] 1.2 Extract TitleBar component (`TitleBar.axaml` + code-behind)
- [x] 1.3 Extract SideNav component (`SideNav.axaml` + code-behind)
- [x] 1.4 Extract ContentHost component (`ContentHost.axaml` + code-behind)
- [x] 1.5 Refactor MainWindow.axaml to compose the three components
- [x] 1.6 Verify visual parity with original layout

## 2. Blazor Host Components

- [x] 2.1 Extract AppBar component (`AppBar.razor`)
- [x] 2.2 Extract NavDrawer component (`NavDrawer.razor`) with icon mapping
- [x] 2.3 Extract ContentHost component (`ContentHost.razor`)
- [x] 2.4 Extract ThemeProvider component (`ThemeProvider.razor`) with theme configuration
- [x] 2.5 Refactor MainLayout.razor to compose the four components
- [x] 2.6 Verify visual parity with original layout

## 3. Validation

- [x] 3.1 Run both hosts and verify navigation works correctly
- [x] 3.2 Verify theme toggle works in both hosts
- [x] 3.3 Ensure module views still render properly in ContentHost
- [x] 3.4 Run existing integration tests
