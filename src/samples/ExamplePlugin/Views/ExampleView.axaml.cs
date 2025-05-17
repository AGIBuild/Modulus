using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ExamplePlugin.Views
{
    public partial class ExampleView : UserControl
    {
        public ExampleView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
