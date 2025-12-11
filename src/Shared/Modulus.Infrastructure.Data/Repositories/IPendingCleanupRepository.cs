using Modulus.Infrastructure.Data.Models;

namespace Modulus.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for managing pending directory cleanups.
/// </summary>
public interface IPendingCleanupRepository
{
    /// <summary>
    /// Gets all pending cleanups.
    /// </summary>
    Task<IReadOnlyList<PendingCleanupEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a pending cleanup record.
    /// </summary>
    Task AddAsync(PendingCleanupEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a pending cleanup by ID.
    /// </summary>
    Task RemoveAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all pending cleanups for a specific directory path.
    /// </summary>
    Task RemoveByPathAsync(string directoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all pending cleanups for a specific module ID.
    /// Called when a module is reinstalled to prevent accidental deletion.
    /// </summary>
    Task RemoveByModuleIdAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates retry count and last attempt time for a cleanup.
    /// </summary>
    Task UpdateRetryAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a directory path has a pending cleanup.
    /// </summary>
    Task<bool> ExistsAsync(string directoryPath, CancellationToken cancellationToken = default);
}

