using Microsoft.UI.Xaml;

namespace $ext_safeprojectname$.Host.Blazor.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => $ext_safeprojectname$.Host.Blazor.MauiProgram.CreateMauiApp();
}


