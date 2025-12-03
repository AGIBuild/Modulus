## MODIFIED Requirements

### Requirement: SideNav Component
Each host MUST provide a SideNav/NavDrawer component that renders the navigation menu with main items and bottom items, supports collapse mode, and integrates with the navigation service.

#### Scenario: SideNav renders menu items from registry
- **WHEN** modules register menu items via IMenuRegistry
- **THEN** main items appear in the top section of the SideNav
- **AND** bottom items appear in the bottom section of the SideNav

#### Scenario: SideNav item selection triggers navigation
- **WHEN** user clicks a navigation item
- **THEN** the INavigationService is invoked with the item's NavigationKey
- **AND** navigation guards are evaluated
- **AND** the selected item is visually highlighted if navigation succeeds

#### Scenario: SideNav supports collapsed mode
- **WHEN** the navigation panel is toggled to collapsed state
- **THEN** only icons are displayed with tooltips on hover
- **AND** the panel width reduces to accommodate icons only

#### Scenario: SideNav shows badges on items
- **WHEN** a menu item has a non-zero BadgeCount
- **THEN** the badge is displayed adjacent to the item (visible in both expanded and collapsed modes)

#### Scenario: SideNav respects disabled state
- **WHEN** a menu item has IsEnabled = false
- **THEN** the item is rendered in a disabled visual state
- **AND** clicks on the item are ignored

### Requirement: TitleBar Component
Each host MUST provide a TitleBar component that encapsulates the application header including branding, navigation toggle button, window controls (Avalonia), and theme toggle button.

#### Scenario: TitleBar displays branding
- **WHEN** the application starts
- **THEN** the TitleBar displays "MODULUS" text and "HOST" badge
- **AND** the TitleBar respects the current theme (light/dark)

#### Scenario: Theme toggle in TitleBar
- **WHEN** user clicks the theme toggle button
- **THEN** the application theme switches between light and dark modes
- **AND** all components reflect the theme change

#### Scenario: Navigation toggle in TitleBar
- **WHEN** user clicks the navigation toggle button (hamburger menu)
- **THEN** the SideNav/NavDrawer toggles between expanded and collapsed states

