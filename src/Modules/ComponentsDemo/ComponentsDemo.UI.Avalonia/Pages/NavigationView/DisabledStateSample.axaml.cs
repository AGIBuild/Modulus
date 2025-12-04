using System.Collections.Generic;
using Avalonia.Controls;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia.Pages.NavigationView;

public partial class DisabledStateSample : UserControl
{
    public DisabledStateSample()
    {
        InitializeComponent();
        InitializeDemo();
    }

    private void InitializeDemo()
    {
        var items = new List<UiMenuItem>
        {
            new UiMenuItem("active1", "Active Item", "✅", "active1"),
            new UiMenuItem("disabled1", "Disabled Item", "🚫", "disabled1") { IsEnabled = false },
            new UiMenuItem("active2", "Another Active", "✅", "active2"),
            new UiMenuItem("disabled2", "Also Disabled", "🚫", "disabled2") { IsEnabled = false },
            new UiMenuItem("active3", "Clickable Item", "✅", "active3")
        };

        DisabledNavView.ItemsSource = items;
    }
}

