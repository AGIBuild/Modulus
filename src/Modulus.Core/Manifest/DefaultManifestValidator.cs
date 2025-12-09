using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;
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

    public async Task<ManifestValidationResult> ValidateAsync(string packagePath, string manifestPath, ModuleManifest manifest, string? hostType = null, CancellationToken cancellationToken = default)
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

        if (string.IsNullOrWhiteSpace(manifest.DisplayName))
        {
            errors.Add("Manifest is missing required field 'displayName'.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version) || !NuGetVersion.TryParse(manifest.Version, out _))
        {
            errors.Add($"Manifest version '{manifest.Version}' is not a valid semantic version.");
        }

        if (manifest.CoreAssemblies == null || manifest.CoreAssemblies.Count == 0)
        {
            errors.Add("Manifest must include at least one core assembly in 'coreAssemblies'.");
        }

        if (manifest.UiAssemblies == null)
        {
            errors.Add("Manifest must include uiAssemblies object (may be empty per host).");
        }

        if (hostType != null)
        {
            if (manifest.SupportedHosts == null || !manifest.SupportedHosts.Any(h => string.Equals(h, hostType, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"Host '{hostType}' is not supported by this module. Supported hosts: {FormatHosts(manifest.SupportedHosts)}.");
            }

            if (manifest.UiAssemblies == null || !manifest.UiAssemblies.TryGetValue(hostType, out var hostAssemblies) || hostAssemblies == null || hostAssemblies.Count == 0)
            {
                errors.Add($"Host '{hostType}' requires UI assemblies but none are provided in uiAssemblies.");
            }
        }

        if (manifest.SupportedHosts == null || !manifest.SupportedHosts.Any())
        {
            errors.Add("Manifest must declare at least one supported host in 'supportedHosts'.");
        }

        foreach (var (dependencyId, dependencyRange) in manifest.Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependencyId))
            {
                errors.Add("Dependency id cannot be empty.");
                continue;
            }

            if (string.Equals(dependencyId, manifest.Id, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Module cannot depend on itself.");
            }

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

        // Validate sharedAssemblyHints
        if (manifest.SharedAssemblyHints.Count > SharedAssemblyOptions.MaxManifestHints)
        {
            errors.Add($"sharedAssemblyHints contains {manifest.SharedAssemblyHints.Count} entries, exceeding maximum of {SharedAssemblyOptions.MaxManifestHints}.");
        }

        foreach (var hint in manifest.SharedAssemblyHints)
        {
            if (string.IsNullOrWhiteSpace(hint))
            {
                errors.Add("sharedAssemblyHints contains empty or whitespace entry.");
                continue;
            }

            if (hint.Length > SharedAssemblyOptions.MaxAssemblyNameLength)
            {
                errors.Add($"sharedAssemblyHints entry '{hint[..50]}...' exceeds maximum length of {SharedAssemblyOptions.MaxAssemblyNameLength}.");
            }
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                _logger.LogWarning(error);
            }
            return ManifestValidationResult.Failure(errors);
        }

        if (manifest.Signature is null)
        {
            _logger.LogDebug("Manifest {ManifestPath} has no signature, skipping verification (dev mode).", manifestPath);
            return ManifestValidationResult.Success();
        }

        var validSignature = await _signatureVerifier.VerifyAsync(manifestPath, manifest.Signature, cancellationToken);
        if (!validSignature)
        {
            _logger.LogWarning("Manifest signature verification failed for {ManifestPath}.", manifestPath);
            return ManifestValidationResult.Failure(new[] { "Manifest signature verification failed." });
        }

        return ManifestValidationResult.Success();
    }

    private static string FormatHosts(List<string>? hosts) =>
        hosts == null || hosts.Count == 0 ? "(none)" : string.Join(", ", hosts);

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hashBytes = await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes);
    }
}

