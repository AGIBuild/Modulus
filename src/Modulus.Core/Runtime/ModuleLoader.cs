using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Modulus.Core;
using Modulus.Core.Architecture;
using Modulus.Core.Installation;
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
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISharedAssemblyCatalog _sharedAssemblyCatalog;
    private readonly ISharedAssemblyResolutionReporter? _resolutionReporter;
    private readonly IModuleExecutionGuard _executionGuard;
    private IServiceProvider? _hostServices;

    public ModuleLoader(
        RuntimeContext runtimeContext,
        IManifestValidator manifestValidator,
        ISharedAssemblyCatalog sharedAssemblyCatalog,
        IModuleExecutionGuard executionGuard,
        ILogger<ModuleLoader> logger,
        ILoggerFactory loggerFactory,
        IServiceProvider? hostServices = null,
        ISharedAssemblyResolutionReporter? resolutionReporter = null)
    {
        _runtimeContext = runtimeContext;
        _manifestValidator = manifestValidator;
        _sharedAssemblyCatalog = sharedAssemblyCatalog;
        _executionGuard = executionGuard;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _hostServices = hostServices;
        _resolutionReporter = resolutionReporter;
    }

    public void BindHostServices(IServiceProvider hostServices)
    {
        _logger.LogDebug("BindHostServices called. Updating {Count} module handles...", _runtimeContext.ModuleHandles.Count);
        _hostServices = hostServices;
        
        // Update all existing module handles with the new host services
        foreach (var handle in _runtimeContext.ModuleHandles)
        {
            _logger.LogDebug("  Binding host services to module {ModuleName} ({ModuleId})", 
                handle.RuntimeModule.Descriptor.DisplayName, handle.RuntimeModule.Descriptor.Id);
            handle.UpdateCompositeServiceProvider(hostServices);
        }
    }

    /// <summary>
    /// Initializes all pre-loaded modules that were loaded with skipModuleInitialization=true.
    /// This should be called after BindHostServices.
    /// </summary>
    public async Task InitializeLoadedModulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("InitializeLoadedModulesAsync called. HostServices bound: {Bound}", _hostServices != null);
        
        if (_hostServices == null)
        {
            _logger.LogWarning("Cannot initialize modules: host services not bound.");
            return;
        }

        _logger.LogDebug("Found {Count} module handles to initialize.", _runtimeContext.ModuleHandles.Count);
        
        IReadOnlyList<RuntimeModuleHandle> sortedHandles;
        try
        {
            sortedHandles = RuntimeDependencyGraph.TopologicallySort(_runtimeContext.ModuleHandles, _logger);
        }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to build runtime dependency graph. Module initialization aborted.");
                    foreach (var handle in _runtimeContext.ModuleHandles)
                    {
                        handle.RuntimeModule.TransitionTo(ModuleState.Error, "Dependency graph build failed", ex);
                    }
                    return;
                }

        foreach (var handle in sortedHandles)
        {
            var module = handle.RuntimeModule;
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["HostType"] = _runtimeContext.HostType ?? "unknown",
                ["ModuleId"] = module.Descriptor.Id,
                ["ModuleName"] = module.Descriptor.DisplayName,
                ["ModuleVersion"] = module.Descriptor.Version
            }))
            {
                _logger.LogDebug("Checking module {ModuleName} ({ModuleId}), State={State}", module.Descriptor.DisplayName, module.Descriptor.Id, module.State);
                if (module.State != ModuleState.Loaded) continue; // Skip already initialized or errored modules

                _logger.LogDebug("Initializing pre-loaded module {ModuleName} ({ModuleId}) with {InstanceCount} instances...", 
                    module.Descriptor.DisplayName, module.Descriptor.Id, handle.ModuleInstances.Count);

                handle.UpdateCompositeServiceProvider(_hostServices);
                var initContext = new ModuleInitializationContext(handle.CompositeServiceProvider);

                var initSuccess = true;
                foreach (var moduleInstance in handle.ModuleInstances)
                {
                    _logger.LogDebug("  Calling OnApplicationInitializationAsync on {Type}...", moduleInstance.GetType().Name);
                    
                    // Use execution guard for exception isolation
                    var success = await _executionGuard.ExecuteSafeAsync(
                        module.Descriptor.Id,
                        async () =>
                        {
                            await moduleInstance.OnApplicationInitializationAsync(initContext, cancellationToken).ConfigureAwait(false);
                            return true;
                        },
                        fallback: false,
                        caller: $"Initialize:{moduleInstance.GetType().Name}");
                    
                    if (!success)
                    {
                        initSuccess = false;
                        break;
                    }
                }
                
                if (initSuccess)
                {
                    module.TransitionTo(ModuleState.Active, "Host binding initialization completed");
                    _logger.LogInformation("Module {ModuleName} ({ModuleId}) activated.", module.Descriptor.DisplayName, module.Descriptor.Id);
                }
                else
                {
                    var healthInfo = _executionGuard.GetHealthInfo(module.Descriptor.Id);
                    module.TransitionTo(ModuleState.Error, "Initialization failed", healthInfo.LastError);
                }
            }
        }
    }

    public async Task<ModuleDescriptor?> LoadAsync(string packagePath, bool isSystem = false, bool skipModuleInitialization = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var hostScope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["HostType"] = _runtimeContext.HostType ?? "unknown"
        });

        if (!Directory.Exists(packagePath))
        {
            _logger.LogWarning("Module package path {Path} does not exist.", packagePath);
            return null;
        }

        var manifestPath = Path.Combine(packagePath, SystemModuleInstaller.VsixManifestFileName);
        var manifest = await VsixManifestReader.ReadFromFileAsync(manifestPath).ConfigureAwait(false);
        if (manifest is null)
        {
            _logger.LogWarning("Failed to read manifest at {ManifestPath}.", manifestPath);
            return null;
        }

        var identity = manifest.Metadata.Identity;
        var hostType = _runtimeContext.HostType;
        if (string.IsNullOrWhiteSpace(hostType))
        {
            _logger.LogWarning("Host type is not set in RuntimeContext. Set host before loading modules.");
            return null;
        }

        var validationResult = await _manifestValidator.ValidateAsync(packagePath, manifestPath, manifest, hostType, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                _logger.LogWarning("Manifest validation error for {ManifestPath}: {Error}", manifestPath, error);
            }
            return null;
        }

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["HostType"] = hostType,
            ["ModuleId"] = identity.Id,
            ["ModuleName"] = manifest.Metadata.DisplayName,
            ["ModuleVersion"] = identity.Version
        });

        _logger.LogInformation("Loaded manifest for module {ModuleName} ({ModuleId}) v{ModuleVersion}.", manifest.Metadata.DisplayName, identity.Id, identity.Version);
        _logger.LogInformation("Validating manifest for host {HostType}.", hostType);

        // Check if already loaded
        if (_runtimeContext.TryGetModule(identity.Id, out var existingModule))
        {
            _logger.LogWarning("Module {ModuleId} is already loaded.", identity.Id);
            return existingModule!.Descriptor;
        }

        // Allow explicit reload after unload by resetting execution health.
        // Unload marks the module as Unloaded in the execution guard; a subsequent Load/Reload is an explicit recovery attempt.
        _executionGuard.ResetHealth(identity.Id);

        if (!NuGetVersion.TryParse(identity.Version, out _))
        {
            _logger.LogWarning("Module {ModuleId} version {Version} is not a valid semantic version.", identity.Id, identity.Version);
            return null;
        }

        if (!EnsureDependenciesSatisfied(manifest))
        {
            return null;
        }

        // Get supported hosts from Installation targets
        var supportedHosts = manifest.Installation.Select(t => t.Id).ToList();
        
        var descriptor = new ModuleDescriptor(
            identity.Id, 
            identity.Version, 
            manifest.Metadata.DisplayName, 
            manifest.Metadata.Description,
            supportedHosts);
        var alc = new ModuleLoadContext(identity.Id, packagePath, _sharedAssemblyCatalog, _loggerFactory.CreateLogger<ModuleLoadContext>());
        var loadedAssemblies = new List<Assembly>();

        try
        {
            // Load assemblies from Assets based on type
            var packageAssets = manifest.Assets
                .Where(a => string.Equals(a.Type, ModulusAssetTypes.Package, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(a.Type, ModulusAssetTypes.Assembly, StringComparison.OrdinalIgnoreCase))
                .Where(a => string.IsNullOrEmpty(a.TargetHost) || 
                            ModulusHostIds.Matches(a.TargetHost, hostType))
                .ToList();

            foreach (var asset in packageAssets)
            {
                if (string.IsNullOrEmpty(asset.Path)) continue;
                
                var assemblyPath = Path.Combine(packagePath, asset.Path);
                if (!File.Exists(assemblyPath))
                {
                    _logger.LogWarning("Assembly {AssemblyPath} not found for module {ModuleId}.", assemblyPath, identity.Id);
                    continue;
                }

                var assembly = alc.LoadFromAssemblyPath(assemblyPath);
                if (assembly != null)
                {
                    loadedAssemblies.Add(assembly);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assemblies for module {ModuleId}.", identity.Id);
            alc.Unload();
            return null;
        }

        if (loadedAssemblies.Count == 0 && alc.Assemblies.Any())
        {
            loadedAssemblies.AddRange(alc.Assemblies);
        }

        // Hard convention: all module ViewModels MUST inherit ViewModelBase.
        // This is enforced at module load time so both Avalonia and Blazor hosts are covered consistently.
        ViewModelConventionsEnforcer.Enforce(identity.Id, loadedAssemblies, _logger);

        var componentTypes = loadedAssemblies
            .SelectMany(SafeGetTypes)
            // Only ModulusPackage entry points are supported (no legacy ModulusComponent-only entry points).
            .Where(t => typeof(ModulusPackage).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .ToList();

        IReadOnlyList<Type> sortedTypes;
        try
        {
            sortedTypes = ComponentDependencyResolver.TopologicallySort(componentTypes, _logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve component dependencies for module {ModuleId}.", identity.Id);
            return null;
        }
        _logger.LogInformation("Resolved {Count} module components after dependency graph build.", sortedTypes.Count);

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
        services.TryAddSingleton(_loggerFactory);
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
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

        EnforceHostLogging(services, descriptor);

        var moduleProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var moduleScope = moduleProvider.CreateScope();
        var scopedProvider = moduleScope.ServiceProvider;
        var compositeProvider = _hostServices != null
            ? new CompositeServiceProvider(scopedProvider, _hostServices)
            : scopedProvider;

        var registeredMenus = new List<MenuItem>();
        var runtimeModule = new RuntimeModule(descriptor, alc, packagePath, manifest, isSystem);

        var initialized = false;
        try
        {
            // Only run module initialization if not skipped (host services must be bound first)
            if (!skipModuleInitialization)
            {
                var initContext = new ModuleInitializationContext(compositeProvider);
                var initSuccess = true;
                
                foreach (var module in sortedModules)
                {
                    // Use execution guard for exception isolation
                    var success = await _executionGuard.ExecuteSafeAsync(
                        identity.Id,
                        async () =>
                        {
                            await module.OnApplicationInitializationAsync(initContext, cancellationToken).ConfigureAwait(false);
                            return true;
                        },
                        fallback: false,
                        caller: $"Initialize:{module.GetType().Name}");
                    
                    if (!success)
                    {
                        initSuccess = false;
                        _logger.LogError("Module {ModuleType} initialization failed, aborting load", module.GetType().Name);
                        break;
                    }
                }
                
                if (!initSuccess)
                {
                    return null;
                }
                
                runtimeModule.TransitionTo(ModuleState.Active, "Module initialization completed");
            }

            _runtimeContext.RegisterModule(runtimeModule);
            var handle = new RuntimeModuleHandle(runtimeModule, manifest, moduleScope, moduleProvider, compositeProvider, sortedModules, registeredMenus, loadedAssemblies);
            _runtimeContext.RegisterModuleHandle(handle);
            initialized = true;

            if (skipModuleInitialization)
            {
                _logger.LogDebug("Module {ModuleName} ({ModuleId}) v{Version} loaded (pending activation).",
                    descriptor.DisplayName, identity.Id, identity.Version);
            }
            else
            {
                _logger.LogInformation("Module {ModuleName} ({ModuleId}) v{Version} loaded and activated.",
                    descriptor.DisplayName, identity.Id, identity.Version);
            }

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
            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["HostType"] = _runtimeContext.HostType ?? "unknown",
                ["ModuleId"] = runtimeModule!.Descriptor.Id,
                ["ModuleName"] = runtimeModule.Descriptor.DisplayName,
                ["ModuleVersion"] = runtimeModule.Descriptor.Version
            });

            if (runtimeModule!.IsSystem)
            {
                _logger.LogWarning("Cannot unload system module {ModuleId}.", moduleId);
                throw new InvalidOperationException($"Cannot unload system module {moduleId}.");
            }

            _logger.LogDebug("Unloading module {ModuleName} ({ModuleId})...", runtimeModule.Descriptor.DisplayName, moduleId);

            RuntimeModuleHandle? handle = null;
            if (_runtimeContext.TryGetModuleHandle(moduleId, out var storedHandle))
            {
                handle = storedHandle;
            }

            if (handle != null)
            {
                // 1. Invoke module shutdown hooks (reverse order) with exception isolation
                _logger.LogDebug("Invoking shutdown hooks for {Count} module instances...", handle.ModuleInstances.Count);
                var shutdownContext = new ModuleInitializationContext(handle.CompositeServiceProvider);
                var moduleInstances = handle.ModuleInstances.ToList();
                for (var i = moduleInstances.Count - 1; i >= 0; i--)
                {
                    var instance = moduleInstances[i];
                    await _executionGuard.ExecuteSafeAsync(
                        moduleId,
                        () => instance.OnApplicationShutdownAsync(shutdownContext),
                        caller: $"Shutdown:{instance.GetType().Name}");
                }

                // 2. Unregister menus
                _logger.LogDebug("Unregistering menu items...");
                if (_hostServices?.GetService<IMenuRegistry>() is IMenuRegistry menuRegistry)
                {
                    menuRegistry.UnregisterModuleItems(moduleId);
                }
                else if (handle.CompositeServiceProvider.GetService<IMenuRegistry>() is IMenuRegistry compositeRegistry)
                {
                    compositeRegistry.UnregisterModuleItems(moduleId);
                }

                // 3. Clear navigation cache for this module
                _logger.LogDebug("Clearing navigation cache...");
                if (_hostServices?.GetService<INavigationService>() is INavigationService navigationService)
                {
                    navigationService.ClearModuleCache(moduleId);
                }
                else if (handle.CompositeServiceProvider.GetService<INavigationService>() is INavigationService compositeNavService)
                {
                    compositeNavService.ClearModuleCache(moduleId);
                }

                // 4. Dispose scoped resources
                _logger.LogDebug("Disposing scoped resources...");
                await handle.DisposeAsync().ConfigureAwait(false);
                _runtimeContext.RemoveModuleHandle(moduleId);
            }
            else
            {
                // Fallback: try to clean up menus and navigation if handle is missing (e.g. startup modules)
                if (_hostServices?.GetService<IMenuRegistry>() is IMenuRegistry menuRegistry)
                {
                    menuRegistry.UnregisterModuleItems(moduleId);
                }
                if (_hostServices?.GetService<INavigationService>() is INavigationService navigationService)
                {
                    navigationService.ClearModuleCache(moduleId);
                }
            }

            _runtimeContext.RemoveModule(moduleId);

            runtimeModule.TransitionTo(ModuleState.Unloaded, "Module unloaded and context disposed");
            
            // Unload AssemblyLoadContext and allow GC to reclaim resources
            var loadContextRef = new WeakReference(runtimeModule.LoadContext);
            runtimeModule.LoadContext.Unload();

            // Force GC to reclaim the unloaded assemblies
            // This helps release file locks on DLLs faster
            for (int i = 0; i < 3 && loadContextRef.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                if (loadContextRef.IsAlive)
                {
                    await Task.Delay(100 * (i + 1)); // 100ms, 200ms, 300ms
                }
            }

            if (loadContextRef.IsAlive)
            {
                _logger.LogDebug("AssemblyLoadContext for module {ModuleId} still alive after GC. File locks may persist.", moduleId);
            }
            else
            {
                _logger.LogDebug("AssemblyLoadContext for module {ModuleId} successfully collected.", moduleId);
            }

            // Notify execution guard that module is unloaded
            if (_executionGuard is ModuleExecutionGuard guard)
            {
                guard.OnModuleUnloaded(moduleId);
            }

            _logger.LogInformation("Module {ModuleName} ({ModuleId}) unloaded.", runtimeModule.Descriptor.DisplayName, moduleId);
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
            using var scope = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["HostType"] = _runtimeContext.HostType ?? "unknown",
                ["ModuleId"] = runtimeModule.Descriptor.Id,
                ["ModuleName"] = runtimeModule.Descriptor.DisplayName,
                ["ModuleVersion"] = runtimeModule.Descriptor.Version
            });
            await UnloadAsync(moduleId);
            
            return await LoadAsync(packagePath, isSystem, skipModuleInitialization: false, cancellationToken).ConfigureAwait(false);
        }
        
        _logger.LogWarning("Cannot reload module {ModuleId}: not found.", moduleId);
        return null;
    }

    public async Task<ModuleDescriptor?> GetDescriptorAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(packagePath, SystemModuleInstaller.VsixManifestFileName);
        var manifest = await VsixManifestReader.ReadFromFileAsync(manifestPath).ConfigureAwait(false);
        if (manifest is null)
        {
            return null;
        }
        var identity = manifest.Metadata.Identity;
        var supportedHosts = manifest.Installation.Select(t => t.Id).ToList();
        return new ModuleDescriptor(
            identity.Id, 
            identity.Version, 
            manifest.Metadata.DisplayName,
            manifest.Metadata.Description,
            supportedHosts);
    }

    private bool EnsureDependenciesSatisfied(VsixManifest manifest)
    {
        var identity = manifest.Metadata.Identity;
        foreach (var dep in manifest.Dependencies)
        {
            if (!_runtimeContext.TryGetModule(dep.Id, out var dependencyModule) || dependencyModule == null)
            {
                _logger.LogWarning("Module {ModuleId} requires dependency {DependencyId} which is not loaded.", identity.Id, dep.Id);
                return false;
            }

            if (!NuGetVersion.TryParse(dependencyModule.Descriptor.Version, out var dependencyVersion))
            {
                _logger.LogWarning("Module {ModuleId} dependency {DependencyId} has invalid version {DependencyVersion}.", identity.Id, dep.Id, dependencyModule.Descriptor.Version);
                return false;
            }

            if (!VersionRange.TryParse(dep.Version, out var range))
            {
                _logger.LogWarning("Module {ModuleId} dependency {DependencyId} has invalid version range {Range}.", identity.Id, dep.Id, dep.Version);
                return false;
            }

            if (!range.Satisfies(dependencyVersion))
            {
                _logger.LogWarning("Module {ModuleId} dependency {DependencyId} version {DependencyVersion} does not satisfy range {Range}.", identity.Id, dep.Id, dependencyVersion, dep.Version);
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

    private void EnforceHostLogging(IServiceCollection services, ModuleDescriptor descriptor)
    {
        var providerDescriptors = services.Where(d => typeof(ILoggerProvider).IsAssignableFrom(d.ServiceType)).ToList();
        if (providerDescriptors.Count > 0)
        {
            foreach (var descriptorToRemove in providerDescriptors)
            {
                services.Remove(descriptorToRemove);
            }

            _logger.LogWarning("Module {ModuleName} attempted to register logging providers; ignoring to keep host pipeline.", descriptor.DisplayName);
        }

        services.Replace(ServiceDescriptor.Singleton<ILoggerFactory>(_loggerFactory));
        services.Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
    }
}
