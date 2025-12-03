using System.Collections;
using Avalonia;
using Avalonia.Controls;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.UI.Avalonia.Components;

/// <summary>
/// Side navigation component with main and bottom menu sections.
/// </summary>
public partial class SideNav : UserControl
{
    public static readonly StyledProperty<IEnumerable?> MainItemsProperty =
        AvaloniaProperty.Register<SideNav, IEnumerable?>(nameof(MainItems));

    public static readonly StyledProperty<IEnumerable?> BottomItemsProperty =
        AvaloniaProperty.Register<SideNav, IEnumerable?>(nameof(BottomItems));

    public static readonly StyledProperty<UiMenuItem?> MainSelectedItemProperty =
        AvaloniaProperty.Register<SideNav, UiMenuItem?>(
            nameof(MainSelectedItem),
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<UiMenuItem?> BottomSelectedItemProperty =
        AvaloniaProperty.Register<SideNav, UiMenuItem?>(
            nameof(BottomSelectedItem),
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<SideNav, bool>(nameof(IsCollapsed));

    /// <summary>
    /// Main menu items (top section).
    /// </summary>
    public IEnumerable? MainItems
    {
        get => GetValue(MainItemsProperty);
        set => SetValue(MainItemsProperty, value);
    }

    /// <summary>
    /// Bottom menu items (bottom section).
    /// </summary>
    public IEnumerable? BottomItems
    {
        get => GetValue(BottomItemsProperty);
        set => SetValue(BottomItemsProperty, value);
    }

    /// <summary>
    /// Selected item in main menu.
    /// </summary>
    public UiMenuItem? MainSelectedItem
    {
        get => GetValue(MainSelectedItemProperty);
        set => SetValue(MainSelectedItemProperty, value);
    }

    /// <summary>
    /// Selected item in bottom menu.
    /// </summary>
    public UiMenuItem? BottomSelectedItem
    {
        get => GetValue(BottomSelectedItemProperty);
        set => SetValue(BottomSelectedItemProperty, value);
    }

    /// <summary>
    /// Whether the navigation is collapsed (icon-only mode).
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    public SideNav()
    {
        InitializeComponent();
    }
}

