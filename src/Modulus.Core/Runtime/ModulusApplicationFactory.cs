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
using Modulus.UI.Abstractions;

namespace Modulus.Core.Runtime;

public static class ModulusApplicationFactory
{
    /// <summary>
    /// Creates a Modulus application with explicit module directories.
    /// </summary>
    /// <param name="services">Service collection for DI.</param>
    /// <param name="moduleDirectories">Directories containing modules to install. Null = use defaults.</param>
    /// <param name="hostType">Host type identifier (e.g., "Modulus.Host.Avalonia").</param>
    /// <param name="databasePath">Path to SQLite database.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="hostVersion">Optional host version for compatibility validation.</param>
    public static async Task<ModulusApplication> CreateAsync<TStartupModule>(
        IServiceCollection services,
        IReadOnlyList<ModuleDirectory>? moduleDirectories = null,
        string? hostType = null,
        string? databasePath = null,
        IConfiguration? configuration = null,
        ILoggerFactory? loggerFactory = null,
        Version? hostVersion = null)
        where TStartupModule : IModule, new()
    {
        // 1. Setup Runtime Components
        var runtimeContext = new RuntimeContext();
        if (hostType != null)
        {
            runtimeContext.SetCurrentHost(hostType);
        }
        if (hostVersion != null)
        {
            runtimeContext.SetHostVersion(hostVersion);
        }

        var effectiveConfig = configuration ?? new ConfigurationBuilder()
            .Build();

        loggerFactory ??= ModulusLogging.CreateLoggerFactory(effectiveConfig, hostType ?? "Host");
        var logger = loggerFactory.CreateLogger<ModulusApplication>();

        using var hostScope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["HostType"] = hostType ?? "UnknownHost"
        });

        var manifestValidator = new DefaultManifestValidator(loggerFactory.CreateLogger<DefaultManifestValidator>(), runtimeContext);
        
        // Build shared assembly catalog from domain metadata + host configuration + built-in shared policy
        var configuredSharedAssemblies = effectiveConfig.GetSection(SharedAssemblyOptions.SectionPath).Get<List<string>>();
        var configuredSharedPrefixes = effectiveConfig.GetSection(SharedAssemblyOptions.PrefixesSectionPath).Get<List<string>>();
        var mergedSharedAssemblies = SharedAssemblyPolicy.MergeWithConfiguredAssemblies(configuredSharedAssemblies);
        var sharedAssemblies = SharedAssemblyCatalog.FromAssemblies(
            AppDomain.CurrentDomain.GetAssemblies(),
            mergedSharedAssemblies,
            configuredSharedPrefixes,
            loggerFactory.CreateLogger<SharedAssemblyCatalog>());
        
        // Create resolution reporter for diagnostics
        var resolutionReporter = new SharedAssemblyResolutionReporter(loggerFactory.CreateLogger<SharedAssemblyResolutionReporter>());
        
        // Log initial catalog state with source breakdown
        var catalogEntries = sharedAssemblies.GetEntries();
        var domainCount = catalogEntries.Count(e => e.Source == SharedAssemblySource.DomainAttribute);
        var configCount = catalogEntries.Count(e => e.Source == SharedAssemblySource.HostConfig);
        logger.LogInformation(
            "Shared assembly catalog initialized: {Total} total (domain: {Domain}, config: {Config})",
            catalogEntries.Count, domainCount, configCount);
        foreach (var entry in catalogEntries.Where(e => e.Source == SharedAssemblySource.HostConfig))
        {
            logger.LogDebug("Shared assembly from config: {Name}", entry.Name);
        }
        var initialMismatches = sharedAssemblies.GetMismatches();
        foreach (var mismatch in initialMismatches)
        {
            logger.LogWarning("Shared assembly mismatch: {Name} - {Reason}", mismatch.AssemblyName, mismatch.Reason);
            resolutionReporter.ReportFailure(new SharedAssemblyResolutionFailedEvent
            {
                ModuleId = mismatch.ModuleId ?? "(host-config)",
                AssemblyName = mismatch.AssemblyName,
                Source = mismatch.RequestSource,
                DeclaredDomain = mismatch.DeclaredDomain,
                Reason = mismatch.Reason
            });
        }
        
        var executionGuard = new ModuleExecutionGuard(loggerFactory.CreateLogger<ModuleExecutionGuard>(), runtimeContext);
        var moduleLoader = new ModuleLoader(runtimeContext, manifestValidator, sharedAssemblies, executionGuard, loggerFactory.CreateLogger<ModuleLoader>(), loggerFactory, resolutionReporter: resolutionReporter);
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
        tempServices.AddScoped<IPendingCleanupRepository, PendingCleanupRepository>();
        tempServices.AddSingleton<IModuleCleanupService, ModuleCleanupService>();
        tempServices.AddScoped<IModuleInstallerService, ModuleInstallerService>();
        tempServices.AddScoped<SystemModuleInstaller>();
        tempServices.AddScoped<ModuleIntegrityChecker>();

        using (var sp = tempServices.BuildServiceProvider())
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ModulusDbContext>();
            await db.Database.MigrateAsync();

            // Fail fast on legacy database/menu formats. This change intentionally does NOT provide backward compatibility.
            // New menu ids are projected as: {ModuleId}.{HostType}.{Key}.{Index} (>= 4 dot-separated parts).
            if (!string.IsNullOrWhiteSpace(hostType))
            {
                // NOTE: Use client-side check to avoid provider-specific SQL translation issues.
                var menuIds = await db.Menus
                    .AsNoTracking()
                    .Select(m => m.Id)
                    .ToListAsync();
                var hasLegacyMenus = menuIds.Any(id => id.Split('.', StringSplitOptions.RemoveEmptyEntries).Length < 4);

                if (hasLegacyMenus)
                {
                    throw new InvalidOperationException(
                        $"Legacy database detected (menu ids are not in the expected format). " +
                        $"Delete the database file and restart. DatabasePath='{databasePath}', HostType='{hostType}'.");
                }
            }

            var installer = scope.ServiceProvider.GetRequiredService<SystemModuleInstaller>();

            // Project Host menus into DB (unified projection pipeline; host menus also come from DB at render time).
            // This replaces HostModuleSeeder and keeps host behavior consistent across Avalonia/Blazor.
            if (!string.IsNullOrWhiteSpace(hostType))
            {
                await ProjectHostModuleAndMenusAsync<TStartupModule>(
                    scope.ServiceProvider,
                    hostType!,
                    hostVersion,
                    loggerFactory.CreateLogger("HostMenuProjection"),
                    CancellationToken.None);
            }

            // Install modules from specified directories
            if (moduleDirectories != null)
            {
                foreach (var dir in moduleDirectories)
                {
                    await installer.InstallFromDirectoryAsync(dir.Path, dir.IsSystem, hostType);
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
                    // Resolve path relative to application base directory (not CWD)
                    var basePath = Path.IsPathRooted(module.Path)
                        ? module.Path
                        : Path.Combine(AppContext.BaseDirectory, module.Path);
                    var absolutePath = Path.GetFullPath(basePath);
                    
                    // Determine package path - handle both directory and file paths
                    string packagePath;
                    if (Directory.Exists(absolutePath))
                    {
                        // Path is a directory
                        packagePath = absolutePath;
                    }
                    else if (File.Exists(absolutePath))
                    {
                        // Path is a manifest file
                        packagePath = Path.GetDirectoryName(absolutePath)!;
                    }
                    else
                    {
                        // Path doesn't exist
                        logger.LogWarning(
                            "Module path not found: {Path} (resolved to {AbsolutePath}). " +
                            "BaseDirectory: {BaseDir}, CWD: {Cwd}",
                            module.Path, absolutePath, AppContext.BaseDirectory, Directory.GetCurrentDirectory());
                        packagePath = absolutePath;
                    }

                    if (packagePath != null)
                    {
                        logger.LogInformation("Loading module {ModuleName} ({ModuleId}) from {Path}...", module.DisplayName, module.Id, packagePath);
                        // Skip module initialization - it will be done after host services are bound
                        var descriptor = await moduleLoader.LoadAsync(packagePath, module.IsSystem, skipModuleInitialization: true).ConfigureAwait(false);
                        if (descriptor == null)
                        {
                            logger.LogWarning("Module {ModuleName} ({ModuleId}) failed to load.", module.DisplayName, module.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to load module {ModuleName} ({ModuleId}) from {Path}", module.DisplayName, module.Id, module.Path);
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
        services.AddSingleton<IModuleExecutionGuard>(executionGuard);
        services.AddSingleton<IModuleLoader>(moduleLoader);
        services.AddSingleton(moduleManager);
        services.AddSingleton<ISharedAssemblyCatalog>(sharedAssemblies);
        services.AddSingleton<ISharedAssemblyDiagnosticsService>(new SharedAssemblyDiagnosticsService(sharedAssemblies));
        services.AddSingleton<ISharedAssemblyResolutionReporter>(resolutionReporter);
        services.AddSingleton<IManifestValidator>(manifestValidator);

        var app = new ModulusApplication(services, moduleManager, runtimeContext, logger);
        app.ConfigureServices();

        return app;
    }

    private static async Task ProjectHostModuleAndMenusAsync<TStartupModule>(
        IServiceProvider serviceProvider,
        string hostType,
        Version? hostVersion,
        ILogger logger,
        CancellationToken cancellationToken)
        where TStartupModule : IModule, new()
    {
        var moduleRepo = serviceProvider.GetRequiredService<IModuleRepository>();
        var menuRepo = serviceProvider.GetRequiredService<IMenuRepository>();

        var displayName =
            ModulusHostIds.Matches(hostType, ModulusHostIds.Blazor) ? "Modulus Host (Blazor)" :
            ModulusHostIds.Matches(hostType, ModulusHostIds.Avalonia) ? "Modulus Host (Avalonia)" :
            "Modulus Host";

        var resolvedVersion =
            hostVersion?.ToString(3)
            ?? typeof(TStartupModule).Assembly.GetName().Version?.ToString(3)
            ?? "1.0.0";

        var hostModule = new ModuleEntity
        {
            Id = hostType,
            DisplayName = displayName,
            Version = resolvedVersion,
            Language = "en-US",
            Publisher = "Modulus Framework",
            Website = "https://github.com/AGIBuild/Modulus",
            Path = "built-in", // Special marker for host module (no physical package path)
            IsSystem = true,
            IsEnabled = true,
            State = Modulus.Infrastructure.Data.Models.ModuleState.Ready,
            MenuLocation = MenuLocation.Main
        };

        await moduleRepo.UpsertAsync(hostModule, cancellationToken);

        var hostModuleType = typeof(TStartupModule);
        var menus = BuildHostMenuEntities(hostType, hostModuleType);
        await menuRepo.ReplaceModuleMenusAsync(hostType, menus, cancellationToken);

        logger.LogInformation("Host module {HostType} projected {MenuCount} menus to database.", hostType, menus.Length);
    }

    private static MenuEntity[] BuildHostMenuEntities(string hostType, Type hostModuleType)
    {
        if (ModulusHostIds.Matches(hostType, ModulusHostIds.Blazor))
        {
            var attrs = (BlazorMenuAttribute[])Attribute.GetCustomAttributes(hostModuleType, typeof(BlazorMenuAttribute), inherit: false);
            return BuildMenuEntities(hostType, hostType, attrs.Select(a => new MenuProjection(a.Key, a.DisplayName, a.Route)
            {
                Icon = a.Icon.ToString(),
                Location = a.Location,
                Order = a.Order
            }).ToList());
        }

        if (ModulusHostIds.Matches(hostType, ModulusHostIds.Avalonia))
        {
            var attrs = (AvaloniaMenuAttribute[])Attribute.GetCustomAttributes(hostModuleType, typeof(AvaloniaMenuAttribute), inherit: false);
            return BuildMenuEntities(hostType, hostType, attrs.Select(a => new MenuProjection(
                a.Key,
                a.DisplayName,
                a.ViewModelType.FullName ?? a.ViewModelType.Name)
            {
                Icon = a.Icon.ToString(),
                Location = a.Location,
                Order = a.Order
            }).ToList());
        }

        return Array.Empty<MenuEntity>();
    }

    private static MenuEntity[] BuildMenuEntities(string moduleId, string hostType, List<MenuProjection> projections)
    {
        var entities = new List<MenuEntity>();
        foreach (var group in projections.GroupBy(p => p.Key))
        {
            var idx = 0;
            foreach (var p in group)
            {
                entities.Add(new MenuEntity
                {
                    Id = $"{moduleId}.{hostType}.{p.Key}.{idx}",
                    ModuleId = moduleId,
                    DisplayName = p.DisplayName,
                    Icon = p.Icon,
                    Route = p.Route,
                    Location = p.Location,
                    Order = p.Order,
                    ParentId = null
                });
                idx++;
            }
        }
        return entities.ToArray();
    }

    private sealed record MenuProjection(string Key, string DisplayName, string Route)
    {
        public string Icon { get; init; } = IconKind.Grid.ToString();
        public MenuLocation Location { get; init; } = MenuLocation.Main;
        public int Order { get; init; } = 50;
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
                // Resolve path relative to application base directory
                var basePath = Path.IsPathRooted(module.Path)
                    ? module.Path
                    : Path.Combine(AppContext.BaseDirectory, module.Path);
                var absolutePath = Path.GetFullPath(basePath);
                var manifestPath = Directory.Exists(absolutePath)
                    ? Path.Combine(absolutePath, SystemModuleInstaller.VsixManifestFileName)
                    : absolutePath;
                    
                if (File.Exists(manifestPath))
                {
                    var manifest = await VsixManifestReader.ReadFromFileAsync(manifestPath);
                    
                    if (manifest != null)
                    {
                        foreach (var dep in manifest.Dependencies)
                        {
                            if (moduleDict.ContainsKey(dep.Id))
                            {
                                deps.Add(dep.Id);
                            }
                            else if (!runtimeContext.TryGetModule(dep.Id, out _))
                            {
                                 // Warn but don't crash
                                 logger.LogDebug("Module {ModuleId} depends on {DepId} which is not in current load list.", module.Id, dep.Id);
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

/// <summary>
/// Represents a directory containing modules to install.
/// </summary>
/// <param name="Path">Path to the modules directory.</param>
/// <param name="IsSystem">Whether modules in this directory are system modules (cannot be uninstalled).</param>
public sealed record ModuleDirectory(string Path, bool IsSystem);
