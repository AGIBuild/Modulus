using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Modulus.Core.Architecture;
using Modulus.Core.Installation;
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
        string? databasePath = null) 
        where TStartupModule : IModule, new()
    {
        // 1. Setup Runtime Components
        var runtimeContext = new RuntimeContext();
        if (hostType != null) 
        {
            runtimeContext.SetCurrentHost(hostType);
        }
        
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ModulusApplication>();
        
        var signatureVerifier = new Sha256ManifestSignatureVerifier(NullLogger<Sha256ManifestSignatureVerifier>.Instance);
        var manifestValidator = new DefaultManifestValidator(signatureVerifier, NullLogger<DefaultManifestValidator>.Instance);
        var sharedAssemblies = SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        var moduleLoader = new ModuleLoader(runtimeContext, manifestValidator, sharedAssemblies, loggerFactory.CreateLogger<ModuleLoader>());
        var moduleManager = new ModuleManager(loggerFactory.CreateLogger<ModuleManager>());

        // 2. Setup Temporary Services for DB & Seeding
        var tempServices = new ServiceCollection();
        tempServices.AddLogging(builder => builder.AddConsole());
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
            
            logger.LogDebug("Found {Count} enabled modules to load.", enabledModules.Count);
            
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
                        logger.LogDebug("Loading module {ModuleId} from {Path}...", module.Id, packagePath);
                        // Skip module initialization - it will be done after host services are bound
                        var descriptor = await moduleLoader.LoadAsync(packagePath, module.IsSystem, skipModuleInitialization: true).ConfigureAwait(false);
                        if (descriptor == null)
                        {
                            logger.LogWarning("Module {ModuleId} failed to load.", module.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load module {ModuleId} from {Path}", module.Id, module.Path);
                }
            }
        }

        // 4. Register Loaded Modules to Manager
        moduleManager.AddModule(new TStartupModule());

        foreach (var runtimeModule in runtimeContext.RuntimeModules)
        {
            foreach (var assembly in runtimeModule.LoadContext.Assemblies)
            {
                 var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
                 
                 foreach (var type in moduleTypes)
                 {
                     if (type == typeof(TStartupModule)) continue;
                     
                     try 
                     {
                         var instance = (IModule)Activator.CreateInstance(type)!;
                         // Let ModuleManager resolve the ID from [Module] attribute or type name
                         // Pass package-level dependencies from manifest
                         moduleManager.AddModule(instance, manifestDependencies: runtimeModule.Manifest.Dependencies.Keys);
                     }
                     catch (Exception ex)
                     {
                         logger.LogError(ex, "Failed to instantiate module {Type}", type.Name);
                     }
                 }
            }
        }

        // 5. Register Services to FINAL ServiceCollection
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
