using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.Services;

namespace Modulus.Cli.Commands;

/// <summary>
/// List command: modulus list
/// </summary>
public static class ListCommand
{
    public static Command Create(IServiceProvider services)
    {
        var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Show detailed information (path, install time)" };

        var command = new Command("list", "List installed modules");
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var verbose = parseResult.GetValue(verboseOption);

            await CliServiceProvider.EnsureMigratedAsync(services);

            var console = services.GetRequiredService<ICliConsole>();
            var handler = new ListHandler(services);
            var result = await handler.ExecuteAsync(verbose, console.Out);

            if (!result.Success)
            {
                console.Error.WriteLine($"Error: {result.Message}");
                return 1;
            }

            return 0;
        });

        return command;
    }
}


