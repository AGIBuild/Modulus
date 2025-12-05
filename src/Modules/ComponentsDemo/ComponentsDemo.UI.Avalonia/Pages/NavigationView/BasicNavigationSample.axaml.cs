using System.Collections.Generic;
using Avalonia.Controls;
using Modulus.UI.Abstractions;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia.Pages.NavigationView;

public partial class BasicNavigationSample : UserControl
{
    public BasicNavigationSample()
    {
        InitializeComponent();
        InitializeDemo();
    }

    private void InitializeDemo()
    {
        // Use IconKind enum for type-safe icons
        var items = new List<UiMenuItem>
        {
            new UiMenuItem("home", "Home", IconKind.Home, "home") { BadgeCount = 3 },
            new UiMenuItem("docs", "Documents", IconKind.Document, "docs"),
            UiMenuItem.CreateGroup("settings", "Settings", IconKind.Settings, new List<UiMenuItem>
            {
                new UiMenuItem("profile", "Profile", IconKind.Person, "profile"),
                new UiMenuItem("security", "Security", IconKind.Lock, "security"),
                new UiMenuItem("disabled", "Disabled Item", IconKind.Block, "disabled") { IsEnabled = false }
            }),
            new UiMenuItem("help", "Help", IconKind.Question, "help")
        };
        
        items[2].IsExpanded = true;
        DemoNavView.ItemsSource = items;
        
        DemoNavView.SelectionChanged += (s, item) =>
        {
            System.Diagnostics.Debug.WriteLine($"Selected: {item.DisplayName}");
        };
    }
}

