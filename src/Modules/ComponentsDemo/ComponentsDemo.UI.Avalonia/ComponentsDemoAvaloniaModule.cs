using Microsoft.Extensions.DependencyInjection;
using Modulus.Modules.ComponentsDemo.ViewModels;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Infrastructure;

namespace Modulus.Modules.ComponentsDemo.UI.Avalonia;

/// <summary>
/// Components Demo Avalonia UI - declares Avalonia-specific navigation.
/// </summary>
[AvaloniaMenu("Components", typeof(ComponentsMainViewModel), Icon = IconKind.Grid, Order = 15)]
public class ComponentsDemoAvaloniaModule : AvaloniaModuleBase
{
    public override async Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        await base.OnApplicationInitializationAsync(context, cancellationToken);

        // Register View-ViewModel mapping
        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<ComponentsMainViewModel, ComponentsMainView>();
    }
}

