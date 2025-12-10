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
    public static ServiceProvider Build(bool verbose = false)
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
        var databasePath = DatabaseServiceExtensions.GetDefaultDatabasePath("Modulus");
        services.AddModulusDatabase(databasePath);

        // Repositories
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();

        // Services
        services.AddScoped<IManifestValidator, DefaultManifestValidator>();
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

