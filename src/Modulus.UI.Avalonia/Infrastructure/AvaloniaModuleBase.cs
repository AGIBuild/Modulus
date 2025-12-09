using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Modulus.Sdk;

namespace Modulus.UI.Avalonia.Infrastructure;

/// <summary>
/// Base module that injects shared Avalonia resources before the module runs.
/// </summary>
#pragma warning disable CS0618 // Suppress obsolete warning - AvaloniaModuleBase itself is the recommended base for Avalonia modules
public abstract class AvaloniaModuleBase : ModulusPackage
#pragma warning restore CS0618
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        if (Application.Current != null)
        {
            AvaloniaResourceManager.EnsureResources(Application.Current);
        }

        return base.OnApplicationInitializationAsync(context, cancellationToken);
    }
}

