using Microsoft.Extensions.DependencyInjection;

namespace Modulus.Sdk;

public class ModuleLifecycleContext : IModuleLifecycleContext
{
    public IServiceCollection Services { get; }

    public ModuleLifecycleContext(IServiceCollection services)
    {
        Services = services;
    }
}

public class ModuleInitializationContext : IModuleInitializationContext
{
    public IServiceProvider ServiceProvider { get; }

    public ModuleInitializationContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
}

