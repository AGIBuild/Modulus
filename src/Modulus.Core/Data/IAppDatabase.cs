using Modulus.Core.Data.Entities;

namespace Modulus.Core.Data;

/// <summary>
/// Interface for application database operations.
/// </summary>
public interface IAppDatabase
{
    /// <summary>
    /// Initializes the database (creates tables if not exist).
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    // App Settings
    Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default);
    Task SetSettingAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default);

    // Installed Modules
    Task<IReadOnlyList<InstalledModule>> GetInstalledModulesAsync(CancellationToken cancellationToken = default);
    Task<InstalledModule?> GetInstalledModuleAsync(string moduleId, CancellationToken cancellationToken = default);
    Task UpsertInstalledModuleAsync(InstalledModule module, CancellationToken cancellationToken = default);
    Task DeleteInstalledModuleAsync(string moduleId, CancellationToken cancellationToken = default);
    Task UpdateModuleEnabledStateAsync(string moduleId, bool isEnabled, CancellationToken cancellationToken = default);
}

