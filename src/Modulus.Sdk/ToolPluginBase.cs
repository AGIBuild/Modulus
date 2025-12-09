using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Modulus.Sdk;

/// <summary>
/// Base class for plugins that primarily provide a "Tool" (e.g. a utility panel).
/// </summary>
#pragma warning disable CS0618 // Suppress obsolete warning - ToolPluginBase will be updated to inherit ModulusPackage in v2.0
public abstract class ToolPluginBase : ModulusPackage
#pragma warning restore CS0618
{
    // In the future, this can provide helper methods to register the tool with the Shell
    // e.g. RegisterTool<TView, TViewModel>(...)
    
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Default implementation could auto-register tools if we had a convention
        return base.OnApplicationInitializationAsync(context, cancellationToken);
    }
}

