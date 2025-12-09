using System.Threading;
using System.Threading.Tasks;
using Modulus.Sdk;

namespace Modulus.Core.Manifest;

public interface IManifestValidator
{
    /// <summary>
    /// Validates a VSIX manifest (XML format) and returns a structured result with any errors.
    /// </summary>
    /// <param name="packagePath">The directory containing the module package.</param>
    /// <param name="manifestPath">The path to the extension.vsixmanifest file.</param>
    /// <param name="manifest">The parsed VSIX manifest.</param>
    /// <param name="hostType">The target host type (e.g., "Modulus.Host.Blazor", "Modulus.Host.Avalonia"). When specified, validates host-specific requirements.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation result containing any errors found.</returns>
    Task<ManifestValidationResult> ValidateAsync(string packagePath, string manifestPath, VsixManifest manifest, string? hostType = null, CancellationToken cancellationToken = default);
}
