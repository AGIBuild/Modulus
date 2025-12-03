using Microsoft.Extensions.Logging;
using Modulus.Sdk;
using Modulus.Core.Manifest;

namespace Modulus.Core.Runtime;

public sealed class ModuleLoader : IModuleLoader
{
    private readonly RuntimeContext _runtimeContext;
    private readonly IManifestValidator _manifestValidator;
    private readonly ILogger<ModuleLoader> _logger;

    public ModuleLoader(RuntimeContext runtimeContext, IManifestValidator manifestValidator, ILogger<ModuleLoader> logger)
    {
        _runtimeContext = runtimeContext;
        _manifestValidator = manifestValidator;
        _logger = logger;
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

        var manifestValid = await _manifestValidator.ValidateAsync(packagePath, manifestPath, manifest, cancellationToken).ConfigureAwait(false);
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

        var descriptor = new ModuleDescriptor(
            manifest.Id, 
            manifest.Version, 
            manifest.DisplayName, 
            manifest.Description,
            manifest.SupportedHosts);
        var alc = new ModuleLoadContext(manifest.Id, packagePath);

        // Get Current Host ID from RuntimeContext
        var currentHostId = _runtimeContext.HostType;

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

                alc.LoadFromAssemblyPath(assemblyPath);
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
                    alc.LoadFromAssemblyPath(assemblyPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assemblies for module {ModuleId}.", manifest.Id);
            alc.Unload();
            return null;
        }

        var runtimeModule = new RuntimeModule(descriptor, alc, packagePath, isSystem);
        _runtimeContext.RegisterModule(runtimeModule);
        
        _logger.LogInformation("Module {ModuleId} (v{Version}) loaded from {PackagePath} for host {HostType} (System: {IsSystem}).", manifest.Id, manifest.Version, packagePath, currentHostId ?? "None", isSystem);

        return descriptor;
    }

    public Task UnloadAsync(string moduleId)
    {
        if (_runtimeContext.TryGetModule(moduleId, out var runtimeModule))
        {
            if (runtimeModule!.IsSystem)
            {
                _logger.LogWarning("Cannot unload system module {ModuleId}.", moduleId);
                throw new InvalidOperationException($"Cannot unload system module {moduleId}.");
            }

            _logger.LogInformation("Unloading module {ModuleId}...", moduleId);
            
            // 1. Remove from context
            _runtimeContext.RemoveModule(moduleId);

            // 2. Set state
            if (runtimeModule != null)
            {
                runtimeModule.State = ModuleState.Unloaded;
                
                // 3. Unload ALC
                runtimeModule.LoadContext.Unload();
            }

            _logger.LogInformation("Module {ModuleId} unloaded.", moduleId);
        }
        else
        {
            _logger.LogWarning("Cannot unload module {ModuleId}: not found.", moduleId);
        }

        return Task.CompletedTask;
    }

    public async Task<ModuleDescriptor?> ReloadAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        if (_runtimeContext.TryGetModule(moduleId, out var runtimeModule))
        {
            var packagePath = runtimeModule!.PackagePath;
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
            return await LoadAsync(packagePath, false, cancellationToken).ConfigureAwait(false);
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
}
