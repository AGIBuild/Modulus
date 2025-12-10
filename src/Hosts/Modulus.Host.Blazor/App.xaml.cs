using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core.Data;
using Modulus.Core.Runtime;
using Modulus.Host.Blazor.Shell.Services;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Blazor;

public partial class App : Application
{
    private readonly IModulusApplication _modulusApp;
    private readonly ILogger<App> _logger;
    private bool _initialized;

    public App(IModulusApplication modulusApp, ILogger<App> logger)
    {
        _modulusApp = modulusApp;
        _logger = logger;
        
        InitializeComponent();
        
        // Setup global exception handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        if (e.IsTerminating)
        {
            _logger.LogCritical(ex, "Fatal unhandled exception: {Message}", ex?.Message);
        }
        else
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex?.Message);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        _logger.LogError(e.Exception, "Unobserved task exception: {Message}", e.Exception.Message);
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
        
        var window = new Window(new MainPage()) { Title = "Modulus" };
        
        // Ensure process exits when window is destroyed
        window.Destroying += OnWindowDestroying;
        
        return window;
    }

    private void OnWindowDestroying(object? sender, EventArgs e)
    {
        // Shutdown modules and force exit
        _modulusApp.ShutdownAsync().GetAwaiter().GetResult();
        Environment.Exit(0);
    }

    protected override void CleanUp()
    {
        _modulusApp.ShutdownAsync().GetAwaiter().GetResult();
        base.CleanUp();
    }
}
