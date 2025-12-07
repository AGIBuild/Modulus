using System;
using System.Linq;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Core.Runtime;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Avalonia.Services;

public class AvaloniaUIFactory : IUIFactory
{
    private readonly RuntimeContext _runtimeContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly IViewRegistry _viewRegistry;

    public AvaloniaUIFactory(RuntimeContext runtimeContext, IServiceProvider serviceProvider)
    {
        _runtimeContext = runtimeContext;
        _serviceProvider = serviceProvider;
        _viewRegistry = serviceProvider.GetService<IViewRegistry>() ?? new ViewRegistry();
    }

    public object CreateView(object viewModel)
    {
        var vmType = viewModel.GetType();

        // 1. Try Registry
        var registeredViewType = _viewRegistry.GetViewType(vmType);
        if (registeredViewType != null)
        {
             return CreateViewInstance(registeredViewType, viewModel);
        }

        // 2. Try RuntimeModuleHandle assemblies (more reliable)
        var viewName = vmType.Name.Replace("ViewModel", "View");
        foreach (var module in _runtimeContext.RuntimeModules)
        {
            if (module.State != ModuleState.Active && module.State != ModuleState.Loaded) continue;

            if (_runtimeContext.TryGetModuleHandle(module.Descriptor.Id, out var handle) && handle != null)
            {
                foreach (var asm in handle.Assemblies)
                {
                    Type? type = null;
                    try { type = asm.GetTypes().FirstOrDefault(t => t.Name == viewName); }
                    catch { continue; }

                    if (type != null && typeof(Control).IsAssignableFrom(type))
                    {
                        _viewRegistry.Register(vmType, type);
                        return CreateViewInstance(type, viewModel);
                    }
                }
            }
        }

        // 3. Try LoadContext.Assemblies (Fallback)

        foreach (var module in _runtimeContext.RuntimeModules)
        {
            if (module.State != ModuleState.Active && module.State != ModuleState.Loaded) continue;

            foreach (var asm in module.LoadContext.Assemblies)
            {
                Type? type = null;
                try
                {
                    type = asm.GetTypes().FirstOrDefault(t => t.Name == viewName);
                }
                catch
                {
                    // Ignore loader exceptions
                    continue;
                }

                if (type != null && typeof(Control).IsAssignableFrom(type))
                {
                    // Cache it for future?
                    _viewRegistry.Register(vmType, type);
                    return CreateViewInstance(type, viewModel);
                }
            }
        }
        
        return new TextBlock { Text = $"View not found for {vmType.Name}" };
    }

    private object CreateViewInstance(Type viewType, object viewModel)
    {
        // Try DI first (if registered), else Activator
        var view = (Control)(ActivatorUtilities.CreateInstance(_serviceProvider, viewType) ?? Activator.CreateInstance(viewType)!);
        view.DataContext = viewModel;
        return view;
    }

    public object CreateView(string viewKey)
    {
        throw new NotImplementedException();
    }
}
