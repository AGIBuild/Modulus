using System.IO.Compression;
using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.IntegrationTests.Infrastructure;
using Modulus.Cli.Services;
using Modulus.Sdk;

namespace Modulus.Cli.IntegrationTests.Handlers;

public class PackHandlerTests
{
    [Fact]
    public async Task Execute_DirectoryNotFound_ReturnsError()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var console = new TestCliConsole(@out: stdout, error: stderr);
        var runner = new FakeProcessRunner(_ => new ProcessRunResult(0, "", ""));
        var handler = new PackHandler(console, runner);

        var code = await handler.ExecuteAsync(
            path: Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}"),
            output: null,
            configuration: "Release",
            noBuild: true,
            verbose: false,
            cancellationToken: CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Contains("Directory not found", stderr.ToString());
    }

    [Fact]
    public async Task Execute_NoProjectFound_ReturnsError()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-pack-noproj-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var console = new TestCliConsole(@out: stdout, error: stderr);
            var runner = new FakeProcessRunner(_ => new ProcessRunResult(0, "", ""));
            var handler = new PackHandler(console, runner);

            var code = await handler.ExecuteAsync(
                path: dir,
                output: null,
                configuration: "Release",
                noBuild: true,
                verbose: false,
                cancellationToken: CancellationToken.None);

            Assert.Equal(1, code);
            Assert.Contains("No module project found", stderr.ToString());
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task Execute_NoBuildOutput_ReturnsError()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-pack-nobin-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "X.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Modulus.Sdk\" Version=\"1.0.0\" /></ItemGroup></Project>");

        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var console = new TestCliConsole(@out: stdout, error: stderr);
            var runner = new FakeProcessRunner(_ => new ProcessRunResult(0, "", ""));
            var handler = new PackHandler(console, runner);

            var code = await handler.ExecuteAsync(
                path: dir,
                output: null,
                configuration: "Release",
                noBuild: true,
                verbose: false,
                cancellationToken: CancellationToken.None);

            Assert.Equal(1, code);
            Assert.Contains("build output directory", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task Execute_Verbose_ExcludesSharedAssemblies_And_CreatesPackage()
    {
        var repoRoot = FindRepoRoot();
        var dir = Path.Combine(repoRoot, "artifacts", "tmp", $"modulus-pack-handler-{Guid.NewGuid():N}");
        var moduleName = "PackHandlerUnit";

        Directory.CreateDirectory(dir);
        var moduleDir = Path.Combine(dir, moduleName);
        Directory.CreateDirectory(moduleDir);

        try
        {
            // Project file so ProjectLocator can find something (we run with --no-build).
            File.WriteAllText(
                Path.Combine(moduleDir, $"{moduleName}.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Modulus.Sdk\" Version=\"1.0.0\" /></ItemGroup></Project>");

            // Core output (covers FindBuildOutput Core-project branch)
            var coreDir = Path.Combine(moduleDir, $"{moduleName}.Core");
            var coreOut = Path.Combine(coreDir, "bin", "Release", "net10.0");
            Directory.CreateDirectory(coreOut);
            File.WriteAllText(Path.Combine(coreOut, $"{moduleName}.Core.dll"), "dummy");
            File.WriteAllText(Path.Combine(coreOut, "Modulus.Core.dll"), "dummy-shared"); // should be excluded
            File.WriteAllText(Path.Combine(coreOut, "System.Runtime.dll"), "dummy-framework"); // should be excluded

            // UI output
            var uiDir = Path.Combine(moduleDir, $"{moduleName}.UI.Avalonia");
            var uiOut = Path.Combine(uiDir, "bin", "Release", "net10.0");
            Directory.CreateDirectory(uiOut);
            File.WriteAllText(Path.Combine(uiOut, $"{moduleName}.UI.Avalonia.dll"), "dummy");

            // Manifest in root (covers FindManifest root branch)
            File.WriteAllText(Path.Combine(moduleDir, "extension.vsixmanifest"), CreateManifestXml(
                id: "e3b7c6b2-7b7a-4c8c-bc3d-9b0a42d3a001",
                version: "1.2.3",
                displayName: moduleName,
                hostId: ModulusHostIds.Avalonia));

            // Optional file
            File.WriteAllText(Path.Combine(moduleDir, "README.md"), "readme");

            var outDir = Path.Combine(moduleDir, "out");

            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var console = new TestCliConsole(@out: stdout, error: stderr);
            var runner = new FakeProcessRunner(_ => new ProcessRunResult(0, "", ""));
            var handler = new PackHandler(console, runner);

            var code = await handler.ExecuteAsync(
                path: moduleDir,
                output: outDir,
                configuration: "Release",
                noBuild: true,
                verbose: true,
                cancellationToken: CancellationToken.None);

            Assert.Equal(0, code);
            Assert.Contains("Packaging complete", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Shared assemblies excluded", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.True(string.IsNullOrEmpty(stderr.ToString()), stderr.ToString());

            var packages = Directory.GetFiles(outDir, "*.modpkg");
            Assert.Single(packages);

            var extractDir = Path.Combine(outDir, "extracted");
            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(packages[0], extractDir);

            var dlls = Directory.GetFiles(extractDir, "*.dll").Select(Path.GetFileName).ToList();
            Assert.Contains($"{moduleName}.Core.dll", dlls);
            Assert.Contains($"{moduleName}.UI.Avalonia.dll", dlls);
            Assert.DoesNotContain("Modulus.Core.dll", dlls);
            Assert.DoesNotContain("System.Runtime.dll", dlls);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Modulus.sln")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate repo root (Modulus.sln not found).");
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


