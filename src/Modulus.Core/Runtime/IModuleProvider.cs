using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Modulus.Core.Runtime;

/// <summary>
/// Strategy for discovering module packages.
/// </summary>
public interface IModuleProvider
{
    /// <summary>
    /// Gets a list of paths to module packages (directories containing manifest.json).
    /// </summary>
    Task<IEnumerable<string>> GetModulePackagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether modules provided by this provider are system modules (cannot be unloaded).
    /// </summary>
    bool IsSystemSource { get; }
}


