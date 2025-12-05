using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Runtime;

public interface IModulusApplication : IDisposable
{
    IServiceProvider ServiceProvider { get; }
    void SetServiceProvider(IServiceProvider serviceProvider);
    Task InitializeAsync();
    Task ShutdownAsync();
    
    /// <summary>
    /// Gets all scanned module metadata (from declarative attributes).
    /// </summary>
    IReadOnlyList<ModuleMetadata> ModuleMetadataList { get; }
    
    /// <summary>
    /// Gets all loaded module assemblies (Core + UI modules).
    /// </summary>
    IReadOnlyList<Assembly> LoadedModuleAssemblies { get; }
}

public class ModulusApplication : IModulusApplication
{
    private readonly IServiceCollection _services;
    private IServiceProvider? _serviceProvider;
    private readonly ModuleManager _moduleManager;
    private readonly ILogger<ModulusApplication> _logger;
    private readonly ModuleMetadataScanner _metadataScanner;
    private readonly List<ModuleMetadata> _moduleMetadataList = new();
    private bool _initialized;

    public IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Application not initialized.");
    public IReadOnlyList<ModuleMetadata> ModuleMetadataList => _moduleMetadataList;
    
    public IReadOnlyList<Assembly> LoadedModuleAssemblies => _moduleManager.GetSortedModules()
        .Select(m => m.GetType().Assembly)
        .Distinct()
        .ToList();

    internal ModulusApplication(IServiceCollection services, ModuleManager moduleManager, ILogger<ModulusApplication> logger)
    {
        _services = services;
        _moduleManager = moduleManager;
        _logger = logger;
        _metadataScanner = new ModuleMetadataScanner(logger);
    }

    public void ConfigureServices()
    {
        var context = new ModuleLifecycleContext(_services);
        var sortedModules = _moduleManager.GetSortedModules();

        // Scan core module metadata
        foreach (var module in sortedModules)
        {
            var metadata = _metadataScanner.ScanCoreModule(module.GetType());
            if (metadata != null)
            {
                _moduleMetadataList.Add(metadata);
            }
        }

        foreach (var module in sortedModules)
        {
            module.PreConfigureServices(context);
        }

        foreach (var module in sortedModules)
        {
            module.ConfigureServices(context);
        }

        foreach (var module in sortedModules)
        {
            module.PostConfigureServices(context);
        }
    }

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider not set.");
        }

        // Auto-register menu items from UI module attributes
        RegisterMenuItemsFromAttributes();

        var context = new ModuleInitializationContext(_serviceProvider);
        var sortedModules = _moduleManager.GetSortedModules();

        foreach (var module in sortedModules)
        {
            try 
            {
                await module.OnApplicationInitializationAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing module {ModuleType}", module.GetType().Name);
                throw;
            }
        }
    }

    /// <summary>
    /// Automatically registers menu items from UI module attributes (Avalonia/Blazor).
    /// </summary>
    private void RegisterMenuItemsFromAttributes()
    {
        var menuRegistry = _serviceProvider?.GetService<IMenuRegistry>();
        if (menuRegistry == null)
        {
            _logger.LogDebug("IMenuRegistry not registered. Skipping declarative menu registration.");
            return;
        }

        var runtimeContext = _serviceProvider?.GetService<RuntimeContext>();
        var currentHost = runtimeContext?.HostType;

        var sortedModules = _moduleManager.GetSortedModules();

        foreach (var module in sortedModules)
        {
            var moduleType = module.GetType();
            List<ModuleMenuMetadata> menus;

            // Scan based on host type
            if (currentHost == HostType.Avalonia)
            {
                menus = _metadataScanner.ScanAvaloniaMenus(moduleType);
            }
            else if (currentHost == HostType.Blazor)
            {
                menus = _metadataScanner.ScanBlazorMenus(moduleType);
            }
            else
            {
                continue;
            }

            foreach (var menu in menus)
            {
                var navigationKey = !string.IsNullOrEmpty(menu.Route) ? menu.Route : menu.ViewModelType ?? menu.Id;

                var item = new MenuItem(
                    $"{moduleType.Name}.{menu.Id}",
                    menu.DisplayName,
                    menu.Icon,
                    navigationKey,
                    menu.Location,
                    menu.Order
                );

                menuRegistry.Register(item);
                _logger.LogDebug("Registered menu: {DisplayName} -> {NavigationKey}", menu.DisplayName, navigationKey);
            }
        }
    }

    public async Task ShutdownAsync()
    {
        if (!_initialized) return;
        
        var context = new ModuleInitializationContext(_serviceProvider!);
        var sortedModules = _moduleManager.GetSortedModules();
        
        // Shutdown in reverse order
        for (int i = sortedModules.Count - 1; i >= 0; i--)
        {
            try 
            {
                await sortedModules[i].OnApplicationShutdownAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down module {ModuleType}", sortedModules[i].GetType().Name);
            }
        }
        
        _initialized = false;
    }

    public void Dispose()
    {
        // cleanup
    }
}
