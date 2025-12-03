using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Modulus.Modules.EchoPlugin.ViewModels;

namespace Modulus.Modules.EchoPlugin.UI.Avalonia;

public partial class EchoView : UserControl
{
    public EchoView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

