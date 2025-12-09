using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Modulus.Sdk;

/// <summary>
/// Base class for code components (logical units) that participate in the Modulus runtime.
/// Compatible with existing IModule pipeline.
/// </summary>
/// <remarks>
/// This class is obsolete. Use <see cref="ModulusPackage"/> instead for new extensions.
/// ModulusComponent will be removed in v2.0.
/// </remarks>
[Obsolete("Use ModulusPackage instead. ModulusComponent will be removed in v2.0.")]
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

