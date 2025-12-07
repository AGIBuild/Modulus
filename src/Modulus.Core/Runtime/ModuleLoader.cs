using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core;
using Modulus.Core.Architecture;
using Modulus.Core.Manifest;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using NuGet.Versioning;

namespace Modulus.Core.Runtime;

public interface IHostAwareModuleLoader
{
    void BindHostServices(IServiceProvider hostServices);
    Task InitializeLoadedModulesAsync(CancellationToken cancellationToken = default);
}

public sealed class ModuleLoader : IModuleLoader, IHostAwareModuleLoader
{
    private readonly RuntimeContext _runtimeContext;
    private readonly IManifestValidator _manifestValidator;
    private readonly ILogger<ModuleLoader> _logger;
    private readonly ISharedAssemblyCatalog _sharedAssemblyCatalog;
    private IServiceProvider? _hostServices;

    public ModuleLoader(RuntimeContext runtimeContext, IManifestValidator manifestValidator, ISharedAssemblyCatalog sharedAssemblyCatalog, ILogger<ModuleLoader> logger, IServiceProvider? hostServices = null)
    {
        _runtimeContext = runtimeContext;
        _manifestValidator = manifestValidator;
        _sharedAssemblyCatalog = sharedAssemblyCatalog;
        _logger = logger;
        _hostServices = hostServices;
    }

    public void BindHostServices(IServiceProvider hostServices)
    {
        _logger.LogInformation("BindHostServices called. Updating {Count} module handles...", _runtimeContext.ModuleHandles.Count);
        _hostServices = hostServices;
        
        // Update all existing module handles with the new host services
        foreach (var handle in _runtimeContext.ModuleHandles)
        {
            _logger.LogInformation("  Updating composite provider for module {ModuleId}", handle.RuntimeModule.Descriptor.Id);
            handle.UpdateCompositeServiceProvider(hostServices);
        }
    }

    /// <summary>
    /// Initializes all pre-loaded modules that were loaded with skipModuleInitialization=true.
    /// This should be called after BindHostServices.
    /// </summary>
    public async Task InitializeLoadedModulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("InitializeLoadedModulesAsync called. HostServices bound: {Bound}", _hostServices != null);
        
        if (_hostServices == null)
        {
            _logger.LogWarning("Cannot initialize modules: host services not bound.");
            return;
        }

        _logger.LogInformation("Found {Count} module handles to initialize.", _runtimeContext.ModuleHandles.Count);
        
        foreach (var handle in _runtimeContext.ModuleHandles)
        {
            var module = handle.RuntimeModule;
            _logger.LogInformation("Checking module {ModuleId}, State={State}", module.Descriptor.Id, module.State);
            if (module.State != ModuleState.Loaded) continue; // Skip already initialized or errored modules

            _logger.LogInformation("Initializing pre-loaded module {ModuleId} with {InstanceCount} instances...", 
                module.Descriptor.Id, handle.ModuleInstances.Count);

            var compositeProvider = new CompositeServiceProvider(handle.ServiceProvider, _hostServices);
            var initContext = new ModuleInitializationContext(compositeProvider);

            try
            {
                foreach (var moduleInstance in handle.ModuleInstances)
                {
                    _logger.LogInformation("  Calling OnApplicationInitializationAsync on {Type}...", moduleInstance.GetType().Name);
                    await moduleInstance.OnApplicationInitializationAsync(initContext, cancellationToken).ConfigureAwait(false);
                }
                module.State = ModuleState.Active;
                _logger.LogInformation("Module {ModuleId} initialized successfully.", module.Descriptor.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing module {ModuleId}", module.Descriptor.Id);
                module.State = ModuleState.Error;
            }
        }
    }

    public async Task<ModuleDescriptor?> LoadAsync(string packagePath, bool isSystem = false, bool skipModuleInitialization = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Directory.Exists(packagePath))
        {
            _logger.LogWarning("Module package path {Path} does not exist.", packagePath);
            return null;
        }

        var manifestPath = Path.Combine(packagePath, "manifest.json");
        var manifest = await ManifestReader.ReadFromFileAsync(manifestPath).ConfigureAwait(false);
        if (manifest is null)
        {
            _logger.LogWarning("Failed to read manifest at {ManifestPath}.", manifestPath);
            return null;
        }

        var hostType = _runtimeContext.HostType;
        if (string.IsNullOrWhiteSpace(hostType))
        {
            _logger.LogWarning("Host type is not set in RuntimeContext. Set host before loading modules.");
            return null;
        }

        var manifestValid = await _manifestValidator.ValidateAsync(packagePath, manifestPath, manifest, hostType, cancellationToken).ConfigureAwait(false);
        if (!manifestValid)
        {
            _logger.LogWarning("Manifest validation failed for {ManifestPath}.", manifestPath);
            return null;
        }

        // Check if already loaded
        if (_runtimeContext.TryGetModule(manifest.Id, out var existingModule))
        {
            _logger.LogWarning("Module {ModuleId} is already loaded.", manifest.Id);
            return existingModule!.Descriptor;
        }

        if (!NuGetVersion.TryParse(manifest.Version, out _))
        {
            _logger.LogWarning("Module {ModuleId} version {Version} is not a valid semantic version.", manifest.Id, manifest.Version);
            return null;
        }

        if (!EnsureDependenciesSatisfied(manifest))
        {
            return null;
        }

        var descriptor = new ModuleDescriptor(
            manifest.Id, 
            manifest.Version, 
            manifest.DisplayName, 
            manifest.Description,
            manifest.SupportedHosts);
        var alc = new ModuleLoadContext(manifest.Id, packagePath, _sharedAssemblyCatalog, _logger);
        var loadedAssemblies = new List<Assembly>();

        // Get Current Host ID from RuntimeContext
        var currentHostId = hostType;

        try
        {
            // 1. Load Core Assemblies (Host-Agnostic)
            foreach (var assemblyRelativePath in manifest.CoreAssemblies)
            {
                var assemblyPath = Path.Combine(packagePath, assemblyRelativePath);
                if (!File.Exists(assemblyPath))
                {
                    _logger.LogWarning("Assembly {AssemblyPath} not found for module {ModuleId}.", assemblyPath, manifest.Id);
                    continue;
                }

                var assembly = alc.LoadFromAssemblyPath(assemblyPath);
                if (assembly != null)
                {
                    loadedAssemblies.Add(assembly);
                }
            }

            // 2. Load UI Assemblies (Host-Specific)
            if (!string.IsNullOrEmpty(currentHostId) && manifest.UiAssemblies.TryGetValue(currentHostId, out var uiAssemblies))
            {
                foreach (var assemblyRelativePath in uiAssemblies)
                {
                    var assemblyPath = Path.Combine(packagePath, assemblyRelativePath);
                    if (!File.Exists(assemblyPath))
                    {
                        _logger.LogWarning("UI Assembly {AssemblyPath} not found for module {ModuleId} host {HostType}.", assemblyPath, manifest.Id, currentHostId);
                        continue;
                    }
                    var assembly = alc.LoadFromAssemblyPath(assemblyPath);
                    if (assembly != null)
                    {
                        loadedAssemblies.Add(assembly);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assemblies for module {ModuleId}.", manifest.Id);
            alc.Unload();
            return null;
        }

        if (loadedAssemblies.Count == 0 && alc.Assemblies.Any())
        {
            loadedAssemblies.AddRange(alc.Assemblies);
        }

        var componentTypes = loadedAssemblies
            .SelectMany(SafeGetTypes)
            .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .ToList();

        // Entry component filtering (if specified)
        if (!string.IsNullOrWhiteSpace(manifest.EntryComponent))
        {
            var entryName = manifest.EntryComponent!;
            var map = componentTypes.ToDictionary(t => t.FullName ?? t.Name, StringComparer.OrdinalIgnoreCase);
            if (map.TryGetValue(entryName, out var entryType))
            {
                var reachable = new HashSet<Type>();
                void Dfs(Type t)
                {
                    if (!reachable.Add(t)) return;
                    foreach (var dep in GetComponentDependencies(t))
                    {
                        var depId = dep.FullName ?? dep.Name;
                        if (depId != null && map.TryGetValue(depId, out var depType))
                        {
                            Dfs(depType);
                        }
                    }
                }
                Dfs(entryType);
                componentTypes = reachable.ToList();
            }
            else
            {
                _logger.LogWarning("Entry component {Entry} not found in module {ModuleId}; falling back to all components.", entryName, manifest.Id);
            }
        }

        IReadOnlyList<Type> sortedTypes;
        try
        {
            sortedTypes = ComponentDependencyResolver.TopologicallySort(componentTypes, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve component dependencies for module {ModuleId}.", manifest.Id);
            return null;
        }

        var sortedModules = new List<IModule>();
        foreach (var type in sortedTypes)
        {
            try
            {
                var instance = CreateModuleInstance(type);
                if (instance != null)
                {
                    sortedModules.Add(instance);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to instantiate component {Component}", type.FullName);
                return null;
            }
        }

        var services = new ServiceCollection();
        services.AddSingleton(_runtimeContext);
        services.AddSingleton(descriptor);
        services.AddSingleton(manifest);
        services.AddSingleton(alc);

        var lifecycleContext = new ModuleLifecycleContext(services);
        foreach (var module in sortedModules)
        {
            module.PreConfigureServices(lifecycleContext);
        }

        foreach (var module in sortedModules)
        {
            module.ConfigureServices(lifecycleContext);
        }

        foreach (var module in sortedModules)
        {
            module.PostConfigureServices(lifecycleContext);
        }

        var moduleProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var moduleScope = moduleProvider.CreateScope();
        var scopedProvider = moduleScope.ServiceProvider;
        var compositeProvider = _hostServices != null
            ? new CompositeServiceProvider(scopedProvider, _hostServices)
            : scopedProvider;

        var registeredMenus = new List<MenuItem>();
        var runtimeModule = new RuntimeModule(descriptor, alc, packagePath, manifest, isSystem)
        {
            State = skipModuleInitialization ? ModuleState.Loaded : ModuleState.Active
        };

        var initialized = false;
        try
        {
            // Only run module initialization if not skipped (host services must be bound first)
            if (!skipModuleInitialization)
            {
                var initContext = new ModuleInitializationContext(compositeProvider);
                foreach (var module in sortedModules)
                {
                    try
                    {
                        await module.OnApplicationInitializationAsync(initContext, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error initializing module {ModuleType}", module.GetType().Name);
                        return null;
                    }
                }
                runtimeModule.State = ModuleState.Active;
            }

            _runtimeContext.RegisterModule(runtimeModule);
            var handle = new RuntimeModuleHandle(runtimeModule, manifest, moduleScope, moduleProvider, compositeProvider, sortedModules, registeredMenus, loadedAssemblies);
            _runtimeContext.RegisterModuleHandle(handle);
            initialized = true;

            _logger.LogInformation("Module {ModuleId} (v{Version}) loaded from {PackagePath} for host {HostType} (System: {IsSystem}, Initialized: {Initialized}).", 
                manifest.Id, manifest.Version, packagePath, currentHostId ?? "None", isSystem, !skipModuleInitialization);

            return descriptor;
        }
        finally
        {
            if (!initialized)
            {
                moduleScope.Dispose();
                moduleProvider.Dispose();
                alc.Unload();
            }
        }
    }

    public async Task UnloadAsync(string moduleId)
    {
        if (_runtimeContext.TryGetModule(moduleId, out var runtimeModule))
        {
            if (runtimeModule!.IsSystem)
            {
                _logger.LogWarning("Cannot unload system module {ModuleId}.", moduleId);
                throw new InvalidOperationException($"Cannot unload system module {moduleId}.");
            }

            _logger.LogInformation("Unloading module {ModuleId}...", moduleId);

            RuntimeModuleHandle? handle = null;
            if (_runtimeContext.TryGetModuleHandle(moduleId, out var storedHandle))
            {
                handle = storedHandle;
            }

            if (handle != null)
            {
                var shutdownContext = new ModuleInitializationContext(handle.CompositeServiceProvider);
                var moduleInstances = handle.ModuleInstances.ToList();
                for (var i = moduleInstances.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        await moduleInstances[i].OnApplicationShutdownAsync(shutdownContext).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error shutting down module {ModuleType}", moduleInstances[i].GetType().Name);
                    }
                }

                if (_hostServices?.GetService<IMenuRegistry>() is IMenuRegistry menuRegistry)
                {
                    menuRegistry.UnregisterModuleItems(moduleId);
                }
                else if (handle.CompositeServiceProvider.GetService<IMenuRegistry>() is IMenuRegistry compositeRegistry)
                {
                    compositeRegistry.UnregisterModuleItems(moduleId);
                }

                await handle.DisposeAsync().ConfigureAwait(false);
                _runtimeContext.RemoveModuleHandle(moduleId);
            }
            else
            {
                // Fallback: try to clean up menus if handle is missing (e.g. startup modules)
                if (_hostServices?.GetService<IMenuRegistry>() is IMenuRegistry menuRegistry)
                {
                    menuRegistry.UnregisterModuleItems(moduleId);
                }
            }

            _runtimeContext.RemoveModule(moduleId);

            runtimeModule.State = ModuleState.Unloaded;
            runtimeModule.LoadContext.Unload();

            _logger.LogInformation("Module {ModuleId} unloaded.", moduleId);
        }
        else
        {
            _logger.LogWarning("Cannot unload module {ModuleId}: not found.", moduleId);
        }
    }

    public async Task<ModuleDescriptor?> ReloadAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        if (_runtimeContext.TryGetModule(moduleId, out var runtimeModule))
        {
            var packagePath = runtimeModule!.PackagePath;
            var isSystem = runtimeModule.IsSystem;
            await UnloadAsync(moduleId);
            
            // Allow some time for cleanup if necessary
            
            // Pass false for isSystem (reloaded modules are usually user modules unless we track it)
            // But we can check existing descriptor? No, existing module is gone.
            // Let's assume reloaded module keeps its system status?
            // We should probably store IsSystem in manifest? No, it's a provider property.
            // For now, default to false (safe) or true if we could track it.
            // Actually we can check the path again via providers? Too slow.
            // Reload should initialize immediately (user-initiated action)
            return await LoadAsync(packagePath, isSystem, skipModuleInitialization: false, cancellationToken).ConfigureAwait(false);
        }
        
        _logger.LogWarning("Cannot reload module {ModuleId}: not found.", moduleId);
        return null;
    }

    public async Task<ModuleDescriptor?> GetDescriptorAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(packagePath, "manifest.json");
        var manifest = await ManifestReader.ReadFromFileAsync(manifestPath).ConfigureAwait(false);
        if (manifest is null)
        {
            return null;
        }
        return new ModuleDescriptor(
            manifest.Id, 
            manifest.Version, 
            manifest.DisplayName,
            manifest.Description,
            manifest.SupportedHosts);
    }

    private bool EnsureDependenciesSatisfied(ModuleManifest manifest)
    {
        foreach (var (dependencyId, dependencyRange) in manifest.Dependencies)
        {
            if (!_runtimeContext.TryGetModule(dependencyId, out var dependencyModule) || dependencyModule == null)
            {
                _logger.LogWarning("Module {ModuleId} requires dependency {DependencyId} which is not loaded.", manifest.Id, dependencyId);
                return false;
            }

            if (!NuGetVersion.TryParse(dependencyModule.Descriptor.Version, out var dependencyVersion))
            {
                _logger.LogWarning("Module {ModuleId} dependency {DependencyId} has invalid version {DependencyVersion}.", manifest.Id, dependencyId, dependencyModule.Descriptor.Version);
                return false;
            }

            if (!VersionRange.TryParse(dependencyRange, out var range))
            {
                _logger.LogWarning("Module {ModuleId} dependency {DependencyId} has invalid version range {Range}.", manifest.Id, dependencyId, dependencyRange);
                return false;
            }

            if (!range.Satisfies(dependencyVersion))
            {
                _logger.LogWarning("Module {ModuleId} dependency {DependencyId} version {DependencyVersion} does not satisfy range {Range}.", manifest.Id, dependencyId, dependencyVersion, dependencyRange);
                return false;
            }
        }

        return true;
    }

    private IModule? CreateModuleInstance(Type moduleType)
    {
        if (_hostServices != null)
        {
            try
            {
                return (IModule)ActivatorUtilities.CreateInstance(_hostServices, moduleType)!;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to create module {ModuleType} from host services, falling back to parameterless construction.", moduleType.FullName);
            }
        }

        return (IModule)Activator.CreateInstance(moduleType)!;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static IReadOnlyCollection<Type> GetComponentDependencies(Type componentType)
    {
        var deps = new List<Type>();
        var dependsOnAttrs = componentType.GetCustomAttributes<DependsOnAttribute>();
        foreach (var attr in dependsOnAttrs)
        {
            deps.AddRange(attr.DependedModuleTypes);
        }
        return deps;
    }
}
