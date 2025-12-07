using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;
using Modulus.Core.Manifest;
using Modulus.Sdk;

namespace Modulus.Core.Runtime;

public static class ModulusApplicationFactory
{
    public static async Task<ModulusApplication> CreateAsync<TStartupModule>(
        IServiceCollection services, 
        IEnumerable<IModuleProvider> moduleProviders, 
        string? hostType = null) 
        where TStartupModule : IModule, new()
    {
        // 1. Setup minimal runtime components for loading
        var runtimeContext = new RuntimeContext();
        if (hostType != null)
        {
            runtimeContext.SetCurrentHost(hostType);
        }

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ModulusApplication>();
        
        var signatureVerifier = new Sha256ManifestSignatureVerifier(Microsoft.Extensions.Logging.Abstractions.NullLogger<Sha256ManifestSignatureVerifier>.Instance);
        var manifestValidator = new DefaultManifestValidator(signatureVerifier, Microsoft.Extensions.Logging.Abstractions.NullLogger<DefaultManifestValidator>.Instance);
        var sharedAssemblies = SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        var moduleLoader = new ModuleLoader(runtimeContext, manifestValidator, sharedAssemblies, Microsoft.Extensions.Logging.Abstractions.NullLogger<ModuleLoader>.Instance);
        services.AddSingleton<ISharedAssemblyCatalog>(sharedAssemblies);
        
        var moduleManagerLogger = loggerFactory.CreateLogger<ModuleManager>();
        var moduleManager = new ModuleManager(moduleManagerLogger);

        // 2. Load Modules (Discovery Phase using Providers) with dependency ordering
        var packageInfos = new List<ModulePackageInfo>();
        if (moduleProviders != null)
        {
            foreach (var provider in moduleProviders)
            {
                var packagePaths = await provider.GetModulePackagesAsync().ConfigureAwait(false);
                foreach (var path in packagePaths)
                {
                    try
                    {
                        var manifestPath = Path.Combine(path, "manifest.json");
                        var manifest = await ManifestReader.ReadFromFileAsync(manifestPath).ConfigureAwait(false);
                        if (manifest == null)
                        {
                            logger.LogWarning("Skipping module at {Path}: manifest not found.", path);
                            continue;
                        }

                        packageInfos.Add(new ModulePackageInfo(path, manifest, provider.IsSystemSource));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to read manifest from {Path}", path);
                    }
                }
            }
        }

        var orderedPackages = OrderPackages(packageInfos, runtimeContext, logger);

        foreach (var package in orderedPackages)
        {
            try
            {
                await moduleLoader.LoadAsync(package.Path, package.IsSystem).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load module from {Path}", package.Path);
            }
        }

        // 3. Register Modules to Manager
        // Add Startup Module
        moduleManager.AddModule(new TStartupModule());

        // Add Discovered Dynamic Modules
        foreach (var runtimeModule in runtimeContext.RuntimeModules)
        {
            foreach (var assembly in runtimeModule.LoadContext.Assemblies)
            {
                 // logger.LogInformation("Scanning assembly {Assembly} for modules...", assembly.FullName);
                 var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
                 
                 foreach (var type in moduleTypes)
                 {
                     if (type == typeof(TStartupModule)) continue;
                     
                     try 
                     {
                         // logger.LogInformation("Found module type {Type}", type.FullName);
                         var instance = (IModule)Activator.CreateInstance(type)!;
                         moduleManager.AddModule(instance, runtimeModule.Descriptor.Id, runtimeModule.Manifest.Dependencies.Keys);
                     }
                     catch (Exception ex)
                     {
                         logger.LogError(ex, "Failed to instantiate module {Type}", type.Name);
                     }
                 }
            }
        }

        // 4. Register Core Services
        services.AddSingleton(runtimeContext);
        services.AddSingleton<IModuleLoader>(moduleLoader);
        services.AddSingleton(moduleManager);
        
        // Register Module Providers as IEnumerable<IModuleProvider>
        var providerList = moduleProviders?.ToList() ?? new List<IModuleProvider>();
        services.AddSingleton<IEnumerable<IModuleProvider>>(providerList);
        foreach (var provider in providerList)
        {
            services.AddSingleton(provider.GetType(), provider);
        }

        // 5. Create App and Configure Services
        var app = new ModulusApplication(services, moduleManager, logger);
        app.ConfigureServices(); // Executes Pre/Config/Post

        return app;
    }

    private static IReadOnlyList<ModulePackageInfo> OrderPackages(IEnumerable<ModulePackageInfo> packages, RuntimeContext runtimeContext, ILogger logger)
    {
        var packageList = packages.ToList();
        var packageIdSet = new HashSet<string>(packageList.Select(p => p.Manifest.Id), StringComparer.OrdinalIgnoreCase);

        var registrations = new List<PackageRegistration>();
        foreach (var package in packageList)
        {
            var deps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var dependency in package.Manifest.Dependencies.Keys)
            {
                if (packageIdSet.Contains(dependency))
                {
                    deps.Add(dependency);
                }
                else if (!runtimeContext.TryGetModule(dependency, out _))
                {
                    logger.LogError("Module {ModuleId} is missing dependency {DependencyId}.", package.Manifest.Id, dependency);
                    throw new InvalidOperationException($"Missing dependency '{dependency}' for module '{package.Manifest.Id}'.");
                }
            }

            registrations.Add(new PackageRegistration(package, package.Manifest.Id, deps));
        }

        var sorted = ModuleDependencyResolver.TopologicallySort(
            registrations,
            r => r.ModuleId,
            r => r.Dependencies,
            logger);

        return sorted.Select(r => r.Package).ToList();
    }

    private sealed record ModulePackageInfo(string Path, ModuleManifest Manifest, bool IsSystem);

    private sealed record PackageRegistration(ModulePackageInfo Package, string ModuleId, HashSet<string> Dependencies);
}


