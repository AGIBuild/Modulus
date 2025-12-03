using System.Collections.Generic;
using Avalonia.Controls;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia.Pages;

public partial class NavigationViewPage : UserControl
{
    public NavigationViewPage()
    {
        InitializeComponent();
        InitializeDemo();
    }

    private void InitializeDemo()
    {
        // Create sample menu items
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
        
        // Expand Settings group by default
        items[2].IsExpanded = true;

        // Set items on the NavigationView
        DemoNavView.Items = items;
        
        // Handle selection
        DemoNavView.SelectionChanged += (s, item) =>
        {
            // In a real app, this would trigger navigation
            System.Diagnostics.Debug.WriteLine($"Selected: {item.DisplayName}");
        };
    }
}

