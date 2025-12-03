using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Modulus.Sdk;

public interface IModuleLifecycleContext
{
    IServiceCollection Services { get; }
}

public interface IModuleInitializationContext
{
    IServiceProvider ServiceProvider { get; }
}

public interface IModule
{
    void PreConfigureServices(IModuleLifecycleContext context);
    void ConfigureServices(IModuleLifecycleContext context);
    void PostConfigureServices(IModuleLifecycleContext context);

    Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default);
    Task OnApplicationShutdownAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default);
}
