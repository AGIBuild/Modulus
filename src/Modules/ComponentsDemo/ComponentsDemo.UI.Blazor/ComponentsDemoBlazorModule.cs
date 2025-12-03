using Microsoft.Extensions.DependencyInjection;
using Modulus.Modules.ComponentsDemo.ViewModels;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.ComponentsDemo.UI.Blazor;

/// <summary>
/// Components Demo Blazor UI - declares Blazor-specific navigation.
/// </summary>
[BlazorMenu("Components", "/components", Icon = "palette", Order = 15)]
public class ComponentsDemoBlazorModule : ModuleBase
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Register View-ViewModel mapping (if needed for Blazor)
        var viewRegistry = context.ServiceProvider.GetService<IViewRegistry>();
        viewRegistry?.Register<ComponentsMainViewModel, ComponentsMainPage>();
        return Task.CompletedTask;
    }
}

