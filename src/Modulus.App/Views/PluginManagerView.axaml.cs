using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Modulus.App.ViewModels;

namespace Modulus.App.Views
{
    public partial class PluginManagerView : UserControl
    {

        public PluginManagerView(PluginManagerViewModel  viewModel)
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = viewModel;
        }
    }
}
