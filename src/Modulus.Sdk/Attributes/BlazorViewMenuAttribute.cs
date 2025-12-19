using Modulus.UI.Abstractions;

namespace Modulus.Sdk;

/// <summary>
/// Declares a view-level navigation menu item for a Blazor UI module.
/// Applied to a Razor component (generated type) that is a navigable page (typically contains <c>@page</c>).
/// This attribute provides menu metadata only; the route SHOULD come from <c>Microsoft.AspNetCore.Components.RouteAttribute</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class BlazorViewMenuAttribute : Attribute
{
    /// <summary>
    /// Unique key for this view menu item within the module.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Display name shown in navigation.
    /// </summary>
    public string DisplayName { get; }

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

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorViewMenuAttribute"/> class.
    /// </summary>
    /// <param name="key">Unique key for this view menu item within the module.</param>
    /// <param name="displayName">Display name shown in navigation.</param>
    public BlazorViewMenuAttribute(string key, string displayName)
    {
        Key = key;
        DisplayName = displayName;
    }
}


