using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Modulus.Core.Manifest;
using Modulus.Sdk;
using NSubstitute;

namespace Modulus.Core.Tests;

public class DefaultManifestValidatorTests
{
    private readonly ILogger<DefaultManifestValidator> _logger = Substitute.For<ILogger<DefaultManifestValidator>>();

    [Fact]
    public async Task ValidateAsync_MissingRequiredFields_ReturnsErrors()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "extension.vsixmanifest");
        var manifest = CreateTestManifest(id: "", version: "not-semver", displayName: "");
        manifest.Installation.Clear();

        var validator = new DefaultManifestValidator(_logger);
        var result = await validator.ValidateAsync(tempDir, manifestPath, manifest, ModulusHostIds.Avalonia);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_InvalidDependencyRange_ReturnsError()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "extension.vsixmanifest");
        var manifest = CreateTestManifest();
        manifest.Dependencies.Add(new ManifestDependency { Id = "dep-module", Version = "invalid" });

        var validator = new DefaultManifestValidator(_logger);
        var result = await validator.ValidateAsync(tempDir, manifestPath, manifest, ModulusHostIds.Avalonia);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("invalid version range"));
    }

    [Fact]
    public async Task ValidateAsync_UnsupportedHost_ReturnsError()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "extension.vsixmanifest");
        var manifest = CreateTestManifest();
        manifest.Installation.Clear();
        manifest.Installation.Add(new InstallationTarget { Id = ModulusHostIds.Blazor });

        var validator = new DefaultManifestValidator(_logger);
        var result = await validator.ValidateAsync(tempDir, manifestPath, manifest, ModulusHostIds.Avalonia);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not supported by this module"));
    }

    [Fact]
    public async Task ValidateAsync_MissingPackageAsset_ReturnsError()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "extension.vsixmanifest");
        var manifest = CreateTestManifest();
        manifest.Assets.Clear(); // No assets

        var validator = new DefaultManifestValidator(_logger);
        var result = await validator.ValidateAsync(tempDir, manifestPath, manifest, ModulusHostIds.Avalonia);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains(ModulusAssetTypes.Package));
    }

    [Fact]
    public async Task ValidateAsync_ValidManifest_ReturnsSuccess()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "extension.vsixmanifest");
        var manifest = CreateTestManifest();

        // Create the dll file so path validation passes
        File.WriteAllBytes(Path.Combine(tempDir, "Test.Core.dll"), Array.Empty<byte>());

        var validator = new DefaultManifestValidator(_logger);
        var result = await validator.ValidateAsync(tempDir, manifestPath, manifest, ModulusHostIds.Avalonia);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_UnsupportedAssetType_IsRejected()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "extension.vsixmanifest");
        var manifest = CreateTestManifest();

        // Create the dll file so path validation passes
        File.WriteAllBytes(Path.Combine(tempDir, "Test.Core.dll"), Array.Empty<byte>());

        // Unsupported asset types are rejected (only latest design is allowed).
        manifest.Assets.Add(new ManifestAsset { Type = "Unsupported.Asset", TargetHost = ModulusHostIds.Avalonia });

        var validator = new DefaultManifestValidator(_logger);
        var result = await validator.ValidateAsync(tempDir, manifestPath, manifest, ModulusHostIds.Avalonia);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Unsupported asset type", StringComparison.OrdinalIgnoreCase));
    }

    private static VsixManifest CreateTestManifest(
        string id = "test-module",
        string version = "1.0.0",
        string displayName = "Test Module")
    {
        return new VsixManifest
        {
            Version = "2.0.0",
            Metadata = new ManifestMetadata
            {
                Identity = new ManifestIdentity
                {
                    Id = id,
                    Version = version,
                    Publisher = "TestPublisher"
                },
                DisplayName = displayName,
                Description = "Test description"
            },
            Installation = new List<InstallationTarget>
            {
                new() { Id = ModulusHostIds.Avalonia }
            },
            Assets = new List<ManifestAsset>
            {
                new() { Type = ModulusAssetTypes.Package, Path = "Test.Core.dll" }
            }
        };
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ModulusTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }
}
