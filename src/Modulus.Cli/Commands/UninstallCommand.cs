using System.CommandLine;
using System.CommandLine.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Cli.Services;
using Modulus.Core.Paths;
using Modulus.Infrastructure.Data;

namespace Modulus.Cli.Commands;

/// <summary>
/// Uninstall command: modulus uninstall &lt;module&gt;
/// </summary>
public static class UninstallCommand
{
    public static Command Create()
    {
        var moduleArg = new Argument<string>("module")
        {
            Description = "Module name or ID (GUID) to uninstall"
        };
        
        var forceOption = new Option<bool>("--force", "-f")
        {
            Description = "Skip confirmation prompt"
        };
        
        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Show detailed output"
        };

        var command = new Command("uninstall", "Uninstall a module");
        command.Add(moduleArg);
        command.Add(forceOption);
        command.Add(verboseOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var module = parseResult.GetValue(moduleArg)!;
            var force = parseResult.GetValue(forceOption);
            var verbose = parseResult.GetValue(verboseOption);
            await HandleAsync(module, force, verbose);
        });
        
        return command;
    }

    private static async Task HandleAsync(string module, bool force, bool verbose)
    {
        using var provider = CliServiceProvider.Build(verbose);
        var logger = provider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Ensure database is migrated
            await CliServiceProvider.EnsureMigratedAsync(provider);

            var dbContext = provider.GetRequiredService<ModulusDbContext>();

            // Find module by name or ID
            var moduleEntity = await dbContext.Modules
                .FirstOrDefaultAsync(m => 
                    m.Id == module || 
                    m.Name.ToLower() == module.ToLower());

            if (moduleEntity == null)
            {
                Console.WriteLine($"Error: Module '{module}' not found");
                return;
            }

            // Prevent uninstalling system modules
            if (moduleEntity.IsSystem)
            {
                Console.WriteLine($"Error: Cannot uninstall system module '{moduleEntity.Name}'");
                return;
            }

            Console.WriteLine($"Module: {moduleEntity.Name}");
            Console.WriteLine($"  Version: {moduleEntity.Version}");
            Console.WriteLine($"  ID: {moduleEntity.Id}");

            // Confirm uninstall
            if (!force)
            {
                Console.Write($"Are you sure you want to uninstall '{moduleEntity.Name}'? [y/N]: ");
                var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (response != "y" && response != "yes")
                {
                    Console.WriteLine("Uninstallation cancelled.");
                    return;
                }
            }

            // Determine module directory
            var modulesRoot = Path.Combine(LocalStorage.GetUserRoot(), "Modules");
            var moduleDir = Path.Combine(modulesRoot, moduleEntity.Id);

            // Delete from database
            Console.WriteLine("Removing module from database...");
            
            // Remove associated menus first
            var menus = await dbContext.Menus
                .Where(m => m.ModuleId == moduleEntity.Id)
                .ToListAsync();
            dbContext.Menus.RemoveRange(menus);
            
            // Remove module
            dbContext.Modules.Remove(moduleEntity);
            await dbContext.SaveChangesAsync();

            // Delete files
            if (Directory.Exists(moduleDir))
            {
                Console.WriteLine($"Deleting files: {moduleDir}");
                Directory.Delete(moduleDir, true);
            }
            else
            {
                // Try to find the module directory from the stored path
                if (!string.IsNullOrEmpty(moduleEntity.Path))
                {
                    var manifestDir = Path.GetDirectoryName(moduleEntity.Path);
                    if (!string.IsNullOrEmpty(manifestDir) && Directory.Exists(manifestDir))
                    {
                        Console.WriteLine($"Deleting files: {manifestDir}");
                        Directory.Delete(manifestDir, true);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"✓ Module '{moduleEntity.Name}' uninstalled successfully!");
            Console.WriteLine();
            Console.WriteLine("Note: Restart the Modulus host application to complete the removal.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Uninstallation failed");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
