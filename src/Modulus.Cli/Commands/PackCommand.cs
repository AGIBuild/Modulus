using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Commands.Handlers;

namespace Modulus.Cli.Commands;

/// <summary>
/// Pack command: modulus pack
/// Builds and packages the module into a .modpkg file for distribution.
/// </summary>
public static class PackCommand
{
    public static Command Create(IServiceProvider services)
    {
        var pathOption = new Option<string?>("--path", "-p") { Description = "Path to module project directory (default: current directory)" };
        var outputOption = new Option<string?>("--output", "-o") { Description = "Output directory for .modpkg file (default: ./output)" };
        var configurationOption = new Option<string?>("--configuration", "-c") { Description = "Build configuration (Debug/Release, default: Release)" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building the project (use existing build output)" };
        var verboseOption = new Option<bool>("--verbose", "-v") { Description = "Show detailed output" };

        var command = new Command("pack", "Build and package the module into a .modpkg file");
        command.Options.Add(pathOption);
        command.Options.Add(outputOption);
        command.Options.Add(configurationOption);
        command.Options.Add(noBuildOption);
        command.Options.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(pathOption);
            var output = parseResult.GetValue(outputOption);
            var configuration = parseResult.GetValue(configurationOption) ?? "Release";
            var noBuild = parseResult.GetValue(noBuildOption);
            var verbose = parseResult.GetValue(verboseOption);

            var handler = services.GetRequiredService<PackHandler>();
            return await handler.ExecuteAsync(path, output, configuration, noBuild, verbose, cancellationToken);
        });

        return command;
    }
}


