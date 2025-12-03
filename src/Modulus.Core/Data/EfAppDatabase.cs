using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modulus.Core.Data.Entities;

namespace Modulus.Core.Data;

/// <summary>
/// EF Core implementation of the application database.
/// </summary>
public class EfAppDatabase : IAppDatabase
{
    private readonly ModulusDbContext _context;
    private readonly ILogger<EfAppDatabase> _logger;

    public EfAppDatabase(ModulusDbContext context, ILogger<EfAppDatabase> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // EnsureCreated will create the database and tables if they don't exist
        await _context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Database initialized");
    }

    #region App Settings

    public async Task<string?> GetSettingAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _context.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
            .ConfigureAwait(false);
        return setting?.Value;
    }

    public async Task SetSettingAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AppSettings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken)
            .ConfigureAwait(false);
    }

    #endregion

    #region Installed Modules

    public async Task<IReadOnlyList<InstalledModule>> GetInstalledModulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InstalledModules
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<InstalledModule?> GetInstalledModuleAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        return await _context.InstalledModules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpsertInstalledModuleAsync(InstalledModule module, CancellationToken cancellationToken = default)
    {
        var existing = await _context.InstalledModules
            .FirstOrDefaultAsync(m => m.Id == module.Id, cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
        {
            existing.DisplayName = module.DisplayName;
            existing.Version = module.Version;
            existing.PackagePath = module.PackagePath;
            existing.IsEnabled = module.IsEnabled;
            existing.IsSystem = module.IsSystem;
            existing.LastLoadedAt = module.LastLoadedAt;
        }
        else
        {
            _context.InstalledModules.Add(module);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteInstalledModuleAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        var module = await _context.InstalledModules
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken)
            .ConfigureAwait(false);

        if (module != null)
        {
            _context.InstalledModules.Remove(module);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task UpdateModuleEnabledStateAsync(string moduleId, bool isEnabled, CancellationToken cancellationToken = default)
    {
        var module = await _context.InstalledModules
            .FirstOrDefaultAsync(m => m.Id == moduleId, cancellationToken)
            .ConfigureAwait(false);

        if (module != null)
        {
            module.IsEnabled = isEnabled;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion
}

