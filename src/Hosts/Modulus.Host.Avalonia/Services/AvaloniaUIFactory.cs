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
        if (viewModel is not ViewModelBase)
            throw ViewModelConventionViolationException.ForType("ViewModel", viewModel.GetType());

        var vmType = viewModel.GetType();
        var moduleHandle = _runtimeContext.ModuleHandles.FirstOrDefault(h => h.Assemblies.Any(a => a == vmType.Assembly));

        // 1. Try Registry
        var registeredViewType = _viewRegistry.GetViewType(vmType);
        if (registeredViewType != null)
        {
             return CreateViewInstance(moduleHandle, registeredViewType, viewModel);
        }

        // 2. Convention: derive view type name without enumerating all types (avoid runtime scans).
        var viewType = ResolveViewTypeByConvention(vmType, moduleHandle);
        if (viewType != null)
        {
            _viewRegistry.Register(vmType, viewType);
            return CreateViewInstance(moduleHandle, viewType, viewModel);
        }
        
        return new TextBlock { Text = $"View not found for {vmType.Name}" };
    }

    private object CreateViewInstance(RuntimeModuleHandle? moduleHandle, Type viewType, object viewModel)
    {
        // Module types MUST be created via CompositeServiceProvider to keep ALC isolation and support both module+host deps.
        var provider = moduleHandle?.CompositeServiceProvider ?? _serviceProvider;

        Control view;

        // Prefer DI constructor: MyView(MyViewModel vm)
        try
        {
            view = (Control)ActivatorUtilities.CreateInstance(provider, viewType, viewModel);
        }
        catch
        {
            // Back-compat for existing views without VM constructor.
            view = (Control)(ActivatorUtilities.CreateInstance(provider, viewType) ?? Activator.CreateInstance(viewType)!);
        }

        view.DataContext = viewModel;
        return view;
    }

    public object CreateView(string viewKey)
    {
        throw new NotImplementedException();
    }

    private static Type? ResolveViewTypeByConvention(Type viewModelType, RuntimeModuleHandle? moduleHandle)
    {
        var vmFullName = viewModelType.FullName;
        if (string.IsNullOrWhiteSpace(vmFullName)) return null;

        var candidates = new List<string>();

        // Common convention: *.ViewModels.*ViewModel -> *.UI.Avalonia.*View
        if (vmFullName.Contains(".ViewModels.", StringComparison.Ordinal))
        {
            candidates.Add(
                vmFullName
                    .Replace(".ViewModels.", ".UI.Avalonia.", StringComparison.Ordinal)
                    .Replace("ViewModel", "View", StringComparison.Ordinal));
        }

        // Fallback: suffix replace only (same namespace)
        candidates.Add(vmFullName.Replace("ViewModel", "View", StringComparison.Ordinal));

        var assemblies = moduleHandle != null
            ? moduleHandle.Assemblies
            : new[] { viewModelType.Assembly };

        foreach (var asm in assemblies)
        {
            foreach (var candidate in candidates.Distinct(StringComparer.Ordinal))
            {
                var t = asm.GetType(candidate, throwOnError: false, ignoreCase: false);
                if (t != null && typeof(Control).IsAssignableFrom(t))
                    return t;
            }
        }

        return null;
    }
}
