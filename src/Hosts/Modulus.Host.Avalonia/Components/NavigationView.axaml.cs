using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace Modulus.Host.Avalonia.Components;

public partial class NavigationView : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<NavigationView, IEnumerable?>(nameof(Items));

    public static readonly StyledProperty<Modulus.UI.Abstractions.MenuItem?> SelectedItemProperty =
        AvaloniaProperty.Register<NavigationView, Modulus.UI.Abstractions.MenuItem?>(
            nameof(SelectedItem), 
            defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<NavigationView, bool>(nameof(IsCollapsed));

    /// <summary>
    /// Converter for IsEnabled to Opacity (1.0 for enabled, 0.4 for disabled).
    /// </summary>
    public static readonly IValueConverter EnabledToOpacityConverter =
        new FuncValueConverter<bool, double>(isEnabled => isEnabled ? 1.0 : 0.4);

    public IEnumerable? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public Modulus.UI.Abstractions.MenuItem? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Whether the navigation is in collapsed (icon-only) mode.
    /// </summary>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    public NavigationView()
    {
        InitializeComponent();
    }
}
