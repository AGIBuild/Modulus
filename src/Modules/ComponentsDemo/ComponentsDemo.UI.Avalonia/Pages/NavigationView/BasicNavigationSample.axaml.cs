using System.Collections.Generic;
using Avalonia.Controls;
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
        var items = new List<UiMenuItem>
        {
            new UiMenuItem("home", "Home", "🏠", "home") { BadgeCount = 3 },
            new UiMenuItem("docs", "Documents", "📄", "docs"),
            UiMenuItem.CreateGroup("settings", "Settings", "⚙️", new List<UiMenuItem>
            {
                new UiMenuItem("profile", "Profile", "👤", "profile"),
                new UiMenuItem("security", "Security", "🔒", "security"),
                new UiMenuItem("disabled", "Disabled Item", "🚫", "disabled") { IsEnabled = false }
            }),
            new UiMenuItem("help", "Help", "❓", "help")
        };
        
        items[2].IsExpanded = true;
        DemoNavView.ItemsSource = items;
        
        DemoNavView.SelectionChanged += (s, item) =>
        {
            System.Diagnostics.Debug.WriteLine($"Selected: {item.DisplayName}");
        };
    }
}

