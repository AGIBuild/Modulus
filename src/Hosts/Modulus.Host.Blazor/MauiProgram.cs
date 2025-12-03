using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor.Services;
using Modulus.Core;
using Modulus.Core.Data;
using Modulus.Core.Runtime;
using Modulus.Host.Blazor.Services;
using Modulus.Host.Blazor.Shell.Services;
using Modulus.Host.Blazor.Shell.ViewModels;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Host.Blazor;

[DependsOn()]
public class BlazorHostModule : ModuleBase
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
        // Register built-in menu items
        var menuRegistry = context.ServiceProvider.GetRequiredService<IMenuRegistry>();
        menuRegistry.Register(new UiMenuItem("Modules", "Modules", "extension", "/modules", MenuLocation.Main, 10));
        menuRegistry.Register(new UiMenuItem("Settings", "Settings", "settings", "/settings", MenuLocation.Bottom, 100));
        
        return Task.CompletedTask;
    }
}

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Module Providers
        var providers = new List<IModuleProvider>();

#if DEBUG
        // Development: Scan solution for modules (filter by host type)
        var solutionRoot = FindSolutionRoot(AppContext.BaseDirectory);
        if (solutionRoot != null)
        {
            providers.Add(new DevelopmentModuleScanningProvider(solutionRoot, HostType.Blazor, NullLogger.Instance));
        }
#endif

        // App Modules Directory
        var appModules = Path.Combine(AppContext.BaseDirectory, "Modules");
        if (Directory.Exists(appModules))
        {
            providers.Add(new DirectoryModuleProvider(appModules, NullLogger.Instance, isSystem: true));
        }

        // User Modules Directory
        var userModules = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Modulus", "Modules");
        if (Directory.Exists(userModules))
        {
            providers.Add(new DirectoryModuleProvider(userModules, NullLogger.Instance, isSystem: false));
        }

        // Database
        var dbPath = DatabaseServiceExtensions.GetDefaultDatabasePath();
        builder.Services.AddModulusDatabase(dbPath);

        // Create Modulus App
        var appTask = ModulusApplicationFactory.CreateAsync<BlazorHostModule>(builder.Services, providers, HostType.Blazor);
        var modulusApp = appTask.GetAwaiter().GetResult();

        // Register the app as IModulusApplication so it can be injected
        builder.Services.AddSingleton<IModulusApplication>(modulusApp);

        return builder.Build();
    }

    private static string? FindSolutionRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Modulus.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        return null;
    }
}
