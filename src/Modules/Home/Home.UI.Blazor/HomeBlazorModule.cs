using Microsoft.Extensions.DependencyInjection;
using Modulus.Modules.Home.ViewModels;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.Home.UI.Blazor;

/// <summary>
/// Home Module Blazor UI - declares Blazor-specific navigation.
/// </summary>
[DependsOn(typeof(HomeModule))]
// Menu is declared in extension.vsixmanifest, no [BlazorMenu] needed here
public class HomeBlazorModule : ModulusPackage
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        // Register View-ViewModel mapping
        var viewRegistry = context.ServiceProvider.GetService<IViewRegistry>();
        viewRegistry?.Register<HomeViewModel, HomePage>();
        return Task.CompletedTask;
    }
}


