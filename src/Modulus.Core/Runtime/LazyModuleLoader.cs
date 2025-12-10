using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Infrastructure.Data;

namespace Modulus.Core.Runtime;

/// <summary>
/// Lazy module loader that loads modules on-demand from their database-stored paths.
/// </summary>
public class LazyModuleLoader : ILazyModuleLoader
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IModuleLoader _moduleLoader;
    private readonly RuntimeContext _runtimeContext;
    private readonly ILogger<LazyModuleLoader> _logger;
    
    // Track modules that are currently being loaded to prevent duplicate loads
    private readonly ConcurrentDictionary<string, Task<bool>> _loadingTasks = new();
    // Track load results for quick checks
    private readonly ConcurrentDictionary<string, bool> _loadResults = new();

    public LazyModuleLoader(
        IServiceProvider serviceProvider,
        IModuleLoader moduleLoader,
        RuntimeContext runtimeContext,
        ILogger<LazyModuleLoader> logger)
    {
        _serviceProvider = serviceProvider;
        _moduleLoader = moduleLoader;
        _runtimeContext = runtimeContext;
        _logger = logger;
    }

    public bool IsModuleLoaded(string moduleId)
    {
        // Check if module is registered in runtime context
        return _runtimeContext.TryGetModule(moduleId, out _);
    }

    public async Task<bool> EnsureModuleLoadedAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        // Fast path: already loaded
        if (IsModuleLoaded(moduleId))
        {
            return true;
        }

        // Check if we already know this module failed to load
        if (_loadResults.TryGetValue(moduleId, out var cachedResult) && !cachedResult)
        {
            return false;
        }

        // Use GetOrAdd to ensure only one load task per module
        var loadTask = _loadingTasks.GetOrAdd(moduleId, async id =>
        {
            try
            {
                return await LoadModuleInternalAsync(id, cancellationToken);
            }
            finally
            {
                // Remove from loading tasks when done
                _loadingTasks.TryRemove(id, out _);
            }
        });

        return await loadTask;
    }

    private async Task<bool> LoadModuleInternalAsync(string moduleId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Lazy loading module {ModuleId}...", moduleId);

        try
        {
            // Get module info from database
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ModulusDbContext>();
            
            var moduleEntity = await dbContext.Modules
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken);

            if (moduleEntity == null)
            {
                _logger.LogWarning("Module {ModuleId} not found in database.", moduleId);
                _loadResults[moduleId] = false;
                return false;
            }

            // Check both IsEnabled flag and State for disabled status
            if (!moduleEntity.IsEnabled || moduleEntity.State == Infrastructure.Data.Models.ModuleState.Disabled)
            {
                _logger.LogDebug("Module {ModuleId} is disabled (IsEnabled={IsEnabled}, State={State}).", 
                    moduleId, moduleEntity.IsEnabled, moduleEntity.State);
                _loadResults[moduleId] = false;
                return false;
            }

            // Skip built-in host modules (they have no physical path)
            if (moduleEntity.Path == "built-in")
            {
                _logger.LogDebug("Module {ModuleId} is a built-in host module, no loading needed.", moduleId);
                _loadResults[moduleId] = true;
                return true;
            }

            // Resolve module package path
            var packagePath = ResolveModulePackagePath(moduleEntity.Path);
            if (packagePath == null)
            {
                _logger.LogWarning("Module {ModuleId} package path not found: {Path}", moduleId, moduleEntity.Path);
                _loadResults[moduleId] = false;
                return false;
            }
            
            _logger.LogDebug("Resolved module {ModuleId} path: {Path}", moduleId, packagePath);

            // Load the module
            var descriptor = await _moduleLoader.LoadAsync(
                packagePath, 
                moduleEntity.IsSystem, 
                skipModuleInitialization: false, 
                cancellationToken);

            if (descriptor == null)
            {
                _logger.LogWarning("Module {ModuleId} failed to load from {Path}.", moduleId, packagePath);
                _loadResults[moduleId] = false;
                return false;
            }

            _logger.LogInformation("Module {ModuleId} loaded successfully.", moduleId);
            _loadResults[moduleId] = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading module {ModuleId}.", moduleId);
            _loadResults[moduleId] = false;
            return false;
        }
    }

    /// <summary>
    /// Resolves the module package path from various possible locations.
    /// </summary>
    private string? ResolveModulePackagePath(string storedPath)
    {
        // Try multiple resolution strategies
        var candidates = new List<string>();
        
        // 1. Direct path (might be full path to manifest)
        if (Path.IsPathRooted(storedPath))
        {
            var dir = File.Exists(storedPath) ? Path.GetDirectoryName(storedPath) : storedPath;
            if (dir != null) candidates.Add(dir);
        }
        else
        {
            var baseDir = AppContext.BaseDirectory;
            
            // 2. Relative to app base directory (e.g., "Modules/EchoPlugin")
            candidates.Add(Path.GetFullPath(Path.Combine(baseDir, storedPath)));
            
            // 3. If storedPath is "Modules/X", try just the module name under "Modules/"
            if (storedPath.StartsWith("Modules/") || storedPath.StartsWith("Modules\\"))
            {
                var moduleName = Path.GetFileName(storedPath);
                candidates.Add(Path.GetFullPath(Path.Combine(baseDir, "Modules", moduleName)));
            }
            
            // 4. Try artifacts structure (for development)
            candidates.Add(Path.GetFullPath(Path.Combine(baseDir, "..", "Modules", Path.GetFileName(storedPath))));
            
            // 5. Try relative to current directory
            candidates.Add(Path.GetFullPath(storedPath));
        }
        
        // Find the first valid directory with DLLs
        foreach (var candidate in candidates.Distinct())
        {
            if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.dll").Length > 0)
            {
                _logger.LogDebug("Resolved module path {StoredPath} to {ResolvedPath}", storedPath, candidate);
                return candidate;
            }
        }
        
        _logger.LogWarning("Could not resolve module path: {StoredPath}. Tried: {Candidates}", 
            storedPath, string.Join(", ", candidates));
        return null;
    }

    public async Task<string?> GetModuleIdForNavigationKeyAsync(string navigationKey, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ModulusDbContext>();

        // Find the menu with this route and return its module ID
        var menu = await dbContext.Menus
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Route == navigationKey, cancellationToken);

        return menu?.ModuleId;
    }

    public async Task<bool> EnsureModuleLoadedForNavigationAsync(string navigationKey, CancellationToken cancellationToken = default)
    {
        var moduleId = await GetModuleIdForNavigationKeyAsync(navigationKey, cancellationToken);
        
        if (moduleId == null)
        {
            _logger.LogDebug("No module found for navigation key: {NavigationKey}", navigationKey);
            return false;
        }

        return await EnsureModuleLoadedAsync(moduleId, cancellationToken);
    }
}

