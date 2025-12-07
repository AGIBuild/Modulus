using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;
using NuGet.Versioning;

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

    public async Task<bool> ValidateAsync(string packagePath, string manifestPath, ModuleManifest manifest, string? hostType = null, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (!string.Equals(manifest.ManifestVersion, SupportedManifestVersion, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Manifest version {manifest.ManifestVersion} is not supported. Expected {SupportedManifestVersion}.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            errors.Add("Manifest is missing required field 'id'.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version) || !NuGetVersion.TryParse(manifest.Version, out _))
        {
            errors.Add($"Manifest version '{manifest.Version}' is not a valid semantic version.");
        }

        if (manifest.CoreAssemblies == null || manifest.UiAssemblies == null)
        {
            errors.Add("Manifest must include coreAssemblies and uiAssemblies.");
        }

        if (hostType != null)
        {
            if (manifest.SupportedHosts == null || !manifest.SupportedHosts.Any(h => string.Equals(h, hostType, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"Host '{hostType}' is not supported by this module.");
            }

            if (manifest.UiAssemblies != null && manifest.UiAssemblies.Count > 0 && !manifest.UiAssemblies.ContainsKey(hostType))
            {
                errors.Add($"Manifest does not declare UI assemblies for host '{hostType}'.");
            }
        }

        foreach (var (dependencyId, dependencyRange) in manifest.Dependencies)
        {
            if (!VersionRange.TryParse(dependencyRange, out _))
            {
                errors.Add($"Dependency '{dependencyId}' has invalid version range '{dependencyRange}'.");
            }
        }

        foreach (var (assemblyRelativePath, expectedHash) in manifest.AssemblyHashes)
        {
            var assemblyPath = Path.Combine(packagePath, assemblyRelativePath);
            if (!File.Exists(assemblyPath))
            {
                errors.Add($"Assembly hash declared for missing file '{assemblyRelativePath}'.");
                continue;
            }

            var hash = await ComputeSha256Async(assemblyPath, cancellationToken).ConfigureAwait(false);
            if (!hash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Assembly hash mismatch for '{assemblyRelativePath}'. Expected {expectedHash}, computed {hash}.");
            }
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                _logger.LogWarning(error);
            }
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

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hashBytes = await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes);
    }
}

