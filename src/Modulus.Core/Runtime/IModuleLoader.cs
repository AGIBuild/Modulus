using System.Threading;
using System.Threading.Tasks;

namespace Modulus.Core.Runtime;

public interface IModuleLoader
{
    /// <summary>
    /// Loads a module from a package on disk.
    /// </summary>
    /// <param name="packagePath">Path to the module package.</param>
    /// <param name="isSystem">Whether this module is considered a system module.</param>
    /// <param name="skipModuleInitialization">If true, modules are loaded but not initialized. Call IHostAwareModuleLoader.InitializeLoadedModulesAsync after host services are bound.</param>
    /// <param name="cancellationToken"></param>
    Task<ModuleDescriptor?> LoadAsync(string packagePath, bool isSystem = false, bool skipModuleInitialization = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a module by its ID.
    /// </summary>
    Task UnloadAsync(string moduleId);

    /// <summary>
    /// Reloads a module from its original package path.
    /// </summary>
    Task<ModuleDescriptor?> ReloadAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads module descriptor from a package path without loading the module.
    /// </summary>
    Task<ModuleDescriptor?> GetDescriptorAsync(string packagePath, CancellationToken cancellationToken = default);
}
