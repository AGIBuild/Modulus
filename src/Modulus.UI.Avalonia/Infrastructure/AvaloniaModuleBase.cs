using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Modulus.Sdk;

namespace Modulus.UI.Avalonia.Infrastructure;

/// <summary>
/// Base module that injects shared Avalonia resources before the module runs.
/// </summary>
public abstract class AvaloniaModuleBase : ModulusComponent
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

