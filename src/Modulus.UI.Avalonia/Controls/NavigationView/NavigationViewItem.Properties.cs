using Avalonia;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// NavigationViewItem property definitions.
/// </summary>
public partial class NavigationViewItem
{
    #region Item Property

    /// <summary>
    /// Defines the <see cref="Item"/> property.
    /// </summary>
    public static readonly StyledProperty<UiMenuItem?> ItemProperty =
        AvaloniaProperty.Register<NavigationViewItem, UiMenuItem?>(
            nameof(Item),
            defaultValue: null);

    /// <summary>
    /// Gets or sets the menu item data.
    /// </summary>
    public UiMenuItem? Item
    {
        get => GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    #endregion

    #region IsSelected Property

    /// <summary>
    /// Defines the <see cref="IsSelected"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<NavigationViewItem, bool>(
            nameof(IsSelected),
            defaultValue: false);

    /// <summary>
    /// Gets or sets whether this item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    #endregion

    #region IsExpanded Property

    /// <summary>
    /// Defines the <see cref="IsExpanded"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<NavigationViewItem, bool>(
            nameof(IsExpanded),
            defaultValue: false);

    /// <summary>
    /// Gets or sets whether this item's children are expanded.
    /// </summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    #endregion

    #region Depth Property

    /// <summary>
    /// Defines the <see cref="Depth"/> property.
    /// </summary>
    public static readonly StyledProperty<int> DepthProperty =
        AvaloniaProperty.Register<NavigationViewItem, int>(
            nameof(Depth),
            defaultValue: 0);

    /// <summary>
    /// Gets or sets the nesting depth of this item.
    /// </summary>
    public int Depth
    {
        get => GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    #endregion

    #region IsCollapsed Property

    /// <summary>
    /// Defines the <see cref="IsCollapsed"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<NavigationViewItem, bool>(
            nameof(IsCollapsed),
            defaultValue: false);

    /// <summary>
    /// Gets or sets whether the parent NavigationView is in collapsed mode.
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    #endregion
}

