using Microsoft.Extensions.Logging;
using Modulus.Sdk;

namespace Modulus.Core.Manifest;

public sealed class DefaultManifestValidator : IManifestValidator
{
    private const string SupportedManifestVersion = "1.0";

    private readonly IManifestSignatureVerifier _signatureVerifier;
    private readonly ILogger<DefaultManifestValidator> _logger;

    public DefaultManifestValidator(IManifestSignatureVerifier signatureVerifier, ILogger<DefaultManifestValidator> logger)
    {
        _signatureVerifier = signatureVerifier;
        _logger = logger;
    }

    public async Task<bool> ValidateAsync(string packagePath, string manifestPath, ModuleManifest manifest, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(manifest.ManifestVersion, SupportedManifestVersion, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Manifest version {Version} is not supported. Expected {Expected}.", manifest.ManifestVersion, SupportedManifestVersion);
            return false;
        }

        if (manifest.Signature is null)
        {
            _logger.LogWarning("Manifest {ManifestPath} does not declare a signature. Skipping verification (Development Mode).", manifestPath);
            return true;
        }

        var validSignature = await _signatureVerifier.VerifyAsync(manifestPath, manifest.Signature, cancellationToken);
        if (!validSignature)
        {
            _logger.LogWarning("Manifest signature verification failed for {ManifestPath}.", manifestPath);
            return false;
        }

        return true;
    }
}

