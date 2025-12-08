using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Core.Runtime;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Avalonia.Services;

/// <summary>
/// Avalonia implementation of INavigationService with guard support and instance lifecycle management.
/// </summary>
public class AvaloniaNavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUIFactory _uiFactory;
    private readonly IMenuRegistry _menuRegistry;
    private readonly RuntimeContext _runtimeContext;
    private readonly List<INavigationGuard> _guards = new();
    private readonly ConcurrentDictionary<string, object> _singletonViewModels = new();
    private readonly ConcurrentDictionary<string, object> _singletonViews = new();
    private readonly object _guardsLock = new();

    private string? _currentNavigationKey;
    private object? _currentView;
    private object? _currentViewModel;

    /// <summary>
    /// Action to update the shell's current view. Set by ShellViewModel.
    /// </summary>
    public Action<object?, string>? OnViewChanged { get; set; }

    public string? CurrentNavigationKey => _currentNavigationKey;

    public event EventHandler<NavigationEventArgs>? Navigated;

    public AvaloniaNavigationService(
        IServiceProvider serviceProvider,
        IUIFactory uiFactory,
        IMenuRegistry menuRegistry,
        RuntimeContext runtimeContext)
    {
        _serviceProvider = serviceProvider;
        _uiFactory = uiFactory;
        _menuRegistry = menuRegistry;
        _runtimeContext = runtimeContext;
    }

    public async Task<bool> NavigateToAsync(string navigationKey, NavigationOptions? options = null)
    {
        options ??= new NavigationOptions();

        var context = new NavigationContext
        {
            FromKey = _currentNavigationKey,
            ToKey = navigationKey,
            Options = options
        };

        // Evaluate guards
        if (!await EvaluateGuardsAsync(context))
        {
            return false;
        }

        // Find the MenuItem to determine instance mode
        var menuItem = FindMenuItem(navigationKey);
        var instanceMode = menuItem?.InstanceMode ?? PageInstanceMode.Default;

        // Resolve ViewModel type
        var vmType = ResolveViewModelType(navigationKey);
        if (vmType == null)
        {
            return false;
        }

        // Get or create ViewModel based on instance mode
        var viewModel = GetOrCreateViewModel(vmType, navigationKey, instanceMode, options.ForceNewInstance);
        if (viewModel == null)
        {
            return false;
        }

        // Apply parameters if provided
        if (options.Parameters != null && viewModel is INavigationAware navAware)
        {
            navAware.OnNavigatedTo(options.Parameters);
        }

        // Create view
        var view = _uiFactory.CreateView(viewModel);
        
        // Update state
        var previousKey = _currentNavigationKey;
        _currentNavigationKey = navigationKey;
        _currentView = view;
        _currentViewModel = viewModel;

        // Notify shell
        OnViewChanged?.Invoke(view, menuItem?.DisplayName ?? ExtractDisplayName(navigationKey));

        // Raise event
        Navigated?.Invoke(this, new NavigationEventArgs
        {
            FromKey = previousKey,
            ToKey = navigationKey,
            View = view,
            ViewModel = viewModel
        });

        return true;
    }

    public Task<bool> NavigateToAsync<TViewModel>(NavigationOptions? options = null) where TViewModel : class
    {
        return NavigateToAsync(typeof(TViewModel).FullName!, options);
    }

    public void RegisterNavigationGuard(INavigationGuard guard)
    {
        lock (_guardsLock)
        {
            if (!_guards.Contains(guard))
            {
                _guards.Add(guard);
            }
        }
    }

    public void UnregisterNavigationGuard(INavigationGuard guard)
    {
        lock (_guardsLock)
        {
            _guards.Remove(guard);
        }
    }

    private async Task<bool> EvaluateGuardsAsync(NavigationContext context)
    {
        List<INavigationGuard> guardsCopy;
        lock (_guardsLock)
        {
            guardsCopy = _guards.ToList();
        }

        foreach (var guard in guardsCopy)
        {
            // Check if we can leave current page
            if (context.FromKey != null)
            {
                var canLeave = await guard.CanNavigateFromAsync(context);
                if (!canLeave)
                {
                    return false;
                }
            }

            // Check if we can enter target page
            var canEnter = await guard.CanNavigateToAsync(context);
            if (!canEnter)
            {
                return false;
            }
        }

        return true;
    }

    private MenuItem? FindMenuItem(string navigationKey)
    {
        var allItems = _menuRegistry.GetItems(MenuLocation.Main)
            .Concat(_menuRegistry.GetItems(MenuLocation.Bottom));

        foreach (var item in allItems)
        {
            if (item.NavigationKey == navigationKey)
            {
                return item;
            }

            // Check children
            if (item.Children != null)
            {
                var child = item.Children.FirstOrDefault(c => c.NavigationKey == navigationKey);
                if (child != null)
                {
                    return child;
                }
            }
        }

        return null;
    }

    private Type? ResolveViewModelType(string navigationKey)
    {
        // Try direct type resolution
        var vmType = Type.GetType(navigationKey);
        if (vmType != null)
        {
            return vmType;
        }

        // Search host assemblies
        vmType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(t => t.FullName == navigationKey || t.Name == navigationKey);

        if (vmType != null)
        {
            return vmType;
        }

        // Search module assemblies (loaded in separate AssemblyLoadContexts)
        foreach (var runtimeModule in _runtimeContext.RuntimeModules)
        {
            // Also search RuntimeModuleHandle assemblies (more complete list)
            if (_runtimeContext.TryGetModuleHandle(runtimeModule.Descriptor.Id, out var handle) && handle != null)
            {
                foreach (var assembly in handle.Assemblies)
                {
                    try
                    {
                        vmType = assembly.GetTypes()
                            .FirstOrDefault(t => t.FullName == navigationKey || t.Name == navigationKey);
                        if (vmType != null)
                        {
                            return vmType;
                        }
                    }
                    catch
                    {
                        // Skip assemblies that fail to enumerate types
                    }
                }
            }
            // Fallback to LoadContext.Assemblies
            foreach (var assembly in runtimeModule.LoadContext.Assemblies)
            {
                try
                {
                    vmType = assembly.GetTypes()
                        .FirstOrDefault(t => t.FullName == navigationKey || t.Name == navigationKey);
                    if (vmType != null)
                    {
                        return vmType;
                    }
                }
                catch
                {
                    // Skip assemblies that fail to enumerate types
                }
            }
        }

        return null;
    }

    private object? GetOrCreateViewModel(Type vmType, string navigationKey, PageInstanceMode instanceMode, bool forceNew)
    {
        // Force new always creates a new instance
        if (forceNew || instanceMode == PageInstanceMode.Transient)
        {
            return CreateViewModel(vmType);
        }

        // Singleton or Default (treated as singleton)
        return _singletonViewModels.GetOrAdd(navigationKey, _ => CreateViewModel(vmType)!);
    }

    private object? CreateViewModel(Type vmType)
    {
        try
        {
            var moduleProvider = ResolveModuleServiceProvider(vmType);
            
            if (moduleProvider != null)
            {
                var moduleVm = moduleProvider.GetService(vmType) ?? ActivatorUtilities.CreateInstance(moduleProvider, vmType);
                if (moduleVm != null)
                {
                    return moduleVm;
                }
            }

            var vm = _serviceProvider.GetService(vmType);
            if (vm != null)
            {
                return vm;
            }

            // Fall back to ActivatorUtilities
            return ActivatorUtilities.CreateInstance(_serviceProvider, vmType);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private IServiceProvider? ResolveModuleServiceProvider(Type vmType)
    {
        var handle = _runtimeContext.ModuleHandles.FirstOrDefault(h => h.Assemblies.Any(a => a == vmType.Assembly));
        return handle?.CompositeServiceProvider;
    }

    private static string ExtractDisplayName(string navigationKey)
    {
        var name = navigationKey.Split('.').LastOrDefault() ?? navigationKey;
        return name.Replace("ViewModel", "");
    }

    /// <summary>
    /// Clear cached singleton instances (useful for testing or reset).
    /// </summary>
    public void ClearCache()
    {
        _singletonViewModels.Clear();
        _singletonViews.Clear();
    }
}

/// <summary>
/// Optional interface for ViewModels that want to receive navigation parameters.
/// </summary>
public interface INavigationAware
{
    void OnNavigatedTo(IDictionary<string, object> parameters);
    void OnNavigatedFrom();
}

