using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Modulus.Core.Manifest;
using Modulus.Core.Runtime;
using Modulus.Sdk;
using NSubstitute;

namespace Modulus.Core.Tests;

public class DefaultManifestValidatorTests
{
    private readonly IManifestSignatureVerifier _signatureVerifier = Substitute.For<IManifestSignatureVerifier>();
    private readonly ILogger<DefaultManifestValidator> _logger = Substitute.For<ILogger<DefaultManifestValidator>>();

    [Fact]
    public async Task ValidateAsync_MissingRequiredFields_Fails()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "manifest.json");
        var manifest = new ModuleManifest
        {
            Id = "",
            Version = "not-semver",
            DisplayName = "",
            SupportedHosts = new List<string>(),
            CoreAssemblies = new List<string>(),
            UiAssemblies = new Dictionary<string, List<string>>()
        };

        var validator = new DefaultManifestValidator(_signatureVerifier, _logger);
        var result = await validator.ValidateAsync(tempDir, manifestPath, manifest, HostType.Avalonia);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_InvalidDependencyRange_Fails()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "manifest.json");
        var manifest = new ModuleManifest
        {
            Id = "test-module",
            Version = "1.0.0",
            DisplayName = "Test",
            SupportedHosts = new List<string> { HostType.Avalonia },
            CoreAssemblies = new List<string> { "Test.Core.dll" },
            UiAssemblies = new Dictionary<string, List<string>> { { HostType.Avalonia, new List<string> { "Test.UI.dll" } } },
            Dependencies = new Dictionary<string, string> { { "dep-module", "invalid" } }
        };

        var validator = new DefaultManifestValidator(_signatureVerifier, _logger);
        var result = await validator.ValidateAsync(tempDir, manifestPath, manifest, HostType.Avalonia);

        Assert.False(result);
    }

    [Fact]
    public async Task ValidateAsync_HostAssembliesMissing_Fails()
    {
        var tempDir = CreateTempDir();
        var manifestPath = Path.Combine(tempDir, "manifest.json");
        var manifest = new ModuleManifest
        {
            Id = "test-module",
            Version = "1.0.0",
            DisplayName = "Test",
            SupportedHosts = new List<string> { HostType.Blazor },
            CoreAssemblies = new List<string> { "Test.Core.dll" },
            UiAssemblies = new Dictionary<string, List<string>>()
        };

        var validator = new DefaultManifestValidator(_signatureVerifier, _logger);
        var result = await validator.ValidateAsync(tempDir, manifestPath, manifest, HostType.Blazor);

        Assert.False(result);
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ModulusTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }
}

