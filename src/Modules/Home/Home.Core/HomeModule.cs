using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Modules.Home.Services;
using Modulus.Modules.Home.ViewModels;
using Modulus.Sdk;

namespace Modulus.Modules.Home;

/// <summary>
/// Home Module Core - provides the welcome dashboard for Modulus.
/// </summary>
[DependsOn()]
[Module("Home", "Home", Description = "Welcome dashboard showcasing Modulus framework features.")]
public class HomeModule : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddTransient<HomeViewModel>();
        context.Services.AddSingleton<IHomeStatisticsService, HomeStatisticsService>();
    }

    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        var logger = context.ServiceProvider.GetService<ILogger<HomeModule>>();
        logger?.LogInformation("Home module initialized.");
        return Task.CompletedTask;
    }
}


