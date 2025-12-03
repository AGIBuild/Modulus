## Context
The shell layout is the entry point for users in both Avalonia and Blazor hosts. Current implementation uses single files that mix structural layout with behavior (navigation, theming). This makes targeted changes difficult and increases the learning curve for contributors.

## Goals / Non-Goals
- **Goals**:
  - Decompose layout into single-responsibility components
  - Enable isolated modification of TitleBar, SideNav, ContentHost
  - Preserve existing visual design and behavior
  - Follow each host's idiomatic patterns (UserControl for Avalonia, Razor components for Blazor)

- **Non-Goals**:
  - UI redesign or visual changes
  - Adding new features to navigation or theming
  - Abstracting components across hosts (each host has its own implementation)

## Constraints
- **Blazor**: Use MudBlazor components wherever possible; avoid custom implementations
- **Avalonia**: Follow component best practices (DataContext binding, StyledProperty for bindable properties)

## Decisions

### Component Breakdown

| Component | Avalonia | Blazor | Responsibility |
|-----------|----------|--------|----------------|
| **TitleBar** | `TitleBar.axaml` | `AppBar.razor` | App branding, window controls, theme toggle |
| **SideNav** | `SideNav.axaml` | `NavDrawer.razor` | Navigation menu rendering, item selection |
| **ContentHost** | `ContentHost.axaml` | `ContentHost.razor` | Content area with rounded corners, padding |
| **ThemeProvider** | N/A (in App.axaml) | `ThemeProvider.razor` | MudTheme configuration, dark/light toggle |

### Communication Pattern
- Components communicate via:
  - **Avalonia**: DataContext binding to ShellViewModel (existing pattern)
  - **Blazor**: Cascading parameters and event callbacks

### File Structure

```
Avalonia:
src/Hosts/Modulus.Host.Avalonia/
├── MainWindow.axaml          # Simplified: composes TitleBar + SideNav + ContentHost
└── Shell/
    └── Components/
        ├── TitleBar.axaml
        ├── SideNav.axaml
        └── ContentHost.axaml

Blazor:
src/Hosts/Modulus.Host.Blazor/Components/Layout/
├── MainLayout.razor          # Simplified: composes components
├── AppBar.razor
├── NavDrawer.razor
├── ContentHost.razor
└── ThemeProvider.razor
```

## Risks / Trade-offs
- **Risk**: Component boundaries may require passing more parameters
  - *Mitigation*: Use ShellViewModel (Avalonia) / CascadingParameters (Blazor) to minimize prop drilling
- **Risk**: Slight increase in file count
  - *Mitigation*: Smaller, focused files are easier to navigate and maintain

## Open Questions
1. Should icon mapping logic (`GetIcon` in Blazor) move to a shared utility or stay in NavDrawer?
   - **Recommendation**: Keep in NavDrawer for now; extract if reused elsewhere

