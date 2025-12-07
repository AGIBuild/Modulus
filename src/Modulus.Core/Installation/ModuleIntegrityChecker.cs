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
            
            // Resolve absolute path
            // module.Path is relative to App Root (or CWD)
            var absolutePath = Path.GetFullPath(module.Path);
            
            if (!File.Exists(absolutePath))
            {
                _logger.LogWarning("Integrity Check Failed: Module {ModuleId} manifest missing at {Path}. Marking as MissingFiles.", module.Id, absolutePath);
                
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

