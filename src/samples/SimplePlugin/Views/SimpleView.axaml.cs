using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SimplePlugin.Views
{
    public partial class SimpleView : UserControl
    {
        public SimpleView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
