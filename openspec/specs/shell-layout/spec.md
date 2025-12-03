# shell-layout Specification

## Purpose
TBD - created by archiving change refactor-shell-layout-components. Update Purpose after archive.
## Requirements
### Requirement: Composable Shell Layout
The shell layout for each host MUST be composed of discrete, single-responsibility components that can be modified independently without requiring changes to other layout components.

#### Scenario: Modify TitleBar without affecting navigation
- **WHEN** a developer modifies the TitleBar/AppBar component
- **THEN** the SideNav/NavDrawer and ContentHost components remain unchanged
- **AND** the application compiles and runs correctly

#### Scenario: Modify navigation menu without understanding full layout
- **WHEN** a developer needs to add or change navigation items
- **THEN** they only need to understand the SideNav/NavDrawer component
- **AND** they do not need to modify MainWindow/MainLayout directly

### Requirement: TitleBar Component
Each host MUST provide a TitleBar component that encapsulates the application header including branding, window controls (Avalonia), and theme toggle button.

#### Scenario: TitleBar displays branding
- **WHEN** the application starts
- **THEN** the TitleBar displays "MODULUS" text and "HOST" badge
- **AND** the TitleBar respects the current theme (light/dark)

#### Scenario: Theme toggle in TitleBar
- **WHEN** user clicks the theme toggle button
- **THEN** the application theme switches between light and dark modes
- **AND** all components reflect the theme change

### Requirement: SideNav Component
Each host MUST provide a SideNav/NavDrawer component that renders the navigation menu with main items and bottom items.

#### Scenario: SideNav renders menu items from registry
- **WHEN** modules register menu items via IMenuRegistry
- **THEN** main items appear in the top section of the SideNav
- **AND** bottom items appear in the bottom section of the SideNav

#### Scenario: SideNav item selection triggers navigation
- **WHEN** user clicks a navigation item
- **THEN** the ContentHost displays the corresponding view
- **AND** the selected item is visually highlighted

### Requirement: ContentHost Component
Each host MUST provide a ContentHost component that serves as the container for module views with appropriate styling (rounded corners, padding, background).

#### Scenario: ContentHost displays module view
- **WHEN** navigation occurs to a module view
- **THEN** the ContentHost renders the view with consistent styling
- **AND** the content area has the expected visual appearance (rounded corners, padding)

### Requirement: ThemeProvider Component (Blazor)
The Blazor host MUST provide a ThemeProvider component that manages MudBlazor theme configuration and provides theme state to child components.

#### Scenario: ThemeProvider initializes theme
- **WHEN** the Blazor application starts
- **THEN** the ThemeProvider applies the configured theme palette
- **AND** all MudBlazor components use the correct colors

#### Scenario: ThemeProvider responds to theme changes
- **WHEN** the theme is changed via IThemeService
- **THEN** the ThemeProvider updates the MudThemeProvider
- **AND** all components reflect the new theme

