using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;

namespace Modulus.Modules.$ext_safeprojectname$;

/// <summary>
/// $ext_safeprojectname$ Module - Core business logic.
/// </summary>
[DependsOn()]
[Module("$ext_safeprojectname$", "$ext_safeprojectname$", Description = "A Modulus module.")]
public class $ext_safeprojectname$Module : ModulusPackage
{
    public override void ConfigureServices(IModuleLifecycleContext context)
    {
        context.Services.AddTransient<ViewModels.MainViewModel>();
    }

    public override Task OnApplicationInitializationAsync(IModuleInitializationContext context, CancellationToken cancellationToken = default)
    {
        var logger = context.ServiceProvider.GetService<ILogger<$ext_safeprojectname$Module>>();
        logger?.LogInformation("$ext_safeprojectname$ module initialized.");
        return Task.CompletedTask;
    }
}

