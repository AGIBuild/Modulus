using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;
using Modulus.Core.Installation;
using Modulus.Core.Manifest;
using Modulus.Core.Runtime;
using Modulus.Infrastructure.Data;
using Modulus.Infrastructure.Data.Repositories;

namespace Modulus.Core.Hosting;

public static class ModulusHostBuilderExtensions
{
    public static IHostBuilder UseModulusRuntime(this IHostBuilder builder)
    {
        return builder.ConfigureServices((_, services) =>
        {
            // Database
            services.AddDbContext<ModulusDbContext>(options => 
                options.UseSqlite("Data Source=modulus.db"));

            // Repositories
            services.AddScoped<IModuleRepository, ModuleRepository>();
            services.AddScoped<IMenuRepository, MenuRepository>();

            // Installation Services
            services.AddScoped<IModuleInstallerService, ModuleInstallerService>();
            services.AddScoped<SystemModuleSeeder>();
            services.AddScoped<ModuleIntegrityChecker>();

            // Core Runtime
            services.AddSingleton<RuntimeContext>();
            services.AddSingleton<ISharedAssemblyCatalog>(sp => SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies(), null, sp.GetService<ILogger<SharedAssemblyCatalog>>()));
            services.AddSingleton<IModuleLoader, ModuleLoader>();
            services.AddSingleton<IManifestValidator, DefaultManifestValidator>();
            services.AddSingleton<IManifestSignatureVerifier, Sha256ManifestSignatureVerifier>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RuntimeContext).Assembly));
        });
    }
}
