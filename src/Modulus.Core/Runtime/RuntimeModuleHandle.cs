using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Runtime;

public sealed class RuntimeModuleHandle : IAsyncDisposable, IDisposable
{
    public RuntimeModule RuntimeModule { get; }
    public ModuleManifest Manifest { get; }
    public IServiceProvider ServiceProvider { get; }
    public IServiceProvider CompositeServiceProvider { get; }
    public IReadOnlyCollection<IModule> ModuleInstances { get; }
    public IReadOnlyCollection<MenuItem> RegisteredMenus { get; }
    public IReadOnlyCollection<Assembly> Assemblies { get; }

    private readonly IServiceScope? _serviceScope;

    public RuntimeModuleHandle(
        RuntimeModule runtimeModule,
        ModuleManifest manifest,
        IServiceScope? serviceScope,
        IServiceProvider serviceProvider,
        IServiceProvider compositeServiceProvider,
        IReadOnlyCollection<IModule> moduleInstances,
        IReadOnlyCollection<MenuItem> registeredMenus,
        IReadOnlyCollection<Assembly> assemblies)
    {
        RuntimeModule = runtimeModule;
        Manifest = manifest;
        _serviceScope = serviceScope;
        ServiceProvider = serviceProvider;
        CompositeServiceProvider = compositeServiceProvider;
        ModuleInstances = moduleInstances;
        RegisteredMenus = registeredMenus;
        Assemblies = assemblies;
    }

    public void Dispose()
    {
        _serviceScope?.Dispose();
        (ServiceProvider as IDisposable)?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        if (_serviceScope is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        Dispose();
        return ValueTask.CompletedTask;
    }
}

