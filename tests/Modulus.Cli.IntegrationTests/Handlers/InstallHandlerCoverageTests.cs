using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.Services;

namespace Modulus.Cli.IntegrationTests.Handlers;

public class InstallHandlerCoverageTests
{
    [Fact]
    public async Task ExecuteAsync_SourceDirectoryMissingManifest_ReturnsFailure()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-installhandler-nomanifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            using var sp = services.BuildServiceProvider();

            var handler = new InstallHandler(sp, modulesDirectory: Path.Combine(dir, "Modules"));
            var result = await handler.ExecuteAsync(dir, force: true, output: new StringWriter());

            Assert.False(result.Success);
            Assert.Contains("vsixmanifest", result.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task ExecuteAsync_InvalidManifest_ReturnsFailure()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-installhandler-badmanifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "extension.vsixmanifest"), "<not-xml");

        try
        {
            var services = new ServiceCollection();
            services.AddLogging();
            using var sp = services.BuildServiceProvider();

            var handler = new InstallHandler(sp, modulesDirectory: Path.Combine(dir, "Modules"));
            var result = await handler.ExecuteAsync(dir, force: true, output: new StringWriter());

            Assert.False(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Message));
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenInstallerServiceMissing_ReturnsFailureViaCatch()
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"modulus-installhandler-missingservice-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDir);

        var sourceDir = Path.Combine(workDir, "Source");
        Directory.CreateDirectory(sourceDir);

        // Minimal valid manifest so VsixManifestReader returns non-null.
        File.WriteAllText(Path.Combine(sourceDir, "extension.vsixmanifest"), CreateManifestXml(
            id: Guid.NewGuid().ToString("D"),
            version: "1.0.0",
            displayName: "MissingService",
            hostId: "Modulus.Host.Avalonia"));

        // Place a dummy file to copy.
        File.WriteAllText(Path.Combine(sourceDir, "Some.dll"), "x");

        var modulesDir = Path.Combine(workDir, "Modules");
        Directory.CreateDirectory(modulesDir);

        try
        {
            // Intentionally omit IModuleInstallerService registration to trigger handler catch.
            var services = new ServiceCollection();
            services.AddLogging();
            using var sp = services.BuildServiceProvider();

            var handler = new InstallHandler(sp, modulesDirectory: modulesDir);
            var result = await handler.ExecuteAsync(sourceDir, force: true, output: new StringWriter());

            Assert.False(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Message));
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { /* ignore */ }
        }
    }

    private static string CreateManifestXml(string id, string version, string displayName, string hostId)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<PackageManifest Version=""2.0.0"" xmlns=""http://schemas.microsoft.com/developer/vsx-schema/2011"">
  <Metadata>
    <Identity Id=""{id}"" Version=""{version}"" Language=""en-US"" Publisher=""Tests"" />
    <DisplayName>{displayName}</DisplayName>
    <Description xml:space=""preserve"">Test</Description>
  </Metadata>
  <Installation>
    <InstallationTarget Id=""{hostId}"" Version=""[1.0,2.0)"" />
  </Installation>
  <Assets>
    <Asset Type=""Microsoft.VisualStudio.MefComponent"" Path="".""/>
  </Assets>
</PackageManifest>";
    }
}


