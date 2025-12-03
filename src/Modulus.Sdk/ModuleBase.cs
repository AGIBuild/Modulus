using System;
using System.Threading;
using System.Threading.Tasks;

namespace Modulus.Sdk;

public abstract class ModuleBase : IModule
{
    public virtual void PreConfigureServices(IModuleLifecycleContext context)
    {
    }

    public virtual void ConfigureServices(IModuleLifecycleContext context)
    {
    }

    public virtual void PostConfigureServices(IModuleLifecycleContext context)
    {
    }

    public virtual Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnApplicationShutdownAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
