using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;

namespace Modulus.Core.Installation;

public class ModuleIntegrityChecker
{
    private readonly IModuleRepository _moduleRepository;
    private readonly ILogger<ModuleIntegrityChecker> _logger;

    public ModuleIntegrityChecker(IModuleRepository moduleRepository, ILogger<ModuleIntegrityChecker> logger)
    {
        _moduleRepository = moduleRepository;
        _logger = logger;
    }

    /// <summary>
    /// Checks all enabled modules for file existence.
    /// Updates state to MissingFiles if manifest is gone.
    /// </summary>
    public async Task CheckAsync(CancellationToken cancellationToken = default)
    {
        var modules = await _moduleRepository.GetEnabledModulesAsync(cancellationToken);
        
        foreach (var module in modules)
        {
            // Skip Host modules (Path = "built-in") - they don't have manifest files
            if (module.Path == "built-in")
            {
                continue;
            }
            
            // Resolve absolute path relative to application base directory
            // module.Path can be either manifest file path or module directory path
            var basePath = Path.IsPathRooted(module.Path) 
                ? module.Path 
                : Path.Combine(AppContext.BaseDirectory, module.Path);
            var absolutePath = Path.GetFullPath(basePath);
            
            // Determine manifest path - if path doesn't end with manifest filename, append it
            var manifestPath = absolutePath.EndsWith(SystemModuleInstaller.VsixManifestFileName, StringComparison.OrdinalIgnoreCase)
                ? absolutePath
                : Path.Combine(absolutePath, SystemModuleInstaller.VsixManifestFileName);
            
            if (!File.Exists(manifestPath))
            {
                _logger.LogWarning(
                    "Integrity Check Failed: Module {ModuleId} ({DisplayName}) - manifest not found. " +
                    "Expected: {ManifestPath}. Stored path: {StoredPath}. Marking as MissingFiles.",
                    module.Id, module.DisplayName, manifestPath, module.Path);
                
                await _moduleRepository.UpdateStateAsync(module.Id, ModuleState.MissingFiles, cancellationToken);
            }
            else if (module.State == ModuleState.MissingFiles)
            {
                // Auto-recover if files reappear
                _logger.LogInformation("Integrity Check: Module {ModuleId} files restored. Marking as Ready.", module.Id);
                await _moduleRepository.UpdateStateAsync(module.Id, ModuleState.Ready, cancellationToken);
            }
        }
    }
}

