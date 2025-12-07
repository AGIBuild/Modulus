using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modulus.Infrastructure.Data;
using Modulus.Infrastructure.Data.Models;

namespace Modulus.Core.Data;

/// <summary>
/// EF Core implementation of the application database.
/// Uses Infrastructure.Data.ModulusDbContext for unified persistence.
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

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Database initialization (migrations) is handled by ModulusApplicationFactory
        _logger.LogInformation("AppDatabase initialized");
        return Task.CompletedTask;
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
            _context.AppSettings.Add(new AppSettingEntity
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
}
