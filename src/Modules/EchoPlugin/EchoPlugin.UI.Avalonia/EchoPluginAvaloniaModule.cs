using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Infrastructure;
using Modulus.Modules.EchoPlugin.ViewModels;

namespace Modulus.Modules.EchoPlugin.UI.Avalonia;

/// <summary>
/// Echo Plugin Avalonia UI - declares Avalonia-specific navigation.
/// </summary>
[DependsOn(typeof(Modulus.Modules.EchoPlugin.EchoPluginModule))]
[AvaloniaMenu("Echo Tool", typeof(EchoViewModel), Icon = IconKind.Terminal, Order = 20)]
public class EchoPluginAvaloniaModule : AvaloniaModuleBase
{
    public override async Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        await base.OnApplicationInitializationAsync(context, cancellationToken);

        // Register View-ViewModel mapping
        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<EchoViewModel, EchoView>();
    }
}
