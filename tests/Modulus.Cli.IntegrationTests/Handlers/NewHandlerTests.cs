using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.IntegrationTests.Infrastructure;

namespace Modulus.Cli.IntegrationTests.Handlers;

public class NewHandlerTests
{
    [Fact]
    public async Task Execute_List_PrintsTemplates()
    {
        var stdout = new StringWriter();
        var console = new TestCliConsole(@out: stdout);
        var handler = new NewHandler(console);

        var code = await handler.ExecuteAsync(template: null, name: null, output: null, force: false, list: true, cancellationToken: CancellationToken.None);

        Assert.Equal(0, code);
        Assert.Contains("Available templates:", stdout.ToString());
        Assert.Contains("module-avalonia", stdout.ToString());
    }

    [Fact]
    public async Task Execute_MissingName_ReturnsError()
    {
        var stderr = new StringWriter();
        var console = new TestCliConsole(error: stderr);
        var handler = new NewHandler(console);

        var code = await handler.ExecuteAsync(template: null, name: null, output: null, force: false, list: false, cancellationToken: CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Contains("Missing required option --name", stderr.ToString());
    }

    [Fact]
    public async Task Execute_UnknownTemplate_ReturnsError()
    {
        var stderr = new StringWriter();
        var console = new TestCliConsole(error: stderr);
        var handler = new NewHandler(console);

        var code = await handler.ExecuteAsync(template: "unknown", name: "MyModule", output: null, force: true, list: false, cancellationToken: CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Contains("Unknown template", stderr.ToString());
    }

    [Fact]
    public async Task Execute_ExistingDirectory_NonInteractive_RequiresForce()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"modulus-newhandler-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var existing = Path.Combine(tempRoot, "MyModule");
        Directory.CreateDirectory(existing);
        File.WriteAllText(Path.Combine(existing, "some.txt"), "x");

        try
        {
            var stderr = new StringWriter();
            var console = new TestCliConsole(error: stderr, isInputRedirected: true);
            var handler = new NewHandler(console);

            var code = await handler.ExecuteAsync(template: "module-avalonia", name: "MyModule", output: tempRoot, force: false, list: false, cancellationToken: CancellationToken.None);

            Assert.Equal(1, code);
            Assert.Contains("already exists", stderr.ToString());
            Assert.Contains("--force", stderr.ToString());
        }
        finally
        {
            try { Directory.Delete(tempRoot, recursive: true); } catch { /* ignore */ }
        }
    }
}


