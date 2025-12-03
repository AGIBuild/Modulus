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
        var stack = new StackPanel { Spacing = 16 };
        stack.Children.Add(new TextBlock
        {
            Text = "Badges can display notification counts on menu items.",
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
        });

        var examples = new[]
        {
            ("Error Badge", 5, "#E53935"),
            ("Warning Badge", 12, "#FFA726"),
            ("Info Badge", 3, "#29B6F6"),
            ("Success Badge", 1, "#66BB6A")
        };

        foreach (var (name, count, color) in examples)
        {
            var row = new Border
            {
                Background = global::Avalonia.Media.Brush.Parse("#20FFFFFF"),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 4)
            };
            var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*, Auto") };
            grid.Children.Add(new TextBlock { Text = name, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center });
            var badge = new Border
            {
                Background = global::Avalonia.Media.Brush.Parse(color),
                CornerRadius = new CornerRadius(10),
                MinWidth = 24,
                Padding = new Thickness(6, 2),
                [Grid.ColumnProperty] = 1
            };
            badge.Child = new TextBlock
            {
                Text = count.ToString(),
                Foreground = global::Avalonia.Media.Brushes.White,
                FontSize = 12,
                FontWeight = global::Avalonia.Media.FontWeight.Bold,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center
            };
            grid.Children.Add(badge);
            row.Child = grid;
            stack.Children.Add(row);
        }

        return stack;
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
        return new StackPanel
        {
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Context menus provide additional actions for menu items.",
                    TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
                },
                CreateInfoCard(new[]
                {
                    "• Define ContextActions on MenuItem",
                    "• Right-click to show the menu",
                    "• Each action has Label, Icon, and Execute callback"
                }),
                CreateTipBox("Right-click on 'Context Menu Demo' in the main navigation to see the context menu.")
            }
        };
    }

    private Control CreateKeyboardDemo()
    {
        var stack = new StackPanel { Spacing = 16 };
        stack.Children.Add(new TextBlock
        {
            Text = "Full keyboard navigation support for accessibility and power users.",
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
        });

        var shortcuts = new[]
        {
            ("↑ / ↓", "Move selection up/down"),
            ("Enter / Space", "Activate selected item"),
            ("→", "Expand group"),
            ("←", "Collapse group"),
            ("Escape", "Collapse all groups"),
            ("Tab", "Standard focus navigation")
        };

        var grid = new Grid
        {
            ColumnDefinitions = ColumnDefinitions.Parse("Auto, *"),
            RowDefinitions = RowDefinitions.Parse(string.Join(",", Enumerable.Repeat("Auto", shortcuts.Length)))
        };

        for (int i = 0; i < shortcuts.Length; i++)
        {
            var (key, action) = shortcuts[i];
            var keyText = new TextBlock { Text = key, FontWeight = global::Avalonia.Media.FontWeight.Bold, Margin = new Thickness(0, 0, 16, 8) };
            var actionText = new TextBlock { Text = action, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(keyText, i);
            Grid.SetColumn(keyText, 0);
            Grid.SetRow(actionText, i);
            Grid.SetColumn(actionText, 1);
            grid.Children.Add(keyText);
            grid.Children.Add(actionText);
        }

        var card = new Border
        {
            Background = global::Avalonia.Media.Brush.Parse("#20FFFFFF"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Child = grid
        };
        stack.Children.Add(card);
        stack.Children.Add(CreateTipBox("Focus the navigation panel and try the keyboard shortcuts!"));

        return stack;
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

