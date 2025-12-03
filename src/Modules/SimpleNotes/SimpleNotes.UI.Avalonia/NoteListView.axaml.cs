using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SimpleNotes.UI.Avalonia;

public partial class NoteListView : UserControl
{
    public NoteListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

