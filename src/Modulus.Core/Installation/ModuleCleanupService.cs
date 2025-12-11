using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;

namespace Modulus.Core.Installation;

/// <summary>
/// Manages pending module directory cleanups using database storage.
/// Handles cases where DLLs are locked during module unload and schedules cleanup for next startup.
/// </summary>
public interface IModuleCleanupService
{
    /// <summary>
    /// Schedules a directory for cleanup. Will attempt immediate cleanup first,
    /// falling back to scheduled cleanup on next startup if files are locked.
    /// </summary>
    /// <param name="directoryPath">Directory to clean up</param>
    /// <param name="moduleId">Optional module ID for tracking</param>
    Task ScheduleCleanupAsync(string directoryPath, string? moduleId = null);

    /// <summary>
    /// Processes all pending cleanups. Should be called during application startup.
    /// Does not block application startup on failures.
    /// </summary>
    Task ProcessPendingCleanupsAsync();

    /// <summary>
    /// Attempts to delete a directory with retry logic and GC collection.
    /// </summary>
    Task<bool> TryDeleteDirectoryAsync(string directoryPath, int maxRetries = 3, int delayMs = 500);

    /// <summary>
    /// Cancels any pending cleanup for a directory.
    /// Should be called when a module is reinstalled to prevent accidental deletion.
    /// </summary>
    Task CancelCleanupAsync(string directoryPath);

    /// <summary>
    /// Cancels any pending cleanup for a module.
    /// Should be called when a module is reinstalled to prevent accidental deletion.
    /// </summary>
    Task CancelCleanupByModuleIdAsync(string moduleId);
}

public class ModuleCleanupService : IModuleCleanupService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModuleCleanupService> _logger;
    private const int MaxRetryCountBeforeGiveUp = 10;

    public ModuleCleanupService(IServiceProvider serviceProvider, ILogger<ModuleCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task ScheduleCleanupAsync(string directoryPath, string? moduleId = null)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        // First, try immediate cleanup with retries
        if (await TryDeleteDirectoryAsync(directoryPath, maxRetries: 3, delayMs: 500))
        {
            _logger.LogDebug("Immediately cleaned up directory: {Path}", directoryPath);
            return;
        }

        // If immediate cleanup fails, schedule for next startup via database
        _logger.LogInformation("Scheduling directory for cleanup on next startup: {Path}", directoryPath);
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetService<IPendingCleanupRepository>();
            if (repository != null)
            {
                await repository.AddAsync(new PendingCleanupEntity
                {
                    DirectoryPath = directoryPath,
                    ModuleId = moduleId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("IPendingCleanupRepository not available, cleanup will not be persisted.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to schedule cleanup for directory: {Path}", directoryPath);
        }
    }

    public async Task ProcessPendingCleanupsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetService<IPendingCleanupRepository>();
            if (repository == null)
            {
                _logger.LogDebug("IPendingCleanupRepository not available, skipping pending cleanups.");
                return;
            }

            var pendingCleanups = await repository.GetAllAsync();
            if (pendingCleanups.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Processing {Count} pending directory cleanups...", pendingCleanups.Count);

            var successCount = 0;
            var failCount = 0;

            foreach (var cleanup in pendingCleanups)
            {
                // Skip if directory no longer exists
                if (!Directory.Exists(cleanup.DirectoryPath))
                {
                    _logger.LogDebug("Directory no longer exists, removing from pending: {Path}", cleanup.DirectoryPath);
                    await repository.RemoveAsync(cleanup.Id);
                    continue;
                }

                // Skip if too many retries (likely a persistent issue)
                if (cleanup.RetryCount >= MaxRetryCountBeforeGiveUp)
                {
                    _logger.LogWarning(
                        "Directory {Path} has failed cleanup {RetryCount} times, skipping. Manual cleanup may be required.",
                        cleanup.DirectoryPath, cleanup.RetryCount);
                    continue;
                }

                // Attempt cleanup
                if (await TryDeleteDirectoryAsync(cleanup.DirectoryPath, maxRetries: 2, delayMs: 100))
                {
                    _logger.LogInformation("Cleaned up pending directory: {Path}", cleanup.DirectoryPath);
                    await repository.RemoveAsync(cleanup.Id);
                    successCount++;
                }
                else
                {
                    _logger.LogDebug("Still unable to clean up directory, will retry next startup: {Path}", cleanup.DirectoryPath);
                    await repository.UpdateRetryAsync(cleanup.Id);
                    failCount++;
                }
            }

            if (successCount > 0 || failCount > 0)
            {
                _logger.LogInformation("Pending cleanup results: {Success} succeeded, {Failed} failed/deferred.", successCount, failCount);
            }
        }
        catch (Exception ex)
        {
            // Don't block startup on cleanup failures
            _logger.LogWarning(ex, "Error processing pending cleanups. Application startup will continue.");
        }
    }

    public async Task<bool> TryDeleteDirectoryAsync(string directoryPath, int maxRetries = 3, int delayMs = 500)
    {
        if (!Directory.Exists(directoryPath))
        {
            return true;
        }

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // Force GC to release any lingering references
                if (attempt > 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    await Task.Delay(delayMs * (attempt + 1)); // Exponential backoff
                }

                Directory.Delete(directoryPath, recursive: true);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogDebug("Delete attempt {Attempt}/{MaxRetries} failed (access denied): {Path}", 
                    attempt + 1, maxRetries, directoryPath);
            }
            catch (IOException)
            {
                _logger.LogDebug("Delete attempt {Attempt}/{MaxRetries} failed (in use): {Path}", 
                    attempt + 1, maxRetries, directoryPath);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Delete attempt {Attempt}/{MaxRetries} failed: {Path}", 
                    attempt + 1, maxRetries, directoryPath);
            }

            if (attempt < maxRetries - 1)
            {
                await Task.Delay(delayMs);
            }
        }

        return false;
    }

    public async Task CancelCleanupAsync(string directoryPath)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetService<IPendingCleanupRepository>();
            if (repository != null)
            {
                await repository.RemoveByPathAsync(directoryPath);
                _logger.LogDebug("Cancelled pending cleanup for: {Path}", directoryPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel cleanup for directory: {Path}", directoryPath);
        }
    }

    public async Task CancelCleanupByModuleIdAsync(string moduleId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetService<IPendingCleanupRepository>();
            if (repository != null)
            {
                await repository.RemoveByModuleIdAsync(moduleId);
                _logger.LogDebug("Cancelled pending cleanup for module: {ModuleId}", moduleId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel cleanup for module: {ModuleId}", moduleId);
        }
    }

}
