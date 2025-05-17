using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Modulus.App.Views
{
    public partial class PluginManagerView : UserControl
    {
        public PluginManagerView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
