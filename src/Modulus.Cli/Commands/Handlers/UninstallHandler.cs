using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core.Paths;
using Modulus.Infrastructure.Data;

namespace Modulus.Cli.Commands.Handlers;

/// <summary>
/// Handles module uninstallation logic. Can be used directly for testing.
/// </summary>
public class UninstallHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly string? _modulesDirectory;
    
    public UninstallHandler(IServiceProvider serviceProvider, string? modulesDirectory = null)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<UninstallHandler>>();
        _modulesDirectory = modulesDirectory;
    }
    
    /// <summary>
    /// Result of an uninstall operation.
    /// </summary>
    public record UninstallResult(bool Success, string Message);
    
    /// <summary>
    /// Uninstall a module by name or ID.
    /// </summary>
    public async Task<UninstallResult> ExecuteAsync(string module, bool force = false, TextWriter? output = null)
    {
        output ??= Console.Out;
        
        try
        {
            var dbContext = _serviceProvider.GetRequiredService<ModulusDbContext>();

            // Find module by name or ID
            var moduleEntity = await dbContext.Modules
                .FirstOrDefaultAsync(m => 
                    m.Id == module || 
                    m.DisplayName.ToLower() == module.ToLower());

            if (moduleEntity == null)
            {
                return new UninstallResult(false, $"Module '{module}' not found");
            }

            // Prevent uninstalling system modules
            if (moduleEntity.IsSystem)
            {
                return new UninstallResult(false, $"Cannot uninstall system module '{moduleEntity.DisplayName}'");
            }

            await output.WriteLineAsync($"Module: {moduleEntity.DisplayName}");
            await output.WriteLineAsync($"  Version: {moduleEntity.Version}");
            await output.WriteLineAsync($"  ID: {moduleEntity.Id}");

            // For non-force mode, we'd normally prompt - but in handler we require force or return error
            if (!force)
            {
                return new UninstallResult(false, "Confirmation required. Use --force to skip confirmation.");
            }

            // Determine module directory
            var modulesRoot = _modulesDirectory ?? Path.Combine(LocalStorage.GetUserRoot(), "Modules");
            var moduleDir = Path.Combine(modulesRoot, moduleEntity.Id);

            // Delete from database
            await output.WriteLineAsync("Removing module from database...");
            
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
                await output.WriteLineAsync($"Deleting files: {moduleDir}");
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
                        await output.WriteLineAsync($"Deleting files: {manifestDir}");
                        Directory.Delete(manifestDir, true);
                    }
                }
            }

            await output.WriteLineAsync();
            await output.WriteLineAsync($"✓ Module '{moduleEntity.DisplayName}' uninstalled successfully!");

            return new UninstallResult(true, "Module uninstalled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uninstallation failed");
            return new UninstallResult(false, ex.Message);
        }
    }
}

