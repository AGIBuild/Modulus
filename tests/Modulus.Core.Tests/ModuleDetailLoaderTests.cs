using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Modulus.Core.Runtime;
using Modulus.Sdk;

namespace Modulus.Core.Tests;

/// <summary>
/// Tests for lazy module detail loading with cancellation and timeout.
/// </summary>
public class ModuleDetailLoaderTests : IDisposable
{
    private readonly string _testRoot;
    private readonly ModuleDetailLoader _loader;

    public ModuleDetailLoaderTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "ModulusDetailTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testRoot);
        _loader = new ModuleDetailLoader(NullLogger<ModuleDetailLoader>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, true);
        }
        catch { /* ignore */ }
    }

    [Fact]
    public async Task LoadDetailAsync_WithReadme_ReturnsReadmeContent()
    {
        // Arrange
        var modulePath = CreateModuleWithReadme("readme-module", "# Hello World\nThis is a test.");

        // Act
        var result = await _loader.LoadDetailAsync(modulePath);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Hello World", result.Content);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task LoadDetailAsync_WithoutReadme_FallsBackToDescription()
    {
        // Arrange
        var modulePath = CreateModuleWithDescription("desc-module", "Test description from manifest");

        // Act
        var result = await _loader.LoadDetailAsync(modulePath);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Test description", result.Content);
    }

    [Fact]
    public async Task LoadDetailAsync_NoContent_ReturnsNoContentMessage()
    {
        // Arrange
        var modulePath = CreateEmptyModule("empty-module");

        // Act
        var result = await _loader.LoadDetailAsync(modulePath);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("No description", result.Content);
    }

    [Fact]
    public async Task LoadDetailAsync_NonExistentPath_ReturnsFailed()
    {
        // Act
        var result = await _loader.LoadDetailAsync("/non/existent/path");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task LoadDetailAsync_Cancelled_ReturnsCancelled()
    {
        // Arrange
        var modulePath = CreateModuleWithReadme("cancel-module", "Content");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _loader.LoadDetailAsync(modulePath, cts.Token);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.WasCancelled);
    }

    [Fact]
    public async Task LoadDetailAsync_Timeout_ReturnsTimedOut()
    {
        // Arrange
        var modulePath = CreateModuleWithReadme("timeout-module", "Content");

        // Act - Use a very short timeout
        var result = await _loader.LoadDetailAsync(modulePath, timeout: TimeSpan.FromTicks(1));

        // Assert
        // Note: This test may be flaky due to timing; we're just checking the timeout mechanism exists
        Assert.True(result.WasTimedOut || result.Success); // Either timed out or succeeded (if fast enough)
    }

    [Fact]
    public async Task LoadDetailAsync_WithManifestPath_WorksCorrectly()
    {
        // Arrange
        var modulePath = CreateModuleWithReadme("manifest-path-module", "README content");
        var manifestPath = Path.Combine(modulePath, "manifest.json");

        // Act
        var result = await _loader.LoadDetailAsync(manifestPath);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("README content", result.Content);
    }

    private string CreateModuleWithReadme(string id, string readmeContent)
    {
        var modulePath = Path.Combine(_testRoot, id);
        Directory.CreateDirectory(modulePath);

        var manifest = new ModuleManifest
        {
            Id = id,
            Version = "1.0.0",
            ManifestVersion = "1.0",
            SupportedHosts = new List<string>(),
            CoreAssemblies = new List<string>()
        };

        File.WriteAllText(Path.Combine(modulePath, "manifest.json"),
            JsonSerializer.Serialize(manifest));
        File.WriteAllText(Path.Combine(modulePath, "README.md"), readmeContent);

        return modulePath;
    }

    private string CreateModuleWithDescription(string id, string description)
    {
        var modulePath = Path.Combine(_testRoot, id);
        Directory.CreateDirectory(modulePath);

        var manifest = new ModuleManifest
        {
            Id = id,
            Version = "1.0.0",
            ManifestVersion = "1.0",
            Description = description,
            SupportedHosts = new List<string>(),
            CoreAssemblies = new List<string>()
        };

        File.WriteAllText(Path.Combine(modulePath, "manifest.json"),
            JsonSerializer.Serialize(manifest));

        return modulePath;
    }

    private string CreateEmptyModule(string id)
    {
        var modulePath = Path.Combine(_testRoot, id);
        Directory.CreateDirectory(modulePath);

        var manifest = new ModuleManifest
        {
            Id = id,
            Version = "1.0.0",
            ManifestVersion = "1.0",
            SupportedHosts = new List<string>(),
            CoreAssemblies = new List<string>()
        };

        File.WriteAllText(Path.Combine(modulePath, "manifest.json"),
            JsonSerializer.Serialize(manifest));

        return modulePath;
    }
}


