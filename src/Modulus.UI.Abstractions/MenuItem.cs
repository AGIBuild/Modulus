using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Modulus.UI.Abstractions;

public enum MenuLocation
{
    Main,
    Bottom
}

public partial class MenuItem : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private int? _badgeCount;

    [ObservableProperty]
    private string? _badgeColor;

    [ObservableProperty]
    private bool _isExpanded;

    // Core properties (read-only)
    public string Id { get; }
    public string DisplayName { get; }
    public string Icon { get; }
    public MenuLocation Location { get; }
    public string NavigationKey { get; } // View Key or Route
    public int Order { get; }

    /// <summary>
    /// Controls page instance lifecycle during navigation.
    /// </summary>
    public PageInstanceMode InstanceMode { get; set; } = PageInstanceMode.Default;

    /// <summary>
    /// Child menu items for hierarchical menus. Null for leaf items.
    /// </summary>
    public IReadOnlyList<MenuItem>? Children { get; set; }

    /// <summary>
    /// Context menu actions shown on right-click. Null for no context menu.
    /// </summary>
    public IReadOnlyList<MenuAction>? ContextActions { get; set; }

    public MenuItem(string id, string displayName, string icon, string navigationKey, MenuLocation location = MenuLocation.Main, int order = 0)
    {
        Id = id;
        DisplayName = displayName;
        Icon = icon;
        NavigationKey = navigationKey;
        Location = location;
        Order = order;
    }

    /// <summary>
    /// Creates a group menu item with children.
    /// </summary>
    public static MenuItem CreateGroup(string id, string displayName, string icon, IReadOnlyList<MenuItem> children, MenuLocation location = MenuLocation.Main, int order = 0)
    {
        return new MenuItem(id, displayName, icon, string.Empty, location, order)
        {
            Children = children
        };
    }
}
