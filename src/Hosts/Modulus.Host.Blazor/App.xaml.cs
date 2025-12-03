using Microsoft.Extensions.DependencyInjection;
using Modulus.Core.Data;
using Modulus.Core.Runtime;
using Modulus.Host.Blazor.Shell.Services;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Blazor;

public partial class App : Application
{
    private readonly IModulusApplication _modulusApp;
    private bool _initialized;

    public App(IModulusApplication modulusApp)
    {
        _modulusApp = modulusApp;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Initialize modules here - ServiceProvider is available at this point
        if (!_initialized)
        {
            _initialized = true;
            
            // Get the service provider from MAUI and set it
            if (activationState?.Context?.Services != null)
            {
                var services = activationState.Context.Services;
                _modulusApp.SetServiceProvider(services);
                
                // Initialize Database
                var database = services.GetRequiredService<IAppDatabase>();
                database.InitializeAsync().GetAwaiter().GetResult();
                
                // Initialize Theme Service (load saved theme)
                var themeService = services.GetRequiredService<IThemeService>() as BlazorThemeService;
                themeService?.InitializeAsync().GetAwaiter().GetResult();
                
                // Initialize Modules
                _modulusApp.InitializeAsync().GetAwaiter().GetResult();
            }
        }
        
        return new Window(new MainPage()) { Title = "Modulus" };
    }

    protected override void CleanUp()
    {
        _modulusApp.ShutdownAsync().GetAwaiter().GetResult();
        base.CleanUp();
    }
}
