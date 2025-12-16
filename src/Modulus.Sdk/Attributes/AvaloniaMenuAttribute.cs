using Modulus.UI.Abstractions;

namespace Modulus.Sdk;

/// <summary>
/// Declares a navigation menu item for Avalonia UI module.
/// Applied to the UI.Avalonia module class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AvaloniaMenuAttribute : Attribute
{
    /// <summary>
    /// Unique key for this menu item (used for grouping duplicates).
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Display name shown in navigation.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Icon for this menu item.
    /// </summary>
    public IconKind Icon { get; set; } = IconKind.Grid;

    /// <summary>
    /// ViewModel type to navigate to.
    /// </summary>
    public Type ViewModelType { get; }

    /// <summary>
    /// Menu location: Main or Bottom.
    /// </summary>
    public MenuLocation Location { get; set; } = MenuLocation.Main;

    /// <summary>
    /// Sort order (lower = higher priority).
    /// </summary>
    public int Order { get; set; } = 50;

    public AvaloniaMenuAttribute(string key, string displayName, Type viewModelType)
    {
        Key = key;
        DisplayName = displayName;
        ViewModelType = viewModelType;
    }
}

