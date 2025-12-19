using CommunityToolkit.Mvvm.ComponentModel;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.ComponentsDemo.ViewModels;

/// <summary>
/// ViewModel for navigation feature demonstration.
/// </summary>
public class NavigationDemoViewModel : ViewModelBase
{
    public string Description => "This demo showcases all navigation features including:\n" +
        "• Collapsible navigation panel\n" +
        "• Icon-only mode with tooltips\n" +
        "• Navigation guards (CanNavigateFrom/To)\n" +
        "• Page instance lifecycle (Singleton/Transient)";
}

/// <summary>
/// ViewModel for badge demonstration.
/// </summary>
public class BadgeDemoViewModel : ViewModelBase
{
    public string Description => "Badges can display notification counts on menu items.\n" +
        "• BadgeCount: The number to display\n" +
        "• BadgeColor: 'error', 'warning', 'info', 'success'\n" +
        "• Badge disappears when count is 0 or null";

    public IReadOnlyList<BadgeExample> Examples { get; } = new List<BadgeExample>
    {
        new("Error Badge", 5, "error"),
        new("Warning Badge", 12, "warning"),
        new("Info Badge", 3, "info"),
        new("Success Badge", 1, "success"),
        new("No Badge", null, null)
    };
}

public record BadgeExample(string Name, int? Count, string? Color);

/// <summary>
/// ViewModel for disabled state demonstration.
/// </summary>
public class DisabledDemoViewModel : ViewModelBase
{
    public string Description => "Menu items can be disabled to prevent interaction.\n" +
        "• IsEnabled = false makes item grayed out\n" +
        "• Disabled items don't respond to clicks\n" +
        "• Keyboard navigation skips disabled items";
}

/// <summary>
/// ViewModel for hierarchy/sub-item demonstration.
/// </summary>
public class HierarchyDemoViewModel : ViewModelBase
{
    public string ItemId { get; set; } = "";
    public string Description => $"This is a sub-item of the hierarchical menu.\nItem ID: {ItemId}";
}

/// <summary>
/// ViewModel for context menu demonstration.
/// </summary>
public partial class ContextMenuDemoViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _lastAction = "Right-click on 'Context Menu Demo' in the navigation to see the context menu.";

    public string Description => "Context menus provide additional actions for menu items.\n" +
        "• Define ContextActions on MenuItem\n" +
        "• Right-click to show the menu\n" +
        "• Each action has Label, Icon, and Execute callback";

    public void SetLastAction(string action)
    {
        LastAction = $"{action} triggered at {DateTime.Now:HH:mm:ss}";
    }
}

/// <summary>
/// ViewModel for keyboard navigation demonstration.
/// </summary>
public class KeyboardDemoViewModel : ViewModelBase
{
    public string Description => "Full keyboard navigation support:\n" +
        "• Arrow Up/Down: Move selection\n" +
        "• Enter/Space: Activate item\n" +
        "• Arrow Right: Expand group\n" +
        "• Arrow Left: Collapse group\n" +
        "• Escape: Collapse all groups\n" +
        "• Tab: Standard focus navigation\n\n" +
        "Focus the navigation panel and try the keys!";

    public IReadOnlyList<KeyboardShortcut> Shortcuts { get; } = new List<KeyboardShortcut>
    {
        new("↑ / ↓", "Move selection up/down"),
        new("Enter / Space", "Activate selected item"),
        new("→", "Expand group"),
        new("←", "Collapse group"),
        new("Escape", "Collapse all groups"),
        new("Tab", "Standard focus navigation")
    };
}

public record KeyboardShortcut(string Key, string Action);

/// <summary>
/// ViewModel for page lifecycle demonstration.
/// </summary>
public partial class LifecycleDemoViewModel : ViewModelBase
{
    private static int _instanceCounter;

    [ObservableProperty]
    private int _instanceId;

    [ObservableProperty]
    private DateTime _createdAt;

    public string Description => "Page instance lifecycle control:\n" +
        "• Default: Use host default (Singleton)\n" +
        "• Singleton: Same instance reused\n" +
        "• Transient: New instance each navigation\n\n" +
        "This page uses Transient mode - notice a new instance is created each time.";

    public LifecycleDemoViewModel()
    {
        InstanceId = ++_instanceCounter;
        CreatedAt = DateTime.Now;
    }
}

