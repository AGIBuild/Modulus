using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.IntegrationTests.Infrastructure;
using Modulus.Cli.Services;

namespace Modulus.Cli.IntegrationTests.Handlers;

public class LowCoverageBranchTests
{
    [Fact]
    public void SystemCliConsole_ExposesAllStreams()
    {
        var c = new SystemCliConsole();
        _ = c.Out;
        _ = c.Error;
        _ = c.In;
        _ = c.IsInputRedirected;
    }

    [Fact]
    public async Task BuildHandler_Verbose_Failure_EmitsProcessOutput()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-buildhandler-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "X.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Modulus.Sdk\" Version=\"1.0.0\" /></ItemGroup></Project>");

        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var console = new TestCliConsole(@out: stdout, error: stderr);
            var runner = new FakeProcessRunner(_ => new ProcessRunResult(1, "OUT", "ERR"));
            var handler = new BuildHandler(console, runner);

            var code = await handler.ExecuteAsync(dir, "Release", verbose: true, CancellationToken.None);

            Assert.Equal(1, code);
            Assert.Contains("OUT", stdout.ToString());
            Assert.Contains("ERR", stderr.ToString());
            Assert.Contains("Build failed", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task BuildHandler_SubdirectoryScan_BuildsFirstSubProject()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-projloc-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var sub = Path.Combine(dir, "Sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "Sub.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

        try
        {
            var stdout = new StringWriter();
            var console = new TestCliConsole(@out: stdout);
            var runner = new FakeProcessRunner(req =>
            {
                Assert.Contains("Sub.csproj", req.Arguments, StringComparison.OrdinalIgnoreCase);
                return new ProcessRunResult(0, "", "");
            });
            var handler = new BuildHandler(console, runner);

            var code = await handler.ExecuteAsync(dir, "Release", verbose: false, CancellationToken.None);

            Assert.Equal(0, code);
            Assert.Contains("Build succeeded", stdout.ToString());
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task PackHandler_BuildFails_ReturnsError()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-packhandler-buildfail-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "X.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Modulus.Sdk\" Version=\"1.0.0\" /></ItemGroup></Project>");

        try
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var console = new TestCliConsole(@out: stdout, error: stderr);
            var runner = new FakeProcessRunner(_ => new ProcessRunResult(1, "build-out", "build-err"));
            var handler = new PackHandler(console, runner);

            var code = await handler.ExecuteAsync(
                path: dir,
                output: Path.Combine(dir, "out"),
                configuration: "Release",
                noBuild: false,
                verbose: true,
                cancellationToken: CancellationToken.None);

            Assert.Equal(1, code);
            Assert.Contains("build-out", stdout.ToString());
            Assert.Contains("build-err", stderr.ToString());
            Assert.Contains("Build failed", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task PackHandler_InvalidManifest_ReturnsError()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-packhandler-badmanifest-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "X.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Modulus.Sdk\" Version=\"1.0.0\" /></ItemGroup></Project>");

        var outDir = Path.Combine(dir, "bin", "Release", "net10.0");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "X.dll"), "dummy");

        // Invalid XML
        File.WriteAllText(Path.Combine(dir, "extension.vsixmanifest"), "<not-xml");

        try
        {
            var stderr = new StringWriter();
            var console = new TestCliConsole(error: stderr);
            var runner = new FakeProcessRunner(_ => new ProcessRunResult(0, "", ""));
            var handler = new PackHandler(console, runner);

            var code = await handler.ExecuteAsync(
                path: dir,
                output: Path.Combine(dir, "out"),
                configuration: "Release",
                noBuild: true,
                verbose: false,
                cancellationToken: CancellationToken.None);

            Assert.Equal(1, code);
            Assert.Contains("identity", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task ListHandler_WhenDbContextMissing_ReturnsFailure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        using var sp = services.BuildServiceProvider();

        var handler = new ListHandler(sp);
        var result = await handler.ExecuteAsync(verbose: false, output: new StringWriter());

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }
}


