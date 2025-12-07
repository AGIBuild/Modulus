using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Infrastructure.Data;

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

        // Register unified DbContext (Infrastructure.Data.ModulusDbContext)
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
    /// <param name="databaseName">
    /// Optional database name (without extension). If null, resolves from
    /// environment variable MODULUS_DB_NAME, otherwise falls back to "Modulus".
    /// </param>
    public static string GetDefaultDatabasePath(string? databaseName = null)
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var resolvedName = databaseName
            ?? Environment.GetEnvironmentVariable("MODULUS_DB_NAME")
            ?? "Modulus";

        var sanitizedName = string.IsNullOrWhiteSpace(resolvedName) ? "Modulus" : resolvedName;
        return Path.Combine(appDataPath, "Modulus", $"{sanitizedName}.db");
    }
}

