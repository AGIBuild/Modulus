using System.Threading;
using System.Threading.Tasks;
using Modulus.Sdk;

namespace Modulus.Core.Manifest;

public interface IManifestSignatureVerifier
{
    Task<bool> VerifyAsync(string manifestPath, ManifestSignature signature, CancellationToken cancellationToken = default);
}

