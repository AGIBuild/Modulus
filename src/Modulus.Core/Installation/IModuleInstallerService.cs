using System.Threading;
using System.Threading.Tasks;

namespace Modulus.Core.Installation;

public interface IModuleInstallerService
{
    /// <summary>
    /// Installs or updates a module from a given package directory.
    /// Scans metadata and persists to database.
    /// </summary>
    /// <param name="packagePath">The full path to the module directory.</param>
    /// <param name="isSystem">Whether this is a system module.</param>
    /// <param name="hostType">The current host type (e.g., "AvaloniaApp", "BlazorApp"). Only loads UI assemblies for this host.</param>
    Task InstallFromPathAsync(string packagePath, bool isSystem = false, string? hostType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a development module that exists on disk but is not in the database.
    /// </summary>
    Task RegisterDevelopmentModuleAsync(string manifestPath, CancellationToken cancellationToken = default);
}

