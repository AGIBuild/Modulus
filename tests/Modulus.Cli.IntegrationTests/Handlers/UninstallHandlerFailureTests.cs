using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;

namespace Modulus.Cli.IntegrationTests.Handlers;

public class UninstallHandlerFailureTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDbContextMissing_ReturnsFailure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        using var sp = services.BuildServiceProvider();

        var handler = new UninstallHandler(sp);
        var result = await handler.ExecuteAsync("anything", force: true, output: new StringWriter());

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }
}


