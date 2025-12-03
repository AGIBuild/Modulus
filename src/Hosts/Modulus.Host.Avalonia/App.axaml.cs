using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Modulus.Core;
using Modulus.Core.Data;
using Modulus.Core.Runtime;
using Modulus.Host.Avalonia.Services;
using Modulus.Host.Avalonia.Shell.Services;
using Modulus.Host.Avalonia.Shell.ViewModels;
using Modulus.Host.Avalonia.Shell.Views;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Modulus.Host.Avalonia;

public class AvaloniaHostModule : ModuleBase
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        // UI Services
        context.Services.AddSingleton<IUIFactory, AvaloniaUIFactory>();
        context.Services.AddSingleton<IViewRegistry, ViewRegistry>();
        context.Services.AddSingleton<IThemeService, AvaloniaThemeService>();
        
        // Shell Services
        context.Services.AddSingleton<IMenuRegistry, MenuRegistry>();
        
        // Shell ViewModels
        context.Services.AddSingleton<ShellViewModel>();
        context.Services.AddTransient<ModuleListViewModel>();
        context.Services.AddTransient<SettingsViewModel>();
    }
}

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }
    private IModulusApplication? _modulusApp;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            
            // Add Logging
            services.AddLogging();
            
            // Module Providers
            var providers = new System.Collections.Generic.List<IModuleProvider>();

            #if DEBUG
            var solutionRoot = FindSolutionRoot(AppContext.BaseDirectory);
            if (solutionRoot != null)
            {
                providers.Add(new DevelopmentModuleScanningProvider(solutionRoot, HostType.Avalonia, NullLogger.Instance));
            }
            
            var outputModules = Path.Combine(solutionRoot ?? AppContext.BaseDirectory, "_output", "modules");
            if (Directory.Exists(outputModules))
            {
                providers.Add(new DirectoryModuleProvider(outputModules, NullLogger.Instance, isSystem: true));
            }
            #endif

            var appModules = Path.Combine(AppContext.BaseDirectory, "Modules");
            if (Directory.Exists(appModules))
            {
                providers.Add(new DirectoryModuleProvider(appModules, NullLogger.Instance, isSystem: true));
            }

            var userModules = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Modulus", "Modules");
            if (Directory.Exists(userModules))
            {
                providers.Add(new DirectoryModuleProvider(userModules, NullLogger.Instance, isSystem: false));
            }

            // Database
            var dbPath = DatabaseServiceExtensions.GetDefaultDatabasePath();
            services.AddModulusDatabase(dbPath);

            // Bootstrap Modulus
            var appTask = Task.Run(async () => 
                await ModulusApplicationFactory.CreateAsync<AvaloniaHostModule>(services, providers, HostType.Avalonia)
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
            
            // Initialize Theme Service (load saved theme)
            var themeService = Services.GetRequiredService<IThemeService>() as AvaloniaThemeService;
            themeService?.InitializeAsync().GetAwaiter().GetResult();
            
            // Register Shell Views
            var viewRegistry = Services.GetRequiredService<IViewRegistry>();
            viewRegistry.Register<ModuleListViewModel, ModuleListView>();
            viewRegistry.Register<SettingsViewModel, SettingsView>();
            
            // Register Shell Menu Items
            var menuRegistry = Services.GetRequiredService<IMenuRegistry>();
            menuRegistry.Register(new MenuItem("Modules", "Modules", "🧩", typeof(ModuleListViewModel).FullName!, MenuLocation.Main, 0));
            menuRegistry.Register(new MenuItem("Settings", "Settings", "⚙️", typeof(SettingsViewModel).FullName!, MenuLocation.Bottom, 100));
            
            // Initialize Modules
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
