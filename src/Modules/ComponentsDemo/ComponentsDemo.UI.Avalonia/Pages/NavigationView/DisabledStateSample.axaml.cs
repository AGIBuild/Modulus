using System.Collections.Generic;
using Avalonia.Controls;
using Modulus.UI.Abstractions;
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
        // Use IconKind enum for type-safe icons
        var items = new List<UiMenuItem>
        {
            new UiMenuItem("active1", "Active Item", IconKind.Check, "active1"),
            new UiMenuItem("disabled1", "Disabled Item", IconKind.Block, "disabled1") { IsEnabled = false },
            new UiMenuItem("active2", "Another Active", IconKind.Check, "active2"),
            new UiMenuItem("disabled2", "Also Disabled", IconKind.Block, "disabled2") { IsEnabled = false },
            new UiMenuItem("active3", "Clickable Item", IconKind.Check, "active3")
        };

        DisabledNavView.ItemsSource = items;
    }
}

