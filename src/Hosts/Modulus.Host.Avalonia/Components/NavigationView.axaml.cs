using System.Collections;
using Avalonia;
using Avalonia.Controls;

namespace Modulus.Host.Avalonia.Components;

public partial class NavigationView : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<NavigationView, IEnumerable?>(nameof(Items));

    public static readonly StyledProperty<Modulus.UI.Abstractions.MenuItem?> SelectedItemProperty =
        AvaloniaProperty.Register<NavigationView, Modulus.UI.Abstractions.MenuItem?>(nameof(SelectedItem), defaultBindingMode: global::Avalonia.Data.BindingMode.TwoWay);

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

    public NavigationView()
    {
        InitializeComponent();
    }
}

