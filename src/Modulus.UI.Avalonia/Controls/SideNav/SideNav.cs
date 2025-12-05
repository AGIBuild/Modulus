using System.Collections;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Modulus.UI.Abstractions;

namespace Modulus.UI.Avalonia.Controls;

/// <summary>
/// A side navigation control with main and bottom menu sections.
/// </summary>
public class SideNav : TemplatedControl
{
    protected override Type StyleKeyOverride => typeof(SideNav);

    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="MainItems"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> MainItemsProperty =
        AvaloniaProperty.Register<SideNav, IEnumerable?>(nameof(MainItems));

    /// <summary>
    /// Defines the <see cref="BottomItems"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable?> BottomItemsProperty =
        AvaloniaProperty.Register<SideNav, IEnumerable?>(nameof(BottomItems));

    /// <summary>
    /// Defines the <see cref="MainSelectedItem"/> property.
    /// </summary>
    public static readonly StyledProperty<MenuItem?> MainSelectedItemProperty =
        AvaloniaProperty.Register<SideNav, MenuItem?>(
            nameof(MainSelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="BottomSelectedItem"/> property.
    /// </summary>
    public static readonly StyledProperty<MenuItem?> BottomSelectedItemProperty =
        AvaloniaProperty.Register<SideNav, MenuItem?>(
            nameof(BottomSelectedItem),
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="IsCollapsed"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<SideNav, bool>(nameof(IsCollapsed));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the main menu items (top section).
    /// </summary>
    public IEnumerable? MainItems
    {
        get => GetValue(MainItemsProperty);
        set => SetValue(MainItemsProperty, value);
    }

    /// <summary>
    /// Gets or sets the bottom menu items (bottom section).
    /// </summary>
    public IEnumerable? BottomItems
    {
        get => GetValue(BottomItemsProperty);
        set => SetValue(BottomItemsProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected item in main menu.
    /// </summary>
    public MenuItem? MainSelectedItem
    {
        get => GetValue(MainSelectedItemProperty);
        set => SetValue(MainSelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected item in bottom menu.
    /// </summary>
    public MenuItem? BottomSelectedItem
    {
        get => GetValue(BottomSelectedItemProperty);
        set => SetValue(BottomSelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the navigation is collapsed (icon-only mode).
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    #endregion
}

