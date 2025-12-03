using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Modulus.UI.Abstractions;

public enum MenuLocation
{
    Main,
    Bottom
}

public class MenuItem : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    private int? _badgeCount;
    private string? _badgeColor;
    private bool _isExpanded;

    // Core properties (read-only)
    public string Id { get; }
    public string DisplayName { get; }
    public string Icon { get; }
    public MenuLocation Location { get; }
    public string NavigationKey { get; } // View Key or Route
    public int Order { get; }

    /// <summary>
    /// Whether the menu item is enabled. Disabled items are visible but not clickable.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    /// <summary>
    /// Badge count to display. Null or 0 hides the badge.
    /// </summary>
    public int? BadgeCount
    {
        get => _badgeCount;
        set => SetField(ref _badgeCount, value);
    }

    /// <summary>
    /// Badge color identifier (e.g., "error", "warning", "info", "success").
    /// </summary>
    public string? BadgeColor
    {
        get => _badgeColor;
        set => SetField(ref _badgeColor, value);
    }

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

    /// <summary>
    /// Whether this group item is currently expanded (for hierarchical menus).
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetField(ref _isExpanded, value);
    }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
