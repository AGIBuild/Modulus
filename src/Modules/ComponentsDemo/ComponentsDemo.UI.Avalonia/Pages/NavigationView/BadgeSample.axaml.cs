using System.Collections.Generic;
using Avalonia.Controls;
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
        var items = new List<UiMenuItem>
        {
            new UiMenuItem("inbox", "Inbox", "📥", "inbox") { BadgeCount = 12 },
            new UiMenuItem("starred", "Starred", "⭐", "starred") { BadgeCount = 3 },
            new UiMenuItem("sent", "Sent", "📤", "sent"),
            new UiMenuItem("drafts", "Drafts", "📝", "drafts") { BadgeCount = 1 },
            new UiMenuItem("spam", "Spam", "🚫", "spam") { BadgeCount = 99 }
        };

        BadgeNavView.Items = items;
    }
}

