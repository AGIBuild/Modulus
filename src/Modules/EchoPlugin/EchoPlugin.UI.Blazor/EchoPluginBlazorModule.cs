using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Modulus.Modules.EchoPlugin.ViewModels;

namespace Modulus.Modules.EchoPlugin.UI.Blazor;

/// <summary>
/// Echo Plugin Blazor UI - declares Blazor-specific navigation.
/// </summary>
[BlazorMenu("Echo Tool", "/echo", Icon = "echo", Order = 20)]
public class EchoPluginBlazorModule : ModuleBase
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Register View-ViewModel mapping (if needed for Blazor)
        var viewRegistry = context.ServiceProvider.GetService<IViewRegistry>();
        viewRegistry?.Register<EchoViewModel, EchoView>();
        return Task.CompletedTask;
    }
}
