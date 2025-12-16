using Microsoft.Extensions.DependencyInjection;
using Modulus.Modules.Home.ViewModels;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Infrastructure;

namespace Modulus.Modules.Home.UI.Avalonia;

/// <summary>
/// Home Module Avalonia UI - declares Avalonia-specific navigation.
/// </summary>
[DependsOn(typeof(HomeModule))]
[AvaloniaMenu("home", "Home", typeof(HomeViewModel), Icon = IconKind.Home, Order = 1)]
public class HomeAvaloniaModule : AvaloniaModuleBase
{
    public override async Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        await base.OnApplicationInitializationAsync(context, cancellationToken);

        // Register View-ViewModel mapping
        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<HomeViewModel, HomeView>();
    }
}


