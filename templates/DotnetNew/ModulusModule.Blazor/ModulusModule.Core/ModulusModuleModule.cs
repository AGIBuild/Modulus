using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;

namespace Modulus.Modules.ModulusModule;

/// <summary>
/// ModulusModule Module - Core business logic.
/// </summary>
[DependsOn()]
[Module("ModulusModule", "{{DisplayNameComputed}}", Description = "{{Description}}")]
public class ModulusModuleModule : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddTransient<ViewModels.MainViewModel>();
    }

    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        var logger = context.ServiceProvider.GetService<ILogger<ModulusModuleModule>>();
        logger?.LogInformation("ModulusModule module initialized.");
        return Task.CompletedTask;
    }
}

