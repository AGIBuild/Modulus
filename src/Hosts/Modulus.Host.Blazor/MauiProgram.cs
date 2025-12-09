using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Modulus.Core;
using Modulus.Core.Data;
using Modulus.Core.Installation;
using Modulus.Core.Logging;
using Modulus.Core.Runtime;
using Modulus.Host.Blazor.Services;
using Modulus.Host.Blazor.Shell.Services;
using Modulus.Host.Blazor.Shell.ViewModels;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

#if ANDROID
using Android.Runtime;
#endif

namespace Modulus.Host.Blazor;

[DependsOn()]
public class BlazorHostModule : ModulusComponent
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        // MAUI Blazor Services
        context.Services.AddMauiBlazorWebView();
        context.Services.AddMudServices();

#if DEBUG
        context.Services.AddBlazorWebViewDeveloperTools();
#endif

        // UI Services
        context.Services.AddSingleton<IUIFactory, BlazorUIFactory>();
        context.Services.AddScoped<IViewHost, BlazorViewHost>();
        context.Services.AddSingleton<IThemeService, BlazorThemeService>();

        // Shell Services
        context.Services.AddSingleton<IMenuRegistry, MenuRegistry>();
        context.Services.AddScoped<BlazorNavigationService>();
        context.Services.AddScoped<INavigationService>(sp => sp.GetRequiredService<BlazorNavigationService>());

        // Shell ViewModels
        context.Services.AddSingleton<ShellViewModel>();
        context.Services.AddTransient<ModuleListViewModel>();
        context.Services.AddTransient<SettingsViewModel>();
    }

    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Menus come from database (full database-driven approach)
        // View mappings for Blazor are handled via Razor page routing
        return Task.CompletedTask;
    }
}

public static class MauiProgram
{
    private static ILogger _logger = null!;

    public static void Main(string[] args) {} // Dummy entry point for net10.0 target without MAUI

    public static MauiApp CreateMauiApp()
    {
        // Configuration (environment-aware via DOTNET_ENVIRONMENT)
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        // Initialize logger first
        var loggerFactory = ModulusLogging.CreateLoggerFactory(configuration, HostType.Blazor);
        _logger = loggerFactory.CreateLogger<MauiApp>();

        // Setup global exception handlers (logger is now ready)
        SetupGlobalExceptionHandlers();
        _logger.LogInformation("Global exception handlers initialized.");

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        ModulusLogging.AddLoggerFactory(builder.Services, loggerFactory);

        // Module Providers - load from Modules/ directory relative to executable
        var providers = new List<IModuleProvider>();

        // App Modules: {AppBaseDir}/Modules/ (populated by nuke build)
        var appModules = Path.Combine(AppContext.BaseDirectory, "Modules");
        if (Directory.Exists(appModules))
        {
            providers.Add(new DirectoryModuleProvider(appModules, loggerFactory.CreateLogger<DirectoryModuleProvider>(), isSystem: true));
        }

        // User-installed modules (for runtime installation)
        var userModules = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Modulus", "Modules");
        if (Directory.Exists(userModules))
        {
            providers.Add(new DirectoryModuleProvider(userModules, loggerFactory.CreateLogger<DirectoryModuleProvider>(), isSystem: false));
        }

        // Database (configurable name; defaults to framework/solution name)
        var dbName = configuration["Modulus:DatabaseName"] ?? "Modulus";
        var dbPath = DatabaseServiceExtensions.GetDefaultDatabasePath(dbName);
        builder.Services.AddModulusDatabase(dbPath);

        // Repositories & installers
        builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
        builder.Services.AddScoped<IMenuRepository, MenuRepository>();
        builder.Services.AddScoped<HostModuleSeeder>();

        // Create Modulus App (use same DB path to align migrations and runtime)
        var appTask = ModulusApplicationFactory.CreateAsync<BlazorHostModule>(builder.Services, providers, HostType.Blazor, dbPath, configuration, loggerFactory);
        var modulusApp = appTask.GetAwaiter().GetResult();

        // Register the app as IModulusApplication so it can be injected
        builder.Services.AddSingleton<IModulusApplication>(modulusApp);

        // Build the app first, then seed Host module
        var app = builder.Build();
        
        // Seed Host module and menus to database (full database-driven approach)
        using (var scope = app.Services.CreateScope())
        {
            var hostSeeder = scope.ServiceProvider.GetRequiredService<HostModuleSeeder>();
            hostSeeder.SeedAsync(
                HostType.Blazor,
                "/modules",  // Blazor uses route-based navigation
                "/settings"
            ).GetAwaiter().GetResult();
        }

        return app;
    }

    private static void SetupGlobalExceptionHandlers()
    {
        // Handle all unhandled exceptions in the AppDomain
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Handle unobserved task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

#if ANDROID
        // Android-specific unhandled exception handler
        AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandledException;
#endif
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;

        if (e.IsTerminating)
        {
            _logger.LogCritical(exception, "Fatal unhandled exception - application will terminate: {Message}",
                exception?.Message ?? "Unknown error");
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception caught: {Message}",
                exception?.Message ?? "Unknown error");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Mark as observed to prevent process termination
        e.SetObserved();

        _logger.LogError(e.Exception, "Unobserved task exception: {Message}", e.Exception.Message);
    }

#if ANDROID
    private static void OnAndroidUnhandledException(object? sender, RaiseThrowableEventArgs e)
    {
        _logger.LogError(e.Exception, "Android unhandled exception: {Message}", e.Exception.Message);
        
        // Set handled to prevent crash where possible
        e.Handled = true;
    }
#endif
}
