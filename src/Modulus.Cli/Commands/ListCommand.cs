using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Cli.Services;
using Modulus.Infrastructure.Data;

namespace Modulus.Cli.Commands;

/// <summary>
/// List command: modulus list
/// </summary>
public static class ListCommand
{
    public static Command Create()
    {
        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Show detailed information (path, install time)"
        };

        var command = new Command("list", "List installed modules");
        command.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var verbose = parseResult.GetValue(verboseOption);
            await HandleAsync(verbose);
        });
        
        return command;
    }

    private static async Task HandleAsync(bool verbose)
    {
        using var provider = CliServiceProvider.Build(verbose: false);
        var logger = provider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Ensure database is migrated
            await CliServiceProvider.EnsureMigratedAsync(provider);

            var dbContext = provider.GetRequiredService<ModulusDbContext>();

            var modules = await dbContext.Modules
                .OrderBy(m => m.Name)
                .ToListAsync();

            if (modules.Count == 0)
            {
                Console.WriteLine("No modules installed.");
                return;
            }

            Console.WriteLine($"Installed modules ({modules.Count}):");
            Console.WriteLine();

            foreach (var module in modules)
            {
                var status = module.IsEnabled ? "✓" : "○";
                var systemTag = module.IsSystem ? " [system]" : "";
                var stateTag = module.State != Infrastructure.Data.Models.ModuleState.Ready 
                    ? $" [{module.State}]" 
                    : "";

                Console.WriteLine($"  {status} {module.Name} v{module.Version}{systemTag}{stateTag}");
                Console.WriteLine($"      ID: {module.Id}");
                
                if (!string.IsNullOrEmpty(module.Author))
                {
                    Console.WriteLine($"      Author: {module.Author}");
                }

                if (verbose)
                {
                    if (!string.IsNullOrEmpty(module.Description))
                    {
                        Console.WriteLine($"      Description: {module.Description}");
                    }
                    if (!string.IsNullOrEmpty(module.Path))
                    {
                        Console.WriteLine($"      Path: {module.Path}");
                    }
                    if (module.ValidatedAt.HasValue)
                    {
                        Console.WriteLine($"      Validated: {module.ValidatedAt:yyyy-MM-dd HH:mm:ss}");
                    }
                }

                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list modules");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
