using Microsoft.Extensions.DependencyInjection;
using Modulus.Modules.ComponentsDemo.ViewModels;
using Modulus.Modules.ComponentsDemo;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.ComponentsDemo.UI.Blazor;

/// <summary>
/// Components Demo Blazor UI - declares Blazor-specific navigation.
/// </summary>
[DependsOn(typeof(ComponentsDemoModule))]
[BlazorMenu("Components", "/components", Icon = IconKind.Add, Order = 15)]
public class ComponentsDemoBlazorModule : ModulusPackage
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Register View-ViewModel mapping (if needed for Blazor)
        var viewRegistry = context.ServiceProvider.GetService<IViewRegistry>();
        viewRegistry?.Register<ComponentsMainViewModel, ComponentsMainPage>();
        return Task.CompletedTask;
    }
}

