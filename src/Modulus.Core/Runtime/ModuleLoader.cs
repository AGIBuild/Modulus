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
}

public sealed class ModuleLoader : IModuleLoader, IHostAwareModuleLoader
{
    private readonly RuntimeContext _runtimeContext;
    private readonly IManifestValidator _manifestValidator;
    private readonly ILogger<ModuleLoader> _logger;
    private readonly ISharedAssemblyCatalog _sharedAssemblyCatalog;
    private readonly ModuleMetadataScanner _metadataScanner;
    private IServiceProvider? _hostServices;

    public ModuleLoader(RuntimeContext runtimeContext, IManifestValidator manifestValidator, ISharedAssemblyCatalog sharedAssemblyCatalog, ILogger<ModuleLoader> logger, IServiceProvider? hostServices = null)
    {
        _runtimeContext = runtimeContext;
        _manifestValidator = manifestValidator;
        _sharedAssemblyCatalog = sharedAssemblyCatalog;
        _logger = logger;
        _metadataScanner = new ModuleMetadataScanner(logger);
        _hostServices = hostServices;
    }

    public void BindHostServices(IServiceProvider hostServices)
    {
        _hostServices = hostServices;
    }

    public async Task<ModuleDescriptor?> LoadAsync(string packagePath, bool isSystem = false, CancellationToken cancellationToken = default)
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

        var moduleTypes = loadedAssemblies
            .SelectMany(SafeGetTypes)
            .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .ToList();

        var moduleRegistrations = new List<ModuleRegistration>();
        foreach (var moduleType in moduleTypes)
        {
            try
            {
                var moduleInstance = CreateModuleInstance(moduleType);
                if (moduleInstance == null)
                {
                    continue;
                }

                var moduleId = ResolveModuleId(moduleType, manifest.Id);
                var dependencies = new HashSet<string>(manifest.Dependencies.Keys, StringComparer.OrdinalIgnoreCase);
                foreach (var attr in moduleType.GetCustomAttributes<DependsOnAttribute>())
                {
                    foreach (var depType in attr.DependedModuleTypes)
                    {
                        var depId = ResolveModuleId(depType, depType.FullName ?? depType.Name);
                        dependencies.Add(depId);
                    }
                }

                moduleRegistrations.Add(new ModuleRegistration(moduleInstance, moduleId, dependencies));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to instantiate module {ModuleType}", moduleType.FullName);
                return null;
            }
        }

        var moduleIdSet = new HashSet<string>(moduleRegistrations.Select(r => r.ModuleId), StringComparer.OrdinalIgnoreCase);
        foreach (var registration in moduleRegistrations)
        {
            registration.Dependencies.RemoveWhere(dep => !moduleIdSet.Contains(dep));
        }

        var sortedModules = ModuleDependencyResolver.TopologicallySort(
            moduleRegistrations,
            r => r.ModuleId,
            r => r.Dependencies,
            _logger).Select(r => r.Instance).ToList();

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

        var initContext = new ModuleInitializationContext(compositeProvider);

        var initialized = false;
        try
        {
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

            var registeredMenus = RegisterMenus(sortedModules, compositeProvider, currentHostId, manifest.Id).ToList();

            var runtimeModule = new RuntimeModule(descriptor, alc, packagePath, manifest, isSystem)
            {
                State = ModuleState.Active
            };

            _runtimeContext.RegisterModule(runtimeModule);
            var handle = new RuntimeModuleHandle(runtimeModule, manifest, moduleScope, moduleProvider, compositeProvider, sortedModules, registeredMenus, loadedAssemblies);
            _runtimeContext.RegisterModuleHandle(handle);
            initialized = true;

            _logger.LogInformation("Module {ModuleId} (v{Version}) loaded from {PackagePath} for host {HostType} (System: {IsSystem}).", manifest.Id, manifest.Version, packagePath, currentHostId ?? "None", isSystem);

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
            // Let's modify ReloadAsync signature or just pass false for now.
            // Wait, we can't change signature easily as it's an interface.
            // But we can assume false for ReloadAsync as user-initiated reload is usually for dev/user modules.
            // If we want to support system module reload, we should probably pass isSystem=true if it was system.
            // But we unloaded it, so we lost that info unless we kept it.
            // Let's default to false for now to fix the build error.
            return await LoadAsync(packagePath, isSystem, cancellationToken).ConfigureAwait(false);
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

    private static string ResolveModuleId(Type moduleType, string fallback)
    {
        var moduleAttribute = moduleType.GetCustomAttribute<ModuleAttribute>();
        if (moduleAttribute != null && !string.IsNullOrWhiteSpace(moduleAttribute.Id))
        {
            return moduleAttribute.Id;
        }

        return fallback;
    }

    private IEnumerable<MenuItem> RegisterMenus(IReadOnlyCollection<IModule> modules, IServiceProvider serviceProvider, string? hostType, string fallbackModuleId)
    {
        var menuRegistry = serviceProvider.GetService<IMenuRegistry>();
        if (menuRegistry == null || string.IsNullOrEmpty(hostType))
        {
            return Array.Empty<MenuItem>();
        }

        var menus = new List<MenuItem>();

        foreach (var module in modules)
        {
            var moduleType = module.GetType();
            List<ModuleMenuMetadata> menuMetadata;

            if (hostType == HostType.Avalonia)
            {
                menuMetadata = _metadataScanner.ScanAvaloniaMenus(moduleType);
            }
            else if (hostType == HostType.Blazor)
            {
                menuMetadata = _metadataScanner.ScanBlazorMenus(moduleType);
            }
            else
            {
                continue;
            }

            foreach (var menu in menuMetadata)
            {
                var navigationKey = !string.IsNullOrEmpty(menu.Route) ? menu.Route : menu.ViewModelType ?? menu.Id;
                var item = new MenuItem($"{moduleType.Name}.{menu.Id}", menu.DisplayName, menu.Icon, navigationKey, menu.Location, menu.Order);
                var moduleId = ResolveModuleId(moduleType, fallbackModuleId);
                item.ModuleId = moduleId;
                menuRegistry.Register(item);
                menus.Add(item);
            }
        }

        return menus;
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

    private sealed record ModuleRegistration(IModule Instance, string ModuleId, HashSet<string> Dependencies);
}
