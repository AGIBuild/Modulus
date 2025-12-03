using System.Collections.Generic;
using Avalonia.Controls;
using Modulus.UI.Abstractions;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia.Pages;

public partial class ContextMenuDemoPage : UserControl
{
    public ContextMenuDemoPage()
    {
        InitializeComponent();
        InitializeDemo();
    }

    private void InitializeDemo()
    {
        var contextActions = new List<MenuAction>
        {
            new MenuAction { Label = "Edit", Icon = "✏️", Execute = item => ShowFeedback($"Edit: {item.DisplayName}") },
            new MenuAction { Label = "Rename", Icon = "📝", Execute = item => ShowFeedback($"Rename: {item.DisplayName}") },
            new MenuAction { Label = "Delete", Icon = "🗑️", Execute = item => ShowFeedback($"Delete: {item.DisplayName}") },
            new MenuAction { Label = "Info", Icon = "ℹ️", Execute = item => ShowFeedback($"Info: {item.DisplayName}") }
        };

        var items = new List<UiMenuItem>
        {
            new UiMenuItem("file1", "Document.txt", "📄", "file1") { ContextActions = contextActions },
            new UiMenuItem("file2", "Image.png", "🖼️", "file2") { ContextActions = contextActions },
            new UiMenuItem("file3", "Project.zip", "📦", "file3") { ContextActions = contextActions },
            new UiMenuItem("file4", "Notes.md", "📝", "file4") { ContextActions = contextActions }
        };

        ContextNavView.Items = items;
    }

    private void ShowFeedback(string message)
    {
        ActionFeedback.Text = $"Action executed: {message}";
    }
}

