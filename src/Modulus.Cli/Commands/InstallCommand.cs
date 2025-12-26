using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.Services;

namespace Modulus.Cli.Commands;

/// <summary>
/// Install command: modulus install &lt;source&gt;
/// </summary>
public static class InstallCommand
{
    public static Command Create(IServiceProvider services)
    {
        var sourceArg = new Argument<string>("source") { Description = "Path to .modpkg file or module directory" };
        var forceOption = new Option<bool>("--force", "-f") { Description = "Overwrite existing installation without prompting" };
        var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Show detailed output" };

        var command = new Command("install", "Install a module from a .modpkg file or directory");
        command.Arguments.Add(sourceArg);
        command.Options.Add(forceOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var source = parseResult.GetValue(sourceArg)!;
            var force = parseResult.GetValue(forceOption);
            var verbose = parseResult.GetValue(verboseOption);

            await CliServiceProvider.EnsureMigratedAsync(services);

            var console = services.GetRequiredService<ICliConsole>();
            var config = services.GetRequiredService<CliConfiguration>();
            var handler = new InstallHandler(services, config.ModulesDirectory);

            var result = await handler.ExecuteAsync(source, force, console.Out);
            if (!result.Success)
            {
                console.Error.WriteLine($"Error: {result.Message}");
                return 1;
            }

            if (verbose)
            {
                // Keep verbose flag for future extensions; handler already writes output details.
            }

            console.Out.WriteLine();
            console.Out.WriteLine("Note: Restart the Modulus host application to load the module.");
            return 0;
        });

        return command;
    }
}


