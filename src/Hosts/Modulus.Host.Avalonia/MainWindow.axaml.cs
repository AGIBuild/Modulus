using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Modulus.Host.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ToggleTheme_Click(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is App app)
        {
            app.ToggleTheme();
        }
    }
}
