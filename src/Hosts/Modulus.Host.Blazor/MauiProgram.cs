using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Modulus.Core;
using Modulus.Core.Data;
using Modulus.Core.Installation;
using Modulus.Core.Logging;
using Modulus.Core.Runtime;
using Modulus.HostSdk.Abstractions;
using Modulus.HostSdk.Runtime;
using Modulus.Host.Blazor.Services;
using Modulus.Host.Blazor.Shell.Services;
using Modulus.Host.Blazor.Shell.ViewModels;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

namespace Modulus.Host.Blazor;

[DependsOn()]
[BlazorMenu("extensions", "Extensions", "/modules", Icon = IconKind.AppsAddIn, Order = 1000, Location = MenuLocation.Main)]
[BlazorMenu("settings", "Settings", "/settings", Icon = IconKind.Settings, Order = 100, Location = MenuLocation.Bottom)]
public class BlazorHostModule : ModulusPackage
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
        context.Services.AddSingleton<ModuleStylesheetService>();

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

        // Initialize logger
        var loggerFactory = ModulusLogging.CreateLoggerFactory(configuration, ModulusHostIds.Blazor);

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        ModulusLogging.AddLoggerFactory(builder.Services, loggerFactory);

        // Module Directories - explicit module installation paths
        // Database (configurable name; defaults to framework/solution name)
        var dbName = configuration["Modulus:DatabaseName"] ?? "Modulus";
        var dbPath = DatabaseServiceExtensions.GetDefaultDatabasePath(dbName);

        // Get host version from assembly
        var hostVersion = typeof(BlazorHostModule).Assembly.GetName().Version ?? new Version(1, 0, 0);

        // Create Modulus App via Host SDK (use Task.Run to avoid deadlock in UI sync context)
        var hostSdkOptions = new ModulusHostSdkOptions
        {
            HostId = ModulusHostIds.Blazor,
            HostVersion = hostVersion,
            DatabasePath = dbPath
        };
        var hostSdkBuilder = new ModulusHostSdkBuilder(builder.Services, configuration, hostSdkOptions)
            .AddDefaultModuleDirectories()
            .AddDefaultRuntimeServices();

        var modulusApp = Task.Run(async () =>
            await hostSdkBuilder.BuildAsync<BlazorHostModule>(loggerFactory)
        ).GetAwaiter().GetResult();

        // Register the app as IModulusApplication so it can be injected
        builder.Services.AddSingleton<IModulusApplication>(modulusApp);

        // Build the app
        var app = builder.Build();
        
        // No bundled module seeding (menus are projected from module entry attributes during install/update)

        return app;
    }
}
