using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NavigationExamplePlugin.Views
{
    public partial class NavigationExampleView : UserControl
    {
        public NavigationExampleView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
