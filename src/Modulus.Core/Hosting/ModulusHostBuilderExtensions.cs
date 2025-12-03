using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            services.AddSingleton<IModuleLoader, ModuleLoader>();
            services.AddSingleton<IManifestValidator, DefaultManifestValidator>();
            services.AddSingleton<IManifestSignatureVerifier, Sha256ManifestSignatureVerifier>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RuntimeContext).Assembly));
        });
    }
}

