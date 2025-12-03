using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;

namespace Modulus.Core.Manifest;

public sealed class Sha256ManifestSignatureVerifier : IManifestSignatureVerifier
{
    private readonly ILogger<Sha256ManifestSignatureVerifier> _logger;

    public Sha256ManifestSignatureVerifier(ILogger<Sha256ManifestSignatureVerifier> logger)
    {
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string manifestPath, ManifestSignature signature, CancellationToken cancellationToken = default)
    {
        if (!"SHA256".Equals(signature.Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Unsupported signature algorithm {Algorithm}.", signature.Algorithm);
            return false;
        }

        var signaturePath = Path.Combine(Path.GetDirectoryName(manifestPath) ?? string.Empty, signature.File);
        if (!File.Exists(signaturePath))
        {
            _logger.LogWarning("Signature file {Path} not found.", signaturePath);
            return false;
        }

        var manifestBytes = await File.ReadAllBytesAsync(manifestPath, cancellationToken);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(manifestBytes);
        var hashHex = Convert.ToHexString(hash);

        var expected = (await File.ReadAllTextAsync(signaturePath, cancellationToken)).Trim();
        var match = hashHex.Equals(expected, StringComparison.OrdinalIgnoreCase);
        if (!match)
        {
            _logger.LogWarning("Manifest signature mismatch. Expected {Expected} but computed {Computed}.", expected, hashHex);
        }

        return match;
    }
}

