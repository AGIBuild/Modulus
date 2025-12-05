## Context
The Modulus framework supports two UI hosts (Avalonia and Blazor). Both need consistent navigation capabilities while maintaining independent implementations. The navigation system needs to support advanced features expected in modern desktop applications.

## Goals / Non-Goals
- Goals:
  - Provide collapsible navigation with icon-only mode and tooltips
  - Enable navigation interception (NavigateFrom/NavigateTo guards)
  - Support page instance lifecycle control (singleton vs new instance)
  - Implement full keyboard navigation
  - Support badges, disabled states, and context menus
  - Support hierarchical menu items (groups with children)
  
- Non-Goals:
  - Sharing UI code between Avalonia and Blazor (they remain independent)
  - Drag-and-drop menu reordering
  - Multi-level deep nesting (limit to 2 levels: parent + children)
  - Persistent user preferences for collapse state (future enhancement)

## Decisions

### 1. Navigation Service Architecture
- **Decision**: Introduce `INavigationService` in `Modulus.UI.Abstractions` with navigation guards
- **Why**: Provides UI-agnostic navigation control that works across both hosts
- **Alternatives considered**:
  - Per-host navigation services: Rejected (duplicates logic, harder to test)
  - Event-based system via MediatR: Rejected (overkill for synchronous UI navigation)

```csharp
public interface INavigationService
{
    // Navigation with interception support
    Task<bool> NavigateToAsync(string navigationKey, NavigationOptions? options = null);
    Task<bool> NavigateToAsync<TViewModel>(NavigationOptions? options = null);
    
    // Guards registration
    void RegisterNavigationGuard(INavigationGuard guard);
    void UnregisterNavigationGuard(INavigationGuard guard);
    
    // Current state
    string? CurrentNavigationKey { get; }
    event EventHandler<NavigationEventArgs>? Navigated;
}

public interface INavigationGuard
{
    Task<bool> CanNavigateFromAsync(NavigationContext context);
    Task<bool> CanNavigateToAsync(NavigationContext context);
}

public class NavigationOptions
{
    public bool ForceNewInstance { get; init; } = false;  // Override default lifecycle
    public IDictionary<string, object>? Parameters { get; init; }
}

public class NavigationContext
{
    public string? FromKey { get; init; }
    public string ToKey { get; init; } = "";
    public NavigationOptions Options { get; init; } = new();
}
```

### 2. Page Instance Lifecycle
- **Decision**: Add `InstanceMode` to `MenuItem` with values: `Default`, `Singleton`, `Transient`
- **Why**: Some pages need fresh state each visit (forms), others should preserve state (dashboards)
- **Implementation**: Navigation service caches singleton instances; transient always creates new

```csharp
public enum PageInstanceMode
{
    Default,    // Use host default (typically Singleton)
    Singleton,  // Reuse same instance
    Transient   // Create new instance each navigation
}
```

### 3. Enhanced MenuItem Model
- **Decision**: Extend `MenuItem` with new properties while maintaining backward compatibility

```csharp
public class MenuItem
{
    // Existing properties...
    public string Id { get; }
    public string DisplayName { get; }
    public string Icon { get; }
    public MenuLocation Location { get; }
    public string NavigationKey { get; }
    public int Order { get; }
    
    // New properties
    public bool IsEnabled { get; set; } = true;
    public int? BadgeCount { get; set; }
    public string? BadgeColor { get; set; }
    public PageInstanceMode InstanceMode { get; set; } = PageInstanceMode.Default;
    public IReadOnlyList<MenuItem>? Children { get; set; }  // For hierarchical menus
    public IReadOnlyList<MenuAction>? ContextActions { get; set; }  // Right-click menu
}

public class MenuAction
{
    public string Label { get; init; } = "";
    public string? Icon { get; init; }
    public Action<MenuItem> Execute { get; init; } = _ => { };
}
```

### 4. Avalonia Collapse Implementation
- **Decision**: Use existing `SplitView` with `IsPaneOpen` binding + toggle button in TitleBar
- **Why**: SplitView already supports CompactInline mode; minimize new code
- **Implementation**:
  - Add `IsNavCollapsed` property to ShellViewModel
  - Modify NavigationView to show icon-only when parent SplitView is collapsed
  - Add ToolTip to each nav item showing DisplayName when collapsed

### 5. Blazor Mini-Drawer Implementation
- **Decision**: Use MudDrawer `Variant="DrawerVariant.Mini"` with `MiniWidth` parameter
- **Why**: MudBlazor provides built-in mini drawer variant
- **Optimization**: Current `DrawerVariant.Responsive` works but doesn't support mini mode; switch to manual toggle with mini variant

### 6. Keyboard Navigation
- **Decision**: Implement at component level (not service level)
- **Avalonia**: Use `KeyDown` event handlers on NavigationView
- **Blazor**: Use `@onkeydown` with focus management
- **Keys**: 
  - Arrow Up/Down: Move selection
  - Enter/Space: Activate item
  - Arrow Right: Expand group
  - Arrow Left: Collapse group or move to parent
  - Tab: Standard focus navigation
  - Escape: Collapse all groups

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Breaking existing MenuItem consumers | Use optional properties with defaults |
| Performance with many nav items | Virtualization for lists >50 items (defer to future if needed) |
| Animation jank on low-end devices | CSS/XAML animations (hardware accelerated) |
| Guard async delays navigation | Add timeout (500ms default), log slow guards |

## Migration Plan
1. Add new interfaces/classes to UI.Abstractions (backward compatible)
2. Implement Avalonia NavigationService
3. Update Avalonia NavigationView with collapse support
4. Implement Blazor NavigationService  
5. Update Blazor NavDrawer with mini variant
6. Add keyboard navigation to both hosts
7. Add badges/disabled/context menu support

No breaking changes - all new features are additive.

## Open Questions
- [ ] Should collapsed state persist across app restarts? (Suggest: defer to future)
- [ ] Should guards run in parallel or sequential? (Suggest: sequential for predictability)

