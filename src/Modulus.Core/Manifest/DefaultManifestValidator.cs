using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Modulus.Core.Runtime;
using Modulus.Sdk;
using NuGet.Versioning;

namespace Modulus.Core.Manifest;

public sealed class DefaultManifestValidator : IManifestValidator
{
    private readonly ILogger<DefaultManifestValidator> _logger;
    private readonly RuntimeContext? _runtimeContext;

    public DefaultManifestValidator(ILogger<DefaultManifestValidator> logger)
    {
        _logger = logger;
    }

    public DefaultManifestValidator(ILogger<DefaultManifestValidator> logger, RuntimeContext runtimeContext)
    {
        _logger = logger;
        _runtimeContext = runtimeContext;
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
            var hostTarget = manifest.Installation.FirstOrDefault(t =>
                string.Equals(t.Id, hostType, StringComparison.OrdinalIgnoreCase));

            if (hostTarget == null)
            {
                errors.Add($"Host '{hostType}' is not supported by this module. Supported hosts: {FormatInstallationTargets(manifest.Installation)}.");
            }
            else
            {
                // Validate host version compatibility
                var hostVersionError = ValidateHostVersionCompatibility(hostTarget, hostType);
                if (hostVersionError != null)
                {
                    errors.Add(hostVersionError);
                }
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

    /// <summary>
    /// Validates that the host version satisfies the module's InstallationTarget version range.
    /// </summary>
    private string? ValidateHostVersionCompatibility(InstallationTarget target, string hostType)
    {
        // If no RuntimeContext or no HostVersion, skip version validation
        if (_runtimeContext?.HostVersion == null)
        {
            return null;
        }

        // Parse the version range from the installation target
        if (string.IsNullOrWhiteSpace(target.Version))
        {
            return null;
        }

        if (!VersionRange.TryParse(target.Version, out var versionRange))
        {
            _logger.LogWarning("InstallationTarget for host '{Host}' has invalid version range '{Version}'", 
                hostType, target.Version);
            return $"InstallationTarget for host '{hostType}' has invalid version range '{target.Version}'.";
        }

        // Convert System.Version to NuGetVersion for comparison
        var hostNuGetVersion = new NuGetVersion(
            _runtimeContext.HostVersion.Major,
            _runtimeContext.HostVersion.Minor,
            _runtimeContext.HostVersion.Build >= 0 ? _runtimeContext.HostVersion.Build : 0,
            _runtimeContext.HostVersion.Revision >= 0 ? _runtimeContext.HostVersion.Revision : 0);

        if (!versionRange.Satisfies(hostNuGetVersion))
        {
            _logger.LogWarning(
                "Module requires host '{Host}' version {Range}, but current host version is {CurrentVersion}",
                hostType, target.Version, hostNuGetVersion);
            return $"Module requires host '{hostType}' version {target.Version}, but current host version is {hostNuGetVersion}.";
        }

        return null;
    }
}
