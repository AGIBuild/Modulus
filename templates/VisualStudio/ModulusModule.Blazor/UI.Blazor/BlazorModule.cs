using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Modulus.Modules.$ext_safeprojectname$.ViewModels;

namespace Modulus.Modules.$ext_safeprojectname$.UI.Blazor;

/// <summary>
/// $ext_safeprojectname$ Blazor UI Module.
/// </summary>
[BlazorMenu("$ext_safeprojectname$", "/$ext_safeprojectname$", Icon = IconKind.Apps, Order = 100)]
public class $ext_safeprojectname$BlazorModule : ModulusPackage
{
    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        var viewRegistry = context.ServiceProvider.GetService<IViewRegistry>();
        viewRegistry?.Register<MainViewModel, MainView>();
        return Task.CompletedTask;
    }
}

