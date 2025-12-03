using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Modulus.UI.Avalonia.Components;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia;

public partial class DemoContentControl : UserControl
{
    public static readonly StyledProperty<string> DemoIdProperty =
        AvaloniaProperty.Register<DemoContentControl, string>(nameof(DemoId), "basic-nav");

    public string DemoId
    {
        get => GetValue(DemoIdProperty);
        set => SetValue(DemoIdProperty, value);
    }

    private ContentControl? _contentHost;

    public DemoContentControl()
    {
        InitializeComponent();
        _contentHost = this.FindControl<ContentControl>("ContentHost");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DemoIdProperty)
        {
            UpdateContent();
        }
    }

    protected override void OnAttachedToVisualTree(global::Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateContent();
    }

    private void UpdateContent()
    {
        if (_contentHost == null) return;

        _contentHost.Content = DemoId switch
        {
            "basic-nav" => CreateNavigationDemo(),
            "badge-nav" => CreateBadgeDemo(),
            "disabled-nav" => CreateDisabledDemo(),
            "sub-item-1" or "sub-item-2" or "sub-item-3" => CreateSubItemDemo(DemoId),
            "context-demo" => CreateContextMenuDemo(),
            "keyboard-nav" => CreateKeyboardDemo(),
            "lifecycle-demo" => CreateLifecycleDemo(),
            _ => CreateNavigationDemo()
        };
    }

    private Control CreateNavigationDemo()
    {
        // Create sample menu items for demonstration
        var sampleItems = new List<UiMenuItem>
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
        sampleItems[2].IsExpanded = true; // Expand Settings group

        // Create NavigationView instance
        var navView = new NavigationView
        {
            Items = sampleItems,
            Width = 200,
            Height = 250,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        navView.ItemSelected += (s, item) =>
        {
            // Show selected item (in real app, this would navigate)
        };

        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "NavigationView Component Demo",
                    FontWeight = global::Avalonia.Media.FontWeight.Bold,
                    FontSize = 16
                },
                new TextBlock
                {
                    Text = "This is a live NavigationView component. Click items to select, click groups to expand/collapse:",
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
                },
                new Border
                {
                    Background = global::Avalonia.Media.Brush.Parse("#15000000"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8),
                    Child = navView
                },
                CreateInfoCard(new[]
                {
                    "Features demonstrated:",
                    "• Hierarchical menu with expandable groups",
                    "• Badge indicators on menu items",
                    "• Disabled item state",
                    "• Click to select / expand"
                }),
                CreateTipBox("Toggle the main app navigation using the ☰ button in the title bar to see collapse mode.")
            }
        };
    }

    private Control CreateBadgeDemo()
    {
        // Create menu items with various badge counts
        var badgeItems = new List<UiMenuItem>
        {
            new UiMenuItem("inbox", "Inbox", "📥", "inbox") { BadgeCount = 5, BadgeColor = "error" },
            new UiMenuItem("updates", "Updates", "🔔", "updates") { BadgeCount = 12, BadgeColor = "warning" },
            new UiMenuItem("messages", "Messages", "💬", "messages") { BadgeCount = 3, BadgeColor = "info" },
            new UiMenuItem("completed", "Completed", "✅", "completed") { BadgeCount = 99, BadgeColor = "success" },
            new UiMenuItem("empty", "No Badge", "📭", "empty"), // No badge
        };

        var navView = new NavigationView
        {
            Items = badgeItems,
            Width = 200,
            Height = 200,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Badge Indicators Demo",
                    FontWeight = global::Avalonia.Media.FontWeight.Bold,
                    FontSize = 16
                },
                new TextBlock
                {
                    Text = "Badges display notification counts on menu items. Set BadgeCount and BadgeColor properties:",
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
                },
                new Border
                {
                    Background = global::Avalonia.Media.Brush.Parse("#15000000"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8),
                    Child = navView
                },
                CreateInfoCard(new[]
                {
                    "• BadgeCount: Number to display (null hides badge)",
                    "• BadgeColor: 'error', 'warning', 'info', 'success'",
                    "• Badge auto-hides when count is null or 0"
                })
            }
        };
    }

    private Control CreateDisabledDemo()
    {
        // Create menu items with disabled states
        var disabledItems = new List<UiMenuItem>
        {
            new UiMenuItem("enabled1", "Enabled Item", "✅", "enabled1"),
            new UiMenuItem("disabled1", "Disabled Item", "🚫", "disabled1") { IsEnabled = false },
            new UiMenuItem("enabled2", "Another Enabled", "✅", "enabled2"),
            new UiMenuItem("disabled2", "Also Disabled", "🚫", "disabled2") { IsEnabled = false },
        };

        var navView = new NavigationView
        {
            Items = disabledItems,
            Width = 200,
            Height = 180,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Disabled States Demo",
                    FontWeight = global::Avalonia.Media.FontWeight.Bold,
                    FontSize = 16
                },
                new TextBlock
                {
                    Text = "Disabled items are visible but cannot be clicked. Try clicking the disabled items below:",
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
                },
                new Border
                {
                    Background = global::Avalonia.Media.Brush.Parse("#15000000"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8),
                    Child = navView
                },
                CreateInfoCard(new[]
                {
                    "• IsEnabled = false makes item grayed out (opacity 0.4)",
                    "• Disabled items don't respond to clicks",
                    "• Visual feedback shows item is not interactive"
                })
            }
        };
    }

    private Control CreateSubItemDemo(string itemId)
    {
        // Create hierarchical menu items
        var hierarchyItems = new List<UiMenuItem>
        {
            UiMenuItem.CreateGroup("folder1", "📁 Folder 1", "📁", new List<UiMenuItem>
            {
                new UiMenuItem("file1", "File 1.txt", "📄", "file1"),
                new UiMenuItem("file2", "File 2.txt", "📄", "file2"),
            }),
            UiMenuItem.CreateGroup("folder2", "📁 Folder 2", "📁", new List<UiMenuItem>
            {
                new UiMenuItem("file3", "File 3.txt", "📄", "file3"),
                UiMenuItem.CreateGroup("subfolder", "📁 Subfolder", "📁", new List<UiMenuItem>
                {
                    new UiMenuItem("file4", "Nested File", "📄", "file4"),
                }),
            }),
        };
        hierarchyItems[0].IsExpanded = true;
        hierarchyItems[1].IsExpanded = true;

        var navView = new NavigationView
        {
            Items = hierarchyItems,
            Width = 220,
            Height = 220,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Sub Menu / Hierarchical Demo",
                    FontWeight = global::Avalonia.Media.FontWeight.Bold,
                    FontSize = 16
                },
                new TextBlock
                {
                    Text = $"Currently viewing: {itemId}\n\nClick folder headers to expand/collapse. The NavigationView supports nested hierarchies:",
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
                },
                new Border
                {
                    Background = global::Avalonia.Media.Brush.Parse("#15000000"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8),
                    Child = navView
                },
                CreateInfoCard(new[]
                {
                    "• Use MenuItem.CreateGroup() to create expandable groups",
                    "• Set Children property for child items",
                    "• IsExpanded controls initial expand state",
                    "• Click group header to toggle expand/collapse"
                })
            }
        };
    }

    private Control CreateContextMenuDemo()
    {
        var _selectedAction = new TextBlock { Text = "Right-click an item to see context menu", FontStyle = global::Avalonia.Media.FontStyle.Italic };

        // Create menu items with context actions
        var contextItems = new List<UiMenuItem>
        {
            new UiMenuItem("doc1", "Document 1", "📄", "doc1")
            {
                ContextActions = new List<Modulus.UI.Abstractions.MenuAction>
                {
                    new() { Label = "Open", Icon = "📂", Execute = _ => _selectedAction.Text = "Opened Document 1" },
                    new() { Label = "Edit", Icon = "✏️", Execute = _ => _selectedAction.Text = "Editing Document 1" },
                    new() { Label = "Delete", Icon = "🗑️", Execute = _ => _selectedAction.Text = "Deleted Document 1" },
                }
            },
            new UiMenuItem("doc2", "Document 2", "📄", "doc2")
            {
                ContextActions = new List<Modulus.UI.Abstractions.MenuAction>
                {
                    new() { Label = "Open", Icon = "📂", Execute = _ => _selectedAction.Text = "Opened Document 2" },
                    new() { Label = "Rename", Icon = "✍️", Execute = _ => _selectedAction.Text = "Renaming Document 2" },
                }
            },
            new UiMenuItem("nocontext", "No Context Menu", "📝", "nocontext"),
        };

        var navView = new NavigationView
        {
            Items = contextItems,
            Width = 200,
            Height = 140,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Context Menu Demo",
                    FontWeight = global::Avalonia.Media.FontWeight.Bold,
                    FontSize = 16
                },
                new TextBlock
                {
                    Text = "Right-click on menu items to see context menus with custom actions:",
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
                },
                new Border
                {
                    Background = global::Avalonia.Media.Brush.Parse("#15000000"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8),
                    Child = navView
                },
                new Border
                {
                    Background = global::Avalonia.Media.Brush.Parse("#20FFFFFF"),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12),
                    Child = _selectedAction
                },
                CreateInfoCard(new[]
                {
                    "• Set ContextActions property on MenuItem",
                    "• Each MenuAction has Label, Icon, Execute",
                    "• Execute receives the MenuItem as parameter"
                })
            }
        };
    }

    private Control CreateKeyboardDemo()
    {
        // Create items for keyboard navigation testing
        var keyboardItems = new List<UiMenuItem>
        {
            new UiMenuItem("item1", "Item 1 (↑↓ to move)", "1️⃣", "item1"),
            new UiMenuItem("item2", "Item 2", "2️⃣", "item2"),
            UiMenuItem.CreateGroup("group", "Group (→ expand, ← collapse)", "📁", new List<UiMenuItem>
            {
                new UiMenuItem("child1", "Child 1", "📄", "child1"),
                new UiMenuItem("child2", "Child 2", "📄", "child2"),
            }),
            new UiMenuItem("item3", "Item 3 (Enter to select)", "3️⃣", "item3"),
        };

        var navView = new NavigationView
        {
            Items = keyboardItems,
            Width = 240,
            Height = 180,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var shortcuts = new[]
        {
            ("↑ / ↓", "Move selection up/down"),
            ("Enter / Space", "Activate selected item"),
            ("→", "Expand group"),
            ("←", "Collapse group"),
            ("Escape", "Collapse all groups")
        };

        var grid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("Auto, *"),
            RowDefinitions = RowDefinitions.Parse(string.Join(",", Enumerable.Repeat("Auto", shortcuts.Length)))
        };

        for (int i = 0; i < shortcuts.Length; i++)
        {
            var (key, action) = shortcuts[i];
            var keyText = new TextBlock { Text = key, FontWeight = global::Avalonia.Media.FontWeight.Bold, Margin = new Thickness(0, 0, 16, 6) };
            var actionText = new TextBlock { Text = action, Margin = new Thickness(0, 0, 0, 6) };
            Grid.SetRow(keyText, i);
            Grid.SetColumn(keyText, 0);
            Grid.SetRow(actionText, i);
            Grid.SetColumn(actionText, 1);
            grid.Children.Add(keyText);
            grid.Children.Add(actionText);
        }

        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Keyboard Navigation Demo",
                    FontWeight = global::Avalonia.Media.FontWeight.Bold,
                    FontSize = 16
                },
                new TextBlock
                {
                    Text = "Click the navigation below to focus it, then use keyboard shortcuts:",
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
                },
                new Border
                {
                    Background = global::Avalonia.Media.Brush.Parse("#15000000"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8),
                    Child = navView
                },
                new Border
                {
                    Background = global::Avalonia.Media.Brush.Parse("#20FFFFFF"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Child = grid
                }
            }
        };
    }

    private Control CreateLifecycleDemo()
    {
        var instanceId = new Random().Next(1000, 9999);
        var createdAt = DateTime.Now.ToString("HH:mm:ss.fff");

        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Page instance lifecycle control determines how view instances are managed.",
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
                },
                CreateInfoCard(new[]
                {
                    "• Default: Use host default (typically Singleton)",
                    "• Singleton: Same instance reused across navigations",
                    "• Transient: New instance created each navigation"
                }),
                new Border
                {
                    Background = global::Avalonia.Media.Brush.Parse("#20FFFFFF"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(16),
                    Child = new StackPanel
                    {
                        Spacing = 8,
                        Children =
                        {
                            new TextBlock { Text = "Instance Information:", FontWeight = global::Avalonia.Media.FontWeight.SemiBold },
                            new StackPanel
                            {
                                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                                Children =
                                {
                                    new TextBlock { Text = "Instance ID: " },
                                    new TextBlock { Text = instanceId.ToString(), FontWeight = global::Avalonia.Media.FontWeight.Bold }
                                }
                            },
                            new StackPanel
                            {
                                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                                Children =
                                {
                                    new TextBlock { Text = "Created At: " },
                                    new TextBlock { Text = createdAt, FontWeight = global::Avalonia.Media.FontWeight.Bold }
                                }
                            }
                        }
                    }
                },
                CreateTipBox("Navigate away and back to see a new instance created!")
            }
        };
    }

    private Border CreateInfoCard(string[] lines)
    {
        var stack = new StackPanel { Spacing = 4 };
        foreach (var line in lines)
        {
            stack.Children.Add(new TextBlock { Text = line, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        }
        return new Border
        {
            Background = global::Avalonia.Media.Brush.Parse("#20FFFFFF"), // Semi-transparent white
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Child = stack
        };
    }

    private Border CreateTipBox(string tip)
    {
        return new Border
        {
            Background = global::Avalonia.Media.Brush.Parse("#4029B6F6"), // Semi-transparent blue
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Child = new TextBlock
            {
                Text = "💡 " + tip,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
            }
        };
    }
}


