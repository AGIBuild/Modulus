using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.Services;

namespace Modulus.Cli.Commands;

/// <summary>
/// Uninstall command: modulus uninstall &lt;module&gt;
/// </summary>
public static class UninstallCommand
{
    public static Command Create(IServiceProvider services)
    {
        var moduleArg = new Argument<string>("module") { Description = "Module name or ID (GUID) to uninstall" };
        var forceOption = new Option<bool>("--force", "-f") { Description = "Skip confirmation prompt" };
        var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Show detailed output" };

        var command = new Command("uninstall", "Uninstall a module");
        command.Arguments.Add(moduleArg);
        command.Options.Add(forceOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var module = parseResult.GetValue(moduleArg)!;
            var force = parseResult.GetValue(forceOption);
            var verbose = parseResult.GetValue(verboseOption);

            await CliServiceProvider.EnsureMigratedAsync(services);

            var console = services.GetRequiredService<ICliConsole>();
            var config = services.GetRequiredService<CliConfiguration>();
            var handler = new UninstallHandler(services, config.ModulesDirectory);

            var result = await handler.ExecuteAsync(module, force, console.Out);
            if (!result.Success)
            {
                console.Error.WriteLine($"Error: {result.Message}");
                return 1;
            }

            if (verbose)
            {
                // Keep verbose flag for future extensions.
            }

            console.Out.WriteLine();
            console.Out.WriteLine("Note: Restart the Modulus host application to complete the removal.");
            return 0;
        });

        return command;
    }
}


