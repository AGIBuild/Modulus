using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Modulus.App.ViewModels;

namespace Modulus.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = viewModel;
    }
}
