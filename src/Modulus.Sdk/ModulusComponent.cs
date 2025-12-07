using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Modulus.Sdk;

/// <summary>
/// Base class for code components (logical units) that participate in the Modulus runtime.
/// Compatible with existing IModule pipeline.
/// </summary>
public abstract class ModulusComponent : IModule
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

