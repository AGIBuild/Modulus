using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Modulus.Core.Data;

/// <summary>
/// Extension methods for registering database services.
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Adds SQLite database services using EF Core to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddModulusDatabase(this IServiceCollection services, string databasePath)
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Register DbContext
        services.AddDbContext<ModulusDbContext>(options =>
        {
            options.UseSqlite($"Data Source={databasePath}");
        }, ServiceLifetime.Singleton);

        // Register database service
        services.AddSingleton<IAppDatabase, EfAppDatabase>();
        services.AddSingleton<ISettingsService, SettingsService>();

        return services;
    }

    /// <summary>
    /// Gets the default database path for the application.
    /// </summary>
    public static string GetDefaultDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "Modulus", "modulus.db");
    }
}

