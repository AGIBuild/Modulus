using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;
using NuGet.Versioning;

namespace Modulus.Core.Manifest;

public sealed class DefaultManifestValidator : IManifestValidator
{
    private readonly ILogger<DefaultManifestValidator> _logger;

    public DefaultManifestValidator(ILogger<DefaultManifestValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<ManifestValidationResult> ValidateAsync(string packagePath, string manifestPath, VsixManifest manifest, string? hostType = null, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Validate version
        if (!manifest.Version.StartsWith("2."))
        {
            errors.Add($"Manifest version {manifest.Version} is not supported. Expected 2.x.");
        }

        // Validate Identity
        var identity = manifest.Metadata.Identity;
        if (string.IsNullOrWhiteSpace(identity.Id))
        {
            errors.Add("Manifest is missing required Identity/@Id attribute.");
        }

        if (string.IsNullOrWhiteSpace(identity.Publisher))
        {
            errors.Add("Manifest is missing required Identity/@Publisher attribute.");
        }

        if (string.IsNullOrWhiteSpace(identity.Version) || !NuGetVersion.TryParse(identity.Version, out _))
        {
            errors.Add($"Identity version '{identity.Version}' is not a valid semantic version.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Metadata.DisplayName))
        {
            errors.Add("Manifest is missing required DisplayName element.");
        }

        // Validate Installation targets
        if (manifest.Installation.Count == 0)
        {
            errors.Add("Manifest must declare at least one InstallationTarget.");
        }

        // Host-specific validation
        if (hostType != null)
        {
            var hasHostTarget = manifest.Installation.Any(t =>
                string.Equals(t.Id, hostType, StringComparison.OrdinalIgnoreCase));

            if (!hasHostTarget)
            {
                errors.Add($"Host '{hostType}' is not supported by this module. Supported hosts: {FormatInstallationTargets(manifest.Installation)}.");
            }
        }

        // Validate Dependencies version ranges
        foreach (var dep in manifest.Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dep.Id))
            {
                errors.Add("Dependency Id cannot be empty.");
                continue;
            }

            if (string.Equals(dep.Id, identity.Id, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Module cannot depend on itself.");
            }

            if (!VersionRange.TryParse(dep.Version, out _))
            {
                errors.Add($"Dependency '{dep.Id}' has invalid version range '{dep.Version}'.");
            }
        }

        // Validate Assets - must have at least one Modulus.Package
        var packageAssets = manifest.Assets
            .Where(a => string.Equals(a.Type, ModulusAssetTypes.Package, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (packageAssets.Count == 0)
        {
            errors.Add($"Manifest must include at least one Asset of type '{ModulusAssetTypes.Package}'.");
        }

        // Validate Asset paths exist
        foreach (var asset in manifest.Assets)
        {
            if (!string.IsNullOrEmpty(asset.Path))
            {
                var assetPath = Path.Combine(packagePath, asset.Path);
                if (!File.Exists(assetPath))
                {
                    errors.Add($"Asset '{asset.Type}' references missing file '{asset.Path}'.");
                }
            }

            // Validate Menu assets have required fields
            if (string.Equals(asset.Type, ModulusAssetTypes.Menu, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(asset.Id))
                {
                    errors.Add("Menu asset is missing required 'Id' attribute.");
                }
                if (string.IsNullOrWhiteSpace(asset.DisplayName))
                {
                    errors.Add($"Menu asset '{asset.Id}' is missing required 'DisplayName' attribute.");
                }
                if (string.IsNullOrWhiteSpace(asset.Route))
                {
                    errors.Add($"Menu asset '{asset.Id}' is missing required 'Route' attribute.");
                }
            }
        }

        // Host-specific Asset validation
        if (hostType != null)
        {
            var hasHostPackage = packageAssets.Any(a =>
                string.IsNullOrEmpty(a.TargetHost) || // No target = all hosts
                string.Equals(a.TargetHost, hostType, StringComparison.OrdinalIgnoreCase));

            if (!hasHostPackage)
            {
                errors.Add($"No Package asset available for host '{hostType}'.");
            }
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                _logger.LogWarning(error);
            }
            return Task.FromResult(ManifestValidationResult.Failure(errors));
        }

        return Task.FromResult(ManifestValidationResult.Success());
    }

    private static string FormatInstallationTargets(List<InstallationTarget> targets) =>
        targets.Count == 0 ? "(none)" : string.Join(", ", targets.Select(t => t.Id));
}
