using System.Collections.Generic;
using Avalonia.Controls;
using Modulus.UI.Abstractions;
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
        // Use IconKind enum for type-safe icons
        var items = new List<UiMenuItem>
        {
            new UiMenuItem("dashboard", "Dashboard", IconKind.Dashboard, "dashboard"),
            UiMenuItem.CreateGroup("settings", "Settings", IconKind.Settings, new List<UiMenuItem>
            {
                new UiMenuItem("general", "General", IconKind.Grid, "general"),
                new UiMenuItem("appearance", "Appearance", IconKind.Image, "appearance"),
                UiMenuItem.CreateGroup("advanced", "Advanced", IconKind.Code, new List<UiMenuItem>
                {
                    new UiMenuItem("debug", "Debug", IconKind.Terminal, "debug"),
                    new UiMenuItem("experimental", "Experimental", IconKind.Globe, "experimental")
                })
            }),
            UiMenuItem.CreateGroup("help", "Help", IconKind.Question, new List<UiMenuItem>
            {
                new UiMenuItem("docs", "Documentation", IconKind.Document, "docs"),
                new UiMenuItem("support", "Support", IconKind.Chat, "support"),
                new UiMenuItem("about", "About", IconKind.Info, "about")
            }),
            new UiMenuItem("logout", "Logout", IconKind.ArrowRight, "logout")
        };
        
        items[1].IsExpanded = true;
        SubMenuNavView.ItemsSource = items;
    }
}

