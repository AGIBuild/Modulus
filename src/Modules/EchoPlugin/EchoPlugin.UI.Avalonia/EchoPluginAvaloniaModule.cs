using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Modulus.Modules.EchoPlugin.ViewModels;

namespace Modulus.Modules.EchoPlugin.UI.Avalonia;

/// <summary>
/// Echo Plugin Avalonia UI - declares Avalonia-specific navigation.
/// </summary>
[AvaloniaMenu("Echo Tool", typeof(EchoViewModel), Icon = "📢", Order = 20)]
public class EchoPluginAvaloniaModule : ModuleBase
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Register View-ViewModel mapping
        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<EchoViewModel, EchoView>();
        return Task.CompletedTask;
    }
}
