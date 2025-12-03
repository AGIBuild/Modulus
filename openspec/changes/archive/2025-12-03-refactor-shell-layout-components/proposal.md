# Change: Refactor Shell Layout into Composable Components

## Why
The current MainWindow.axaml (Avalonia) and MainLayout.razor (Blazor) are monolithic files (~100-200 lines) that combine multiple concerns: title bar, navigation drawer, content hosting, theme management, and icon mapping. Modifying a single element (e.g., navigation menu) requires understanding the entire layout structure, increasing cognitive load and risk of unintended side effects.

## What Changes
- Extract **TitleBar/AppBar** into a standalone component
- Extract **SideNav/Drawer** (navigation menu) into a standalone component
- Extract **ContentHost** (main content area) into a standalone component
- Extract **ThemeProvider** logic into a dedicated component (Blazor)
- Refactor MainWindow/MainLayout to compose these components
- Maintain identical visual appearance and behavior

## Impact
- Affected specs: `shell-layout` (new capability)
- Affected code:
  - `src/Hosts/Modulus.Host.Avalonia/MainWindow.axaml`
  - `src/Hosts/Modulus.Host.Avalonia/Shell/` (new components)
  - `src/Hosts/Modulus.Host.Blazor/Components/Layout/MainLayout.razor`
  - `src/Hosts/Modulus.Host.Blazor/Components/Layout/` (new components)

