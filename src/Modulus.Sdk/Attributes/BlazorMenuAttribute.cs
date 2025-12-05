using Modulus.UI.Abstractions;

namespace Modulus.Sdk;

/// <summary>
/// Declares a navigation menu item for Blazor UI module.
/// Applied to the UI.Blazor module class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class BlazorMenuAttribute : Attribute
{
    /// <summary>
    /// Display name shown in navigation.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Route path (e.g., "/notes", "/echo").
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// Icon for the menu item.
    /// </summary>
    public IconKind Icon { get; set; } = IconKind.Grid;

    /// <summary>
    /// Menu location: Main or Bottom.
    /// </summary>
    public MenuLocation Location { get; set; } = MenuLocation.Main;

    /// <summary>
    /// Sort order (lower = higher priority).
    /// </summary>
    public int Order { get; set; } = 50;

    public BlazorMenuAttribute(string displayName, string route)
    {
        DisplayName = displayName;
        Route = route;
    }
}

