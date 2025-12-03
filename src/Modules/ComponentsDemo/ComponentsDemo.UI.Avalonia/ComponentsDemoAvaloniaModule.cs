using Microsoft.Extensions.DependencyInjection;
using Modulus.Modules.ComponentsDemo.ViewModels;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia;

/// <summary>
/// Components Demo Avalonia UI - declares Avalonia-specific navigation.
/// </summary>
[AvaloniaMenu("Components", typeof(ComponentsMainViewModel), Icon = "🎨", Order = 15)]
public class ComponentsDemoAvaloniaModule : ModuleBase
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Register View-ViewModel mapping
        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<ComponentsMainViewModel, ComponentsMainView>();
        return Task.CompletedTask;
    }
}

