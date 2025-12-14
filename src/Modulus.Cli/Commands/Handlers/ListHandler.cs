using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Infrastructure.Data;
using Modulus.Infrastructure.Data.Models;

namespace Modulus.Cli.Commands.Handlers;

/// <summary>
/// Handles module listing logic. Can be used directly for testing.
/// </summary>
public class ListHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    
    public ListHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<ListHandler>>();
    }
    
    /// <summary>
    /// Result of a list operation.
    /// </summary>
    public record ListResult(bool Success, string Message, IReadOnlyList<ModuleInfo> Modules);
    
    /// <summary>
    /// Information about an installed module.
    /// </summary>
    public record ModuleInfo(
        string Id,
        string DisplayName,
        string Version,
        string? Publisher,
        string? Description,
        string? Path,
        bool IsEnabled,
        bool IsSystem,
        ModuleState State,
        DateTime? ValidatedAt);
    
    /// <summary>
    /// List all installed modules.
    /// </summary>
    public async Task<ListResult> ExecuteAsync(bool verbose = false, TextWriter? output = null)
    {
        output ??= Console.Out;
        
        try
        {
            var dbContext = _serviceProvider.GetRequiredService<ModulusDbContext>();

            var modules = await dbContext.Modules
                .OrderBy(m => m.DisplayName)
                .ToListAsync();

            var moduleInfos = modules.Select(m => new ModuleInfo(
                m.Id,
                m.DisplayName,
                m.Version,
                m.Publisher,
                m.Description,
                m.Path,
                m.IsEnabled,
                m.IsSystem,
                m.State,
                m.ValidatedAt
            )).ToList();

            if (modules.Count == 0)
            {
                await output.WriteLineAsync("No modules installed.");
                return new ListResult(true, "No modules installed", moduleInfos);
            }

            await output.WriteLineAsync($"Installed modules ({modules.Count}):");
            await output.WriteLineAsync();

            foreach (var module in modules)
            {
                var status = module.IsEnabled ? "✓" : "○";
                var systemTag = module.IsSystem ? " [system]" : "";
                var stateTag = module.State != ModuleState.Ready 
                    ? $" [{module.State}]" 
                    : "";

                await output.WriteLineAsync($"  {status} {module.DisplayName} v{module.Version}{systemTag}{stateTag}");
                await output.WriteLineAsync($"      ID: {module.Id}");
                
                if (!string.IsNullOrEmpty(module.Publisher))
                {
                    await output.WriteLineAsync($"      Publisher: {module.Publisher}");
                }

                if (verbose)
                {
                    if (!string.IsNullOrEmpty(module.Description))
                    {
                        await output.WriteLineAsync($"      Description: {module.Description}");
                    }
                    if (!string.IsNullOrEmpty(module.Path))
                    {
                        await output.WriteLineAsync($"      Path: {module.Path}");
                    }
                    if (module.ValidatedAt.HasValue)
                    {
                        await output.WriteLineAsync($"      Validated: {module.ValidatedAt:yyyy-MM-dd HH:mm:ss}");
                    }
                }

                await output.WriteLineAsync();
            }

            return new ListResult(true, $"Found {modules.Count} modules", moduleInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list modules");
            return new ListResult(false, ex.Message, Array.Empty<ModuleInfo>());
        }
    }
}

