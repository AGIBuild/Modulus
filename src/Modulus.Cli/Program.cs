using System.CommandLine;
using Modulus.Cli.Commands;

namespace Modulus.Cli;

/// <summary>
/// Modulus CLI entry point.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Modulus module management CLI");

        // Add commands
        rootCommand.Subcommands.Add(NewCommand.Create());
        rootCommand.Subcommands.Add(BuildCommand.Create());
        rootCommand.Subcommands.Add(PackCommand.Create());
        rootCommand.Subcommands.Add(InstallCommand.Create());
        rootCommand.Subcommands.Add(UninstallCommand.Create());
        rootCommand.Subcommands.Add(ListCommand.Create());

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
