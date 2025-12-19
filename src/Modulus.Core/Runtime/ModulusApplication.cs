using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core.Installation;
using Modulus.Infrastructure.Data.Repositories;
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
    /// Gets all scanned module metadata.
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
    private readonly RuntimeContext _runtimeContext;
    private readonly ILogger<ModulusApplication> _logger;
    private readonly List<ModuleMetadata> _moduleMetadataList = new();
    private bool _initialized;

    public IServiceProvider ServiceProvider => _serviceProvider ?? throw new InvalidOperationException("Application not initialized.");
    public IReadOnlyList<ModuleMetadata> ModuleMetadataList => _moduleMetadataList;
    
    /// <summary>
    /// Gets all loaded module assemblies (including all UI assemblies loaded via RuntimeModuleHandle).
    /// </summary>
    public IReadOnlyList<Assembly> LoadedModuleAssemblies => _runtimeContext.ModuleHandles
        .SelectMany(h => h.Assemblies)
        .Distinct()
        .ToList();

    internal ModulusApplication(IServiceCollection services, ModuleManager moduleManager, RuntimeContext runtimeContext, ILogger<ModulusApplication> logger)
    {
        _services = services;
        _moduleManager = moduleManager;
        _runtimeContext = runtimeContext;
        _logger = logger;
    }

    public void ConfigureServices()
    {
        var context = new ModuleLifecycleContext(_services);
        var sortedModules = _moduleManager.GetSortedModules();

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

        if (_serviceProvider.GetService<IModuleLoader>() is IHostAwareModuleLoader hostAwareLoader)
        {
            hostAwareLoader.BindHostServices(_serviceProvider);
        }
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("ServiceProvider not set.");
        }

        var runtimeContext = _serviceProvider.GetService<RuntimeContext>();
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["HostType"] = runtimeContext?.HostType ?? "unknown"
        });

        // Process any pending module directory cleanups from previous session
        var cleanupService = _serviceProvider.GetService<IModuleCleanupService>();
        if (cleanupService != null)
        {
            await cleanupService.ProcessPendingCleanupsAsync();
        }

        // Initialize pre-loaded modules (those loaded during CreateAsync with skipModuleInitialization=true)
        var moduleLoader = _serviceProvider.GetService<IModuleLoader>();
        _logger.LogInformation("ModuleLoader type: {Type}, IsHostAware: {IsHostAware}", 
            moduleLoader?.GetType().Name ?? "null", 
            moduleLoader is IHostAwareModuleLoader);
        
        if (moduleLoader is IHostAwareModuleLoader hostAwareLoader)
        {
            await hostAwareLoader.InitializeLoadedModulesAsync();
        }
        else
        {
            _logger.LogWarning("ModuleLoader is not IHostAwareModuleLoader, cannot initialize pre-loaded modules.");
        }

        // Register menus from Database
        await RegisterMenuItemsFromDatabaseAsync();
        
        // Populate metadata from Database
        await LoadModuleMetadataAsync();

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

    private async Task RegisterMenuItemsFromDatabaseAsync()
    {
        using var scope = _serviceProvider!.CreateScope();
        var menuRepo = scope.ServiceProvider.GetService<IMenuRepository>();
        var menuRegistry = _serviceProvider!.GetService<IMenuRegistry>();
        
        if (menuRepo == null || menuRegistry == null)
        {
            _logger.LogWarning("MenuRepository or MenuRegistry missing. Skipping menu registration.");
            return;
        }
        
        // Only load enabled menus
        var menus = await menuRepo.GetAllEnabledAsync();

        var roots = MenuTreeBuilder.Build(menus, _logger);

        foreach (var root in roots)
        {
            menuRegistry.Register(root);
        }

        _logger.LogInformation(
            "Registered {RootCount} menu root items from database ({TotalCount} total entries).",
            roots.Count,
            menus.Count);
    }

    private async Task LoadModuleMetadataAsync()
    {
        using var scope = _serviceProvider!.CreateScope();
        var moduleRepo = scope.ServiceProvider.GetService<IModuleRepository>();
        if (moduleRepo == null) return;

        var modules = await moduleRepo.GetEnabledModulesAsync();
        foreach (var m in modules)
        {
            _moduleMetadataList.Add(new ModuleMetadata
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                Version = m.Version,
                Author = m.Publisher ?? "",
                // Type is not strictly necessary for display purposes
            });
        }
    }

    public async Task ShutdownAsync()
    {
        if (!_initialized) return;

        var runtimeContext = _serviceProvider?.GetService<RuntimeContext>();
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["HostType"] = runtimeContext?.HostType ?? "unknown"
        });

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

/// <summary>
/// Metadata for a loaded module.
/// </summary>
public class ModuleMetadata
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Version { get; set; } = "";
    public string Author { get; set; } = "";
}