using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.IntegrationTests.Infrastructure;
using Modulus.Cli.Services;

namespace Modulus.Cli.IntegrationTests.Handlers;

public class ProjectLocatorCoverageTests
{
    [Fact]
    public async Task BuildHandler_OutputDirectory_FallsBackToCoreDllSearch()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"modulus-projloc-coredll-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "X.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Modulus.Sdk\" Version=\"1.0.0\" /></ItemGroup></Project>");

        try
        {
            var coreOut = Path.Combine(dir, "bin", "somewhere");
            Directory.CreateDirectory(coreOut);
            File.WriteAllText(Path.Combine(coreOut, "X.Core.dll"), "dummy");

            var stdout = new StringWriter();
            var console = new TestCliConsole(@out: stdout);
            var runner = new FakeProcessRunner(_ => new ProcessRunResult(0, "", ""));
            var handler = new BuildHandler(console, runner);

            var code = await handler.ExecuteAsync(dir, "Release", verbose: false, CancellationToken.None);

            Assert.Equal(0, code);
            Assert.Contains("Output:", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("somewhere", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }
}


