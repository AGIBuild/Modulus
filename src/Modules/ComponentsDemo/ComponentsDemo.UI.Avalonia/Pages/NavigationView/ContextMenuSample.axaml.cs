using System.Collections.Generic;
using Avalonia.Controls;
using Modulus.UI.Abstractions;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia.Pages.NavigationView;

public partial class ContextMenuSample : UserControl
{
    public ContextMenuSample()
    {
        InitializeComponent();
        InitializeDemo();
    }

    private void InitializeDemo()
    {
        // Use IconKind enum for context menu actions
        var contextActions = new List<MenuAction>
        {
            new MenuAction { Label = "Edit", Icon = IconKind.Edit, Execute = item => ShowFeedback($"Edit: {item.DisplayName}") },
            new MenuAction { Label = "Rename", Icon = IconKind.Document, Execute = item => ShowFeedback($"Rename: {item.DisplayName}") },
            new MenuAction { Label = "Delete", Icon = IconKind.Delete, Execute = item => ShowFeedback($"Delete: {item.DisplayName}") },
            new MenuAction { Label = "Info", Icon = IconKind.Info, Execute = item => ShowFeedback($"Info: {item.DisplayName}") }
        };

        // Use IconKind enum for type-safe icons
        var items = new List<UiMenuItem>
        {
            new UiMenuItem("file1", "Document.txt", IconKind.File, "file1") { ContextActions = contextActions },
            new UiMenuItem("file2", "Image.png", IconKind.Image, "file2") { ContextActions = contextActions },
            new UiMenuItem("file3", "Project.zip", IconKind.Archive, "file3") { ContextActions = contextActions },
            new UiMenuItem("file4", "Notes.md", IconKind.Document, "file4") { ContextActions = contextActions }
        };

        ContextNavView.ItemsSource = items;
    }

    private void ShowFeedback(string message)
    {
        ActionFeedback.Text = $"Action executed: {message}";
    }
}

