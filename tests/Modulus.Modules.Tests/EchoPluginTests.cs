using Modulus.Core.Manifest;
using Modulus.Core.Runtime;
using Microsoft.Extensions.Logging.Abstractions;

namespace Modulus.Modules.Tests;

public class EchoPluginTests
{
    [Fact]
    public async Task Can_Load_EchoPlugin_From_Output()
    {
        // Arrange
        // Locate _output/modules/EchoPlugin
        // We assume the test runs from /bin/Debug/net10.0/
        // So we need to go up to root.
        
        var currentDir = AppContext.BaseDirectory;
        var rootDir = FindSolutionRoot(currentDir);
        Assert.NotNull(rootDir);
        
        var echoPluginPath = Path.Combine(rootDir!, "_output", "modules", "EchoPlugin");
        Assert.True(Directory.Exists(echoPluginPath), $"EchoPlugin not found at {echoPluginPath}. Did you run deploy-module.ps1?");

        var runtimeContext = new RuntimeContext();
        var signatureVerifier = new Sha256ManifestSignatureVerifier(NullLogger<Sha256ManifestSignatureVerifier>.Instance);
        var manifestValidator = new DefaultManifestValidator(signatureVerifier, NullLogger<DefaultManifestValidator>.Instance);
        var loader = new ModuleLoader(runtimeContext, manifestValidator, NullLogger<ModuleLoader>.Instance);

        // Act
        var descriptor = await loader.LoadAsync(echoPluginPath);

        // Assert
        Assert.NotNull(descriptor);
        Assert.Equal("EchoPlugin", descriptor.Id);
        Assert.Equal("1.0.0", descriptor.Version);
        Assert.Contains("BlazorApp", descriptor.SupportedHosts);
        Assert.Contains("AvaloniaApp", descriptor.SupportedHosts);
    }

    private string? FindSolutionRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Modulus.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        return null;
    }
}

