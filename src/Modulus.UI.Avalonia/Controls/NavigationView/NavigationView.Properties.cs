using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls.Primitives;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// NavigationView property definitions.
/// </summary>
public partial class NavigationView
{
    #region Items Property

    /// <summary>
    /// Defines the <see cref="Items"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<NavigationView, IEnumerable?>(
            nameof(Items),
            defaultValue: null);

    /// <summary>
    /// Gets or sets the collection of menu items.
    /// </summary>
    public IEnumerable? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    #endregion

    #region SelectedItem Property

    /// <summary>
    /// Defines the <see cref="SelectedItem"/> property.
    /// </summary>
    public static readonly StyledProperty<UiMenuItem?> SelectedItemProperty =
        AvaloniaProperty.Register<NavigationView, UiMenuItem?>(
            nameof(SelectedItem),
            defaultValue: null,
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    /// <summary>
    /// Gets or sets the currently selected menu item.
    /// </summary>
    public UiMenuItem? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    #endregion

    #region IsCollapsed Property

    /// <summary>
    /// Defines the <see cref="IsCollapsed"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<NavigationView, bool>(
            nameof(IsCollapsed),
            defaultValue: false);

    /// <summary>
    /// Gets or sets whether the navigation is in collapsed (icon-only) mode.
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    #endregion

    #region CompactModeWidth Property

    /// <summary>
    /// Defines the <see cref="CompactModeWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> CompactModeWidthProperty =
        AvaloniaProperty.Register<NavigationView, double>(
            nameof(CompactModeWidth),
            defaultValue: 48.0);

    /// <summary>
    /// Gets or sets the width of the navigation pane in compact (collapsed) mode.
    /// </summary>
    public double CompactModeWidth
    {
        get => GetValue(CompactModeWidthProperty);
        set => SetValue(CompactModeWidthProperty, value);
    }

    #endregion

    #region ExpandedModeWidth Property

    /// <summary>
    /// Defines the <see cref="ExpandedModeWidth"/> property.
    /// </summary>
    public static readonly StyledProperty<double> ExpandedModeWidthProperty =
        AvaloniaProperty.Register<NavigationView, double>(
            nameof(ExpandedModeWidth),
            defaultValue: 220.0);

    /// <summary>
    /// Gets or sets the width of the navigation pane in expanded mode.
    /// </summary>
    public double ExpandedModeWidth
    {
        get => GetValue(ExpandedModeWidthProperty);
        set => SetValue(ExpandedModeWidthProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when a navigation item is selected.
    /// </summary>
    public event EventHandler<UiMenuItem>? SelectionChanged;

    /// <summary>
    /// Raised when a navigation item is invoked (clicked).
    /// </summary>
    public event EventHandler<UiMenuItem>? ItemInvoked;

    #endregion
}

