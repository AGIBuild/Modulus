using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core;
using Modulus.Core.Data;
using Modulus.Core.Installation;
using Modulus.Core.Logging;
using Modulus.Core.Runtime;
using Modulus.HostSdk.Abstractions;
using Modulus.HostSdk.Runtime;
using Modulus.Host.Avalonia.Services;
using Modulus.Host.Avalonia.Shell.Services;
using Modulus.Host.Avalonia.Shell.ViewModels;
using Modulus.Host.Avalonia.Shell.Views;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Modulus.Host.Avalonia;

[AvaloniaMenu("extensions", "Extensions", typeof(ModuleListViewModel), Icon = IconKind.AppsAddIn, Order = 1000, Location = MenuLocation.Main)]
[AvaloniaMenu("settings", "Settings", typeof(SettingsViewModel), Icon = IconKind.Settings, Order = 100, Location = MenuLocation.Bottom)]
public class AvaloniaHostModule : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        // UI Services
        context.Services.AddSingleton<IUIFactory, AvaloniaUIFactory>();
        context.Services.AddSingleton<IViewRegistry, ViewRegistry>();
        context.Services.AddSingleton<IThemeService, AvaloniaThemeService>();
        context.Services.AddSingleton<INotificationService, AvaloniaNotificationService>();
        
        // Shell Services
        context.Services.AddSingleton<IMenuRegistry, MenuRegistry>();
        context.Services.AddSingleton<AvaloniaNavigationService>();
        context.Services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<AvaloniaNavigationService>());
        
        // Shell ViewModels
        context.Services.AddSingleton<ShellViewModel>();
        context.Services.AddTransient<ModuleListViewModel>();
        context.Services.AddTransient<SettingsViewModel>();
    }

    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Register view mappings (menus come from database - full database-driven approach)
        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<ModuleListViewModel, ModuleListView>();
        viewRegistry.Register<SettingsViewModel, SettingsView>();

        return Task.CompletedTask;
    }
}

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }
    private IModulusApplication? _modulusApp;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Register UI thread exception handler after Avalonia is initialized
        Program.RegisterDispatcherExceptionHandler();
        
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();

            // Configuration (environment-aware via DOTNET_ENVIRONMENT)
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var loggerFactory = ModulusLogging.CreateLoggerFactory(configuration, ModulusHostIds.Avalonia);
            ModulusLogging.AddLoggerFactory(services, loggerFactory);

            // Database (configurable name; defaults to framework/solution name)
            var dbName = configuration["Modulus:DatabaseName"] ?? "Modulus";
            var dbPath = DatabaseServiceExtensions.GetDefaultDatabasePath(dbName);

            // Get host version from assembly
            var hostVersion = typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0);

            var hostSdkOptions = new ModulusHostSdkOptions
            {
                HostId = ModulusHostIds.Avalonia,
                HostVersion = hostVersion,
                DatabasePath = dbPath
            };
            var hostSdkBuilder = new ModulusHostSdkBuilder(services, configuration, hostSdkOptions)
                .AddDefaultModuleDirectories()
                .AddDefaultRuntimeServices();

            // Bootstrap Modulus via Host SDK
            var appTask = Task.Run(async () =>
                await hostSdkBuilder.BuildAsync<AvaloniaHostModule>(loggerFactory)
            );
            _modulusApp = appTask.GetAwaiter().GetResult();
            
            // Create Window
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            
            services.AddSingleton<IViewHost>(new AvaloniaViewHost(mainWindow));
            
            Services = services.BuildServiceProvider();
            _modulusApp.SetServiceProvider(Services);
            
            // Initialize Database
            var database = Services.GetRequiredService<IAppDatabase>();
            database.InitializeAsync().GetAwaiter().GetResult();
            
            // No bundled module seeding (menus are projected from module entry attributes during install/update)
            
            // Initialize Theme Service (load saved theme)
            var themeService = Services.GetRequiredService<IThemeService>() as AvaloniaThemeService;
            themeService?.InitializeAsync().GetAwaiter().GetResult();
            
            // Register Shell Views (view mappings, menus come from database)
            var viewRegistry = Services.GetRequiredService<IViewRegistry>();
            viewRegistry.Register<ModuleListViewModel, ModuleListView>();
            viewRegistry.Register<SettingsViewModel, SettingsView>();
            
            // Initialize Modules (loads menus from database into IMenuRegistry)
            _modulusApp.InitializeAsync().GetAwaiter().GetResult();
            
            // Create and set ShellViewModel
            var shellVm = Services.GetRequiredService<ShellViewModel>();
            mainWindow.DataContext = shellVm;
            
            // Navigate to default view: first main menu item by Order (typically Home).
            // Avoid hard-coded legacy NavigationKey.
            var menuRegistry = Services.GetRequiredService<IMenuRegistry>();
            var defaultMenu = menuRegistry.GetItems(MenuLocation.Main)
                .OrderBy(m => m.Order)
                .FirstOrDefault();

            if (defaultMenu != null && !string.IsNullOrWhiteSpace(defaultMenu.NavigationKey))
            {
                shellVm.NavigateToRoute(defaultMenu.NavigationKey);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _modulusApp?.ShutdownAsync().GetAwaiter().GetResult();
    }

    public void ToggleTheme()
    {
        var current = RequestedThemeVariant;
        RequestedThemeVariant = current == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}
