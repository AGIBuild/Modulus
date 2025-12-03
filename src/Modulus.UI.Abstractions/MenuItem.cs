using System;

namespace Modulus.UI.Abstractions;

public enum MenuLocation
{
    Main,
    Bottom
}

public class MenuItem
{
    public string Id { get; }
    public string DisplayName { get; }
    public string Icon { get; }
    public MenuLocation Location { get; }
    public string NavigationKey { get; } // View Key or Route
    public int Order { get; }
    
    public MenuItem(string id, string displayName, string icon, string navigationKey, MenuLocation location = MenuLocation.Main, int order = 0)
    {
        Id = id;
        DisplayName = displayName;
        Icon = icon;
        NavigationKey = navigationKey;
        Location = location;
        Order = order;
    }
}

