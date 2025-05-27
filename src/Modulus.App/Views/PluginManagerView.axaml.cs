using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Modulus.App.ViewModels;

namespace Modulus.App.Views
{
    public partial class PluginManagerView : UserControl
    {
        public PluginManagerView()
        {
            InitializeComponent();
        }

        public PluginManagerView(PluginManagerViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
