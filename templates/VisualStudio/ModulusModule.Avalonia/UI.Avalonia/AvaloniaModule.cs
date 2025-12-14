using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Infrastructure;
using Modulus.Modules.$ext_safeprojectname$.ViewModels;

namespace Modulus.Modules.$ext_safeprojectname$.UI.Avalonia;

/// <summary>
/// $ext_safeprojectname$ Avalonia UI Module.
/// </summary>
[DependsOn(typeof($ext_safeprojectname$Module))]
[AvaloniaMenu("$ext_safeprojectname$", typeof(MainViewModel), Icon = IconKind.Apps, Order = 100)]
public class $ext_safeprojectname$AvaloniaModule : AvaloniaModuleBase
{
    public override async Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        await base.OnApplicationInitializationAsync(context, cancellationToken);

        var viewRegistry = context.ServiceProvider.GetRequiredService<IViewRegistry>();
        viewRegistry.Register<MainViewModel, MainView>();
    }
}

