using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;
using Modulus.Core.Installation;
using Modulus.Core.Logging;
using Modulus.Core.Manifest;
using Modulus.Infrastructure.Data;
using Modulus.Infrastructure.Data.Models;
using Modulus.Infrastructure.Data.Repositories;
using Modulus.Sdk;

namespace Modulus.Core.Runtime;

public static class ModulusApplicationFactory
{
    public static async Task<ModulusApplication> CreateAsync<TStartupModule>(
        IServiceCollection services,
        IEnumerable<IModuleProvider> moduleProviders,
        string? hostType = null,
        string? databasePath = null,
        IConfiguration? configuration = null,
        ILoggerFactory? loggerFactory = null)
        where TStartupModule : IModule, new()
    {
        // 1. Setup Runtime Components
        var runtimeContext = new RuntimeContext();
        if (hostType != null)
        {
            runtimeContext.SetCurrentHost(hostType);
        }

        var effectiveConfig = configuration ?? new ConfigurationBuilder()
            .Build();

        loggerFactory ??= ModulusLogging.CreateLoggerFactory(effectiveConfig, hostType ?? "Host");
        var logger = loggerFactory.CreateLogger<ModulusApplication>();

        using var hostScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["HostType"] = hostType ?? "UnknownHost"
        });

        var signatureVerifier = new Sha256ManifestSignatureVerifier(loggerFactory.CreateLogger<Sha256ManifestSignatureVerifier>());
        var manifestValidator = new DefaultManifestValidator(signatureVerifier, loggerFactory.CreateLogger<DefaultManifestValidator>());
        var sharedAssemblies = SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        var moduleLoader = new ModuleLoader(runtimeContext, manifestValidator, sharedAssemblies, loggerFactory.CreateLogger<ModuleLoader>(), loggerFactory);
        var moduleManager = new ModuleManager(loggerFactory.CreateLogger<ModuleManager>());

        // 2. Setup Temporary Services for DB & Seeding (shared logger factory)
        var tempServices = new ServiceCollection();
        ModulusLogging.AddLoggerFactory(tempServices, loggerFactory);
        tempServices.AddSingleton<ISharedAssemblyCatalog>(sharedAssemblies);
        tempServices.AddSingleton<IManifestValidator>(manifestValidator);

        // Use Sqlite default for now
        var connectionString = string.IsNullOrWhiteSpace(databasePath) ? "Data Source=modulus.db" : $"Data Source={databasePath}";
        tempServices.AddDbContext<ModulusDbContext>(options => options.UseSqlite(connectionString));

        tempServices.AddScoped<IModuleRepository, ModuleRepository>();
        tempServices.AddScoped<IMenuRepository, MenuRepository>();
        tempServices.AddScoped<IModuleInstallerService, ModuleInstallerService>();
        tempServices.AddScoped<SystemModuleSeeder>();
        tempServices.AddScoped<ModuleIntegrityChecker>();

        using (var sp = tempServices.BuildServiceProvider())
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ModulusDbContext>();
            await db.Database.MigrateAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<SystemModuleSeeder>();

            // Seed All Modules (system flag determined by provider.IsSystemSource)
            if (moduleProviders != null)
            {
                foreach (var provider in moduleProviders)
                {
                    var paths = await provider.GetModulePackagesAsync();
                    foreach (var path in paths)
                    {
                        // Pass isSystem flag and hostType from provider to seeder
                        await seeder.SeedFromPathAsync(path, provider.IsSystemSource, hostType);
                    }
                }
            }

            // Ensure all changes are persisted before querying
            await db.SaveChangesAsync();

            // Integrity Check
            var checker = scope.ServiceProvider.GetRequiredService<ModuleIntegrityChecker>();
            await checker.CheckAsync();

            // 3. Load Enabled Modules (use fresh query to see seeded data)
            var enabledModules = await db.Modules
                .AsNoTracking()
                .Where(m => m.IsEnabled)
                .ToListAsync();

            logger.LogInformation("Found {Count} enabled modules to load.", enabledModules.Count);

            // 3.1 Order Modules
            var orderedModules = await OrderModulesAsync(enabledModules, runtimeContext, logger);

            // 3.2 Load Loop
            foreach (var module in orderedModules)
            {
                // Skip built-in host modules (they have no physical path)
                if (module.Path == "built-in")
                {
                    logger.LogDebug("Skipping built-in module {ModuleId}.", module.Id);
                    continue;
                }

                try
                {
                    // Resolve absolute path (module.Path is stored as manifest.json path)
                    var manifestPath = Path.GetFullPath(module.Path);
                    var packagePath = Path.GetDirectoryName(manifestPath);

                    if (packagePath != null)
                    {
                        logger.LogInformation("Loading module {ModuleName} ({ModuleId}) from {Path}...", module.Name, module.Id, packagePath);
                        // Skip module initialization - it will be done after host services are bound
                        var descriptor = await moduleLoader.LoadAsync(packagePath, module.IsSystem, skipModuleInitialization: true).ConfigureAwait(false);
                        if (descriptor == null)
                        {
                            logger.LogWarning("Module {ModuleName} ({ModuleId}) failed to load.", module.Name, module.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load module {ModuleName} ({ModuleId}) from {Path}", module.Name, module.Id, module.Path);
                }
            }
        }

        // 4. Register Host Startup Module to Manager
        // Note: Package-loaded modules are managed via RuntimeModuleHandle.ModuleInstances
        // and initialized through IHostAwareModuleLoader.InitializeLoadedModulesAsync().
        // Only the host startup module goes through ModuleManager to avoid double initialization.
        moduleManager.AddModule(new TStartupModule());

        // 5. Register Services to FINAL ServiceCollection
        ModulusLogging.AddLoggerFactory(services, loggerFactory);
        services.AddSingleton(runtimeContext);
        services.AddSingleton<IModuleLoader>(moduleLoader);
        services.AddSingleton(moduleManager);
        services.AddSingleton<ISharedAssemblyCatalog>(sharedAssemblies);
        services.AddSingleton<IManifestValidator>(manifestValidator);

        var app = new ModulusApplication(services, moduleManager, logger);
        app.ConfigureServices();

        return app;
    }

    private static async Task<IReadOnlyList<ModuleEntity>> OrderModulesAsync(
        IEnumerable<ModuleEntity> modules, 
        RuntimeContext runtimeContext, 
        ILogger logger)
    {
        var moduleList = modules.ToList();
        var moduleDict = moduleList.ToDictionary(m => m.Id);
        
        var registrations = new List<SortItem>();

        foreach (var module in moduleList)
        {
            var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            try 
            {
                var manifestPath = Path.GetFullPath(module.Path);
                if (File.Exists(manifestPath))
                {
                    var manifest = await ManifestReader.ReadFromFileAsync(manifestPath);
                    
                    if (manifest != null)
                    {
                        foreach (var depId in manifest.Dependencies.Keys)
                        {
                            if (moduleDict.ContainsKey(depId))
                            {
                                deps.Add(depId);
                            }
                            else if (!runtimeContext.TryGetModule(depId, out _))
                            {
                                 // Warn but don't crash
                                 logger.LogDebug("Module {ModuleId} depends on {DepId} which is not in current load list.", module.Id, depId);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                logger.LogWarning("Could not read manifest for dependency checking: {Path}", module.Path);
            }

            registrations.Add(new SortItem(module, module.Id, deps));
        }

        var sorted = ModuleDependencyResolver.TopologicallySort(
            registrations,
            r => r.Id,
            r => r.Dependencies,
            logger);

        return sorted.Select(r => r.Entity).ToList();
    }

    private sealed record SortItem(ModuleEntity Entity, string Id, HashSet<string> Dependencies);
}
