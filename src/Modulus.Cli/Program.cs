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
        rootCommand.Add(NewCommand.Create());
        rootCommand.Add(BuildCommand.Create());
        rootCommand.Add(PackCommand.Create());
        rootCommand.Add(InstallCommand.Create());
        rootCommand.Add(UninstallCommand.Create());
        rootCommand.Add(ListCommand.Create());

        var config = new CommandLineConfiguration(rootCommand);
        return await config.InvokeAsync(args);
    }
}
