using System.CommandLine;
using Modulus.Cli.Commands;
using Modulus.Cli.Commands.Handlers;
using Modulus.Cli.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Modulus.Cli;

/// <summary>
/// Modulus CLI entry point.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var pathOverrides = CliPathOverrides.FromEnvironment();

        var services = new ServiceCollection();
        services.AddCliServices(
            verbose: false,
            databasePath: pathOverrides.DatabasePath,
            modulesDirectory: pathOverrides.ModulesDirectory);

        services.AddSingleton<ICliConsole, SystemCliConsole>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();

        // Handlers (Command -> Handler architecture)
        services.AddTransient<NewHandler>();
        services.AddTransient<BuildHandler>();
        services.AddTransient<PackHandler>();

        using var provider = services.BuildServiceProvider();

        var rootCommand = new RootCommand("Modulus module management CLI");

        // Add commands
        rootCommand.Subcommands.Add(NewCommand.Create(provider));
        rootCommand.Subcommands.Add(BuildCommand.Create(provider));
        rootCommand.Subcommands.Add(PackCommand.Create(provider));
        rootCommand.Subcommands.Add(InstallCommand.Create(provider));
        rootCommand.Subcommands.Add(UninstallCommand.Create(provider));
        rootCommand.Subcommands.Add(ListCommand.Create(provider));

        return await rootCommand.Parse(args).InvokeAsync();
    }
}
