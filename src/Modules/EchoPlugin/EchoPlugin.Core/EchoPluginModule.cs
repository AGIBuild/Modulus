using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;

namespace Modulus.Modules.EchoPlugin;

/// <summary>
/// Echo Plugin Core - business logic only.
/// UI-specific menu declarations are in UI.Avalonia and UI.Blazor modules.
/// </summary>
[DependsOn()] // no deps
[Module("EchoPlugin", "Echo Tool", Description = "A simple echo plugin to demonstrate the SDK.")]
public class EchoPluginModule : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddTransient<ViewModels.EchoViewModel>();
    }

    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        var logger = context.ServiceProvider.GetService<ILogger<EchoPluginModule>>();
        logger?.LogInformation("EchoPlugin module initialized.");
        return Task.CompletedTask;
    }
}
