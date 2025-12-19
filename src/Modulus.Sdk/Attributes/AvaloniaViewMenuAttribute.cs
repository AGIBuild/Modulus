using Modulus.UI.Abstractions;

namespace Modulus.Sdk;

/// <summary>
/// Declares a view-level navigation menu item for an Avalonia UI module.
/// Applied to a ViewModel type that is a navigable target.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AvaloniaViewMenuAttribute : Attribute
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
    /// Initializes a new instance of the <see cref="AvaloniaViewMenuAttribute"/> class.
    /// </summary>
    /// <param name="key">Unique key for this view menu item within the module.</param>
    /// <param name="displayName">Display name shown in navigation.</param>
    public AvaloniaViewMenuAttribute(string key, string displayName)
    {
        Key = key;
        DisplayName = displayName;
    }
}


