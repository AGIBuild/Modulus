using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Modulus.App.ViewModels;

namespace Modulus.App.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
