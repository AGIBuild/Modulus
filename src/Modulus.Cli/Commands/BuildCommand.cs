using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;

namespace Modulus.Cli.Commands;

/// <summary>
/// Build command: modulus build
/// Builds the module project in the current directory.
/// </summary>
public static class BuildCommand
{
    public static Command Create(IServiceProvider services)
    {
        var pathOption = new Option<string?>("--path", "-p") { Description = "Path to module project directory (default: current directory)" };
        var configurationOption = new Option<string?>("--configuration", "-c") { Description = "Build configuration (Debug/Release, default: Release)" };
        var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Show detailed build output" };

        var command = new Command("build", "Build the module project in the current directory");
        command.Options.Add(pathOption);
        command.Options.Add(configurationOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(pathOption);
            var configuration = parseResult.GetValue(configurationOption) ?? "Release";
            var verbose = parseResult.GetValue(verboseOption);
            var handler = services.GetRequiredService<BuildHandler>();
            var exitCode = await handler.ExecuteAsync(path, configuration, verbose, cancellationToken);
            return exitCode;
        });

        return command;
    }
}
