using System.IO;
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
    /// <param name="manifestPath">Path to extension.vsixmanifest or its containing directory.</param>
    /// <param name="hostType">Current host type for host-aware validation and menu projection.</param>
    Task RegisterDevelopmentModuleAsync(string manifestPath, string? hostType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a module from a .modpkg package file.
    /// Extracts the package, copies to user modules directory, and registers in database.
    /// </summary>
    /// <param name="packagePath">Path to the .modpkg file.</param>
    /// <param name="overwrite">Whether to overwrite existing installation without confirmation.</param>
    /// <param name="hostType">The current host type for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success, failure, or need for confirmation.</returns>
    Task<ModuleInstallResult> InstallFromPackageAsync(string packagePath, bool overwrite = false, string? hostType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a module from a .modpkg package stream.
    /// Saves to temp file, extracts, copies to user modules directory, and registers in database.
    /// </summary>
    /// <param name="packageStream">Stream containing the .modpkg file content.</param>
    /// <param name="fileName">Original file name (for validation).</param>
    /// <param name="overwrite">Whether to overwrite existing installation without confirmation.</param>
    /// <param name="hostType">The current host type for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success, failure, or need for confirmation.</returns>
    Task<ModuleInstallResult> InstallFromPackageStreamAsync(Stream packageStream, string fileName, bool overwrite = false, string? hostType = null, CancellationToken cancellationToken = default);
}

