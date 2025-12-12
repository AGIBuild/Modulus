using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Modulus.Modules.$ext_safeprojectname$.UI.Avalonia;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

