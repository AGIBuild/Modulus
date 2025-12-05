using System.Collections.Generic;
using Avalonia.Controls;
using Modulus.UI.Abstractions;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia.Pages.NavigationView;

public partial class BadgeSample : UserControl
{
    public BadgeSample()
    {
        InitializeComponent();
        InitializeDemo();
    }

    private void InitializeDemo()
    {
        // Use IconKind enum for type-safe icons
        var items = new List<UiMenuItem>
        {
            new UiMenuItem("inbox", "Inbox", IconKind.Mail, "inbox") { BadgeCount = 12 },
            new UiMenuItem("starred", "Starred", IconKind.Star, "starred") { BadgeCount = 3 },
            new UiMenuItem("sent", "Sent", IconKind.Upload, "sent"),
            new UiMenuItem("drafts", "Drafts", IconKind.Document, "drafts") { BadgeCount = 1 },
            new UiMenuItem("spam", "Spam", IconKind.Block, "spam") { BadgeCount = 99 }
        };

        BadgeNavView.ItemsSource = items;
    }
}

