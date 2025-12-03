using System.Threading;
using System.Threading.Tasks;
using Modulus.Sdk;

namespace Modulus.Core.Manifest;

public interface IManifestValidator
{
    Task<bool> ValidateAsync(string packagePath, string manifestPath, ModuleManifest manifest, CancellationToken cancellationToken = default);
}

