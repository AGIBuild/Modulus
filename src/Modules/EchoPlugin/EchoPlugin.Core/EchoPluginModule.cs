using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;

namespace Modulus.Modules.EchoPlugin;

/// <summary>
/// Echo Plugin Core - business logic only.
/// UI-specific menu declarations are in UI.Avalonia and UI.Blazor modules.
/// </summary>
[DependsOn()] // no deps
[Module("EchoPlugin", "Echo Tool", Description = "A simple echo plugin to demonstrate the SDK.")]
public class EchoPluginModule : ModulusComponent
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddTransient<ViewModels.EchoViewModel>();
    }
}
