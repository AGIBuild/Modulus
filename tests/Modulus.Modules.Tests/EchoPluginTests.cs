using Modulus.Core;
using Modulus.Core.Architecture;
using Modulus.Core.Installation;
using Modulus.Core.Manifest;
using Modulus.Core.Runtime;
using Modulus.Sdk;
using Microsoft.Extensions.Logging.Abstractions;

namespace Modulus.Modules.Tests;

public class EchoPluginTests
{
    [Fact]
    public async Task Can_Load_EchoPlugin_From_Output()
    {
        // Arrange
        var currentDir = AppContext.BaseDirectory;
        var rootDir = FindSolutionRoot(currentDir);
        Assert.NotNull(rootDir);

        var echoPluginPath = Path.Combine(rootDir!, "_output", "modules", "EchoPlugin");
        
        // Skip test if output directory doesn't exist (not deployed)
        if (!Directory.Exists(echoPluginPath))
        {
            // This test requires pre-deployed module, skip gracefully
            return;
        }

        var runtimeContext = new RuntimeContext();
        runtimeContext.SetCurrentHost(ModulusHostIds.Avalonia);
        var manifestValidator = new DefaultManifestValidator(NullLogger<DefaultManifestValidator>.Instance);
        var sharedAssemblies = SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        var loggerFactory = NullLoggerFactory.Instance;
        var executionGuard = new ModuleExecutionGuard(NullLogger<ModuleExecutionGuard>.Instance, runtimeContext);
        var loader = new ModuleLoader(runtimeContext, manifestValidator, sharedAssemblies, executionGuard, NullLogger<ModuleLoader>.Instance, loggerFactory);

        // Act
        var descriptor = await loader.LoadAsync(echoPluginPath);

        // Assert
        Assert.NotNull(descriptor);
        // Module ID is now a GUID, check for non-empty
        Assert.False(string.IsNullOrEmpty(descriptor.Id));
        Assert.Equal("1.0.0", descriptor.Version);
        Assert.Contains(ModulusHostIds.Blazor, descriptor.SupportedHosts);
        Assert.Contains(ModulusHostIds.Avalonia, descriptor.SupportedHosts);
    }

    [Fact]
    public async Task Can_Load_EchoPlugin_From_DevOutput()
    {
        // Arrange - Load from development build output
        var currentDir = AppContext.BaseDirectory;
        var rootDir = FindSolutionRoot(currentDir);
        Assert.NotNull(rootDir);

        // Development path: artifacts/Modules/EchoPlugin/
        var devOutputPath = Path.Combine(rootDir!, "artifacts", "Modules", "EchoPlugin");
        
        // Skip if not built
        if (!Directory.Exists(devOutputPath) || !File.Exists(Path.Combine(devOutputPath, SystemModuleInstaller.VsixManifestFileName)))
        {
            return;
        }

        var runtimeContext = new RuntimeContext();
        runtimeContext.SetCurrentHost(ModulusHostIds.Avalonia);
        var manifestValidator = new DefaultManifestValidator(NullLogger<DefaultManifestValidator>.Instance);
        var sharedAssemblies = SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        var loggerFactory = NullLoggerFactory.Instance;
        var executionGuard = new ModuleExecutionGuard(NullLogger<ModuleExecutionGuard>.Instance, runtimeContext);
        var loader = new ModuleLoader(runtimeContext, manifestValidator, sharedAssemblies, executionGuard, NullLogger<ModuleLoader>.Instance, loggerFactory);

        // Act
        var descriptor = await loader.LoadAsync(devOutputPath);

        // Assert
        Assert.NotNull(descriptor);
        Assert.False(string.IsNullOrEmpty(descriptor.Id));
        Assert.Equal("1.0.0", descriptor.Version);
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
