using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core.Data;
using Modulus.Core.Installation;
using Modulus.Core.Manifest;
using Modulus.Core.Paths;
using Modulus.Infrastructure.Data;
using Modulus.Infrastructure.Data.Repositories;

namespace Modulus.Cli.Services;

/// <summary>
/// Configures dependency injection for CLI commands.
/// </summary>
public static class CliServiceProvider
{
    /// <summary>
    /// Creates a configured service provider for CLI operations.
    /// </summary>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <param name="databasePath">Custom database path. If null, uses default location.</param>
    /// <param name="modulesDirectory">Custom modules directory. If null, uses default location.</param>
    public static ServiceProvider Build(
        bool verbose = false,
        string? databasePath = null,
        string? modulesDirectory = null)
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
            builder.AddConsole(options =>
            {
                options.FormatterName = "simple";
            });
        });

        // Database
        var effectiveDatabasePath = databasePath ?? DatabaseServiceExtensions.GetDefaultDatabasePath("Modulus");
        services.AddModulusDatabase(effectiveDatabasePath);

        // Store custom modules directory for installer service
        var effectiveModulesDirectory = modulesDirectory ?? Path.Combine(LocalStorage.GetUserRoot(), "Modules");
        services.AddSingleton(new CliConfiguration
        {
            ModulesDirectory = effectiveModulesDirectory
        });

        // Repositories
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IPendingCleanupRepository, PendingCleanupRepository>();

        // Services
        services.AddScoped<IManifestValidator, DefaultManifestValidator>();
        services.AddSingleton<IModuleCleanupService, ModuleCleanupService>();
        services.AddScoped<IModuleInstallerService, ModuleInstallerService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Ensures the database is migrated to the latest version.
    /// </summary>
    public static async Task EnsureMigratedAsync(ServiceProvider provider)
    {
        var dbContext = provider.GetRequiredService<ModulusDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}

/// <summary>
/// Configuration for CLI services, supporting test isolation.
/// </summary>
public class CliConfiguration
{
    /// <summary>
    /// Directory where modules are installed.
    /// </summary>
    public string ModulesDirectory { get; init; } = string.Empty;
}

