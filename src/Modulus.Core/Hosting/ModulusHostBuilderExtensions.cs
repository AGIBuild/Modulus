using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modulus.Core.Architecture;
using Modulus.Core.Manifest;
using Modulus.Core.Runtime;

namespace Modulus.Core.Hosting;

public static class ModulusHostBuilderExtensions
{
    public static IHostBuilder UseModulusRuntime(this IHostBuilder builder)
    {
        return builder.ConfigureServices((_, services) =>
        {
            services.AddSingleton<RuntimeContext>();
            services.AddSingleton<ISharedAssemblyCatalog>(sp => SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies(), null, sp.GetService<ILogger<SharedAssemblyCatalog>>()));
            services.AddSingleton<IModuleLoader, ModuleLoader>();
            services.AddSingleton<IManifestValidator, DefaultManifestValidator>();
            services.AddSingleton<IManifestSignatureVerifier, Sha256ManifestSignatureVerifier>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RuntimeContext).Assembly));
        });
    }
}

