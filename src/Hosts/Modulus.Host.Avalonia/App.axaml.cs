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

            // Module Directories - explicit module installation paths
            var moduleDirectories = new System.Collections.Generic.List<ModuleDirectory>();

#if DEBUG
            // Development: Load from artifacts/Modules/ (populated by nuke build-module)
            var solutionRoot = FindSolutionRoot(AppContext.BaseDirectory);
            if (solutionRoot != null)
            {
                var artifactsModules = Path.Combine(solutionRoot, "artifacts", "Modules");
                if (Directory.Exists(artifactsModules))
                {
                    // User modules from artifacts - NOT system modules
                    moduleDirectories.Add(new ModuleDirectory(artifactsModules, IsSystem: false));
                }
            }
#else
            // Production: Load from {AppBaseDir}/Modules/ 
            var appModules = Path.Combine(AppContext.BaseDirectory, "Modules");
            if (Directory.Exists(appModules))
            {
                moduleDirectories.Add(new ModuleDirectory(appModules, IsSystem: true));
            }
#endif

            // User-installed modules (for runtime installation)
            var userModules = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Modulus", "Modules");
            if (Directory.Exists(userModules))
            {
                moduleDirectories.Add(new ModuleDirectory(userModules, IsSystem: false));
            }

            // Database (configurable name; defaults to framework/solution name)
            var dbName = configuration["Modulus:DatabaseName"] ?? "Modulus";
            var dbPath = DatabaseServiceExtensions.GetDefaultDatabasePath(dbName);
            services.AddModulusDatabase(dbPath);

            // Repositories & installers (needed at runtime for menu registration)
            services.AddScoped<IModuleRepository, ModuleRepository>();
            services.AddScoped<IMenuRepository, MenuRepository>();
            services.AddScoped<IPendingCleanupRepository, PendingCleanupRepository>();
            services.AddSingleton<IModuleCleanupService, ModuleCleanupService>();
            services.AddScoped<IModuleInstallerService, ModuleInstallerService>();
            services.AddScoped<SystemModuleInstaller>();
            services.AddScoped<ModuleIntegrityChecker>();
            services.AddScoped<HostModuleSeeder>();
            services.AddSingleton<ILazyModuleLoader, LazyModuleLoader>();

            // Get host version from assembly
            var hostVersion = typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0);

            // Bootstrap Modulus
            var appTask = Task.Run(async () => 
                await ModulusApplicationFactory.CreateAsync<AvaloniaHostModule>(services, moduleDirectories, ModulusHostIds.Avalonia, dbPath, configuration, loggerFactory, hostVersion)
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

            // Seed host module menus to database (menus come from DB only at render time)
            using (var scope = Services.CreateScope())
            {
                var hostSeeder = scope.ServiceProvider.GetRequiredService<HostModuleSeeder>();
                var hostVersionString = typeof(AvaloniaHostModule).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
                hostSeeder.SeedOrUpdateFromAttributesAsync(
                    ModulusHostIds.Avalonia,
                    "Modulus Host (Avalonia)",
                    hostVersionString,
                    typeof(AvaloniaHostModule)
                ).GetAwaiter().GetResult();
            }
            
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
            
            // Navigate to default view (Modules)
            shellVm.NavigateTo<ModuleListViewModel>();
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
    
#if DEBUG
    private static string? FindSolutionRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Modulus.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
#endif
}
