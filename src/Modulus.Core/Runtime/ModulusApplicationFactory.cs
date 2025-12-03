using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var moduleLoader = new ModuleLoader(runtimeContext, manifestValidator, Microsoft.Extensions.Logging.Abstractions.NullLogger<ModuleLoader>.Instance);
        
        var moduleManagerLogger = loggerFactory.CreateLogger<ModuleManager>();
        var moduleManager = new ModuleManager(moduleManagerLogger);

        // 2. Load Modules (Discovery Phase using Providers)
        if (moduleProviders != null)
        {
            foreach (var provider in moduleProviders)
            {
                var packagePaths = await provider.GetModulePackagesAsync().ConfigureAwait(false);
                foreach (var path in packagePaths)
                {
                    try
                    {
                        // Pass IsSystemSource from provider
                        await moduleLoader.LoadAsync(path, provider.IsSystemSource).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to load module from {Path}", path);
                    }
                }
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
                         moduleManager.AddModule(instance);
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
}
