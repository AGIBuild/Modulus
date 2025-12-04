using System.Collections.Generic;
using Avalonia.Controls;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia.Pages.NavigationView;

public partial class SubMenuSample : UserControl
{
    public SubMenuSample()
    {
        InitializeComponent();
        InitializeDemo();
    }

    private void InitializeDemo()
    {
        var items = new List<UiMenuItem>
        {
            new UiMenuItem("dashboard", "Dashboard", "📊", "dashboard"),
            UiMenuItem.CreateGroup("settings", "Settings", "⚙️", new List<UiMenuItem>
            {
                new UiMenuItem("general", "General", "🔧", "general"),
                new UiMenuItem("appearance", "Appearance", "🎨", "appearance"),
                UiMenuItem.CreateGroup("advanced", "Advanced", "🔬", new List<UiMenuItem>
                {
                    new UiMenuItem("debug", "Debug", "🐛", "debug"),
                    new UiMenuItem("experimental", "Experimental", "🧪", "experimental")
                })
            }),
            UiMenuItem.CreateGroup("help", "Help", "❓", new List<UiMenuItem>
            {
                new UiMenuItem("docs", "Documentation", "📚", "docs"),
                new UiMenuItem("support", "Support", "💬", "support"),
                new UiMenuItem("about", "About", "ℹ️", "about")
            }),
            new UiMenuItem("logout", "Logout", "🚪", "logout")
        };
        
        items[1].IsExpanded = true;
        SubMenuNavView.ItemsSource = items;
    }
}

