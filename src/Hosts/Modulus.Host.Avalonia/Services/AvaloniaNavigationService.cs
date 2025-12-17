using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ILazyModuleLoader _lazyModuleLoader;
    private readonly IModuleExecutionGuard _executionGuard;
    private readonly ILogger<AvaloniaNavigationService> _logger;
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
        RuntimeContext runtimeContext,
        ILazyModuleLoader lazyModuleLoader,
        IModuleExecutionGuard executionGuard,
        ILogger<AvaloniaNavigationService> logger)
    {
        _serviceProvider = serviceProvider;
        _uiFactory = uiFactory;
        _menuRegistry = menuRegistry;
        _runtimeContext = runtimeContext;
        _executionGuard = executionGuard;
        _logger = logger;
        _lazyModuleLoader = lazyModuleLoader;
    }

    public async Task<bool> NavigateToAsync(string navigationKey, NavigationOptions? options = null)
    {
        // Intentionally no logs for normal navigation; only Warning/Error logs for abnormal behavior are emitted.
        try
        {
            return await NavigateToAsyncCore(navigationKey, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NavigateToAsync threw exception for {NavigationKey}", navigationKey);
            return false;
        }
    }

    private async Task<bool> NavigateToAsyncCore(string navigationKey, NavigationOptions? options)
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
            _logger.LogWarning("Navigation blocked by guard: {NavigationKey}", navigationKey);
            return false;
        }

        // Find the MenuItem to determine instance mode and module ID
        var menuItem = FindMenuItem(navigationKey);
        var instanceMode = menuItem?.InstanceMode ?? PageInstanceMode.Default;

        // Lazy load module if needed
        if (menuItem?.ModuleId != null)
        {
            var loaded = await _lazyModuleLoader.EnsureModuleLoadedAsync(menuItem.ModuleId);
            if (!loaded)
            {
                _logger.LogWarning("Lazy loading module failed: {ModuleId}", menuItem.ModuleId);
            }
        }

        // Resolve ViewModel type
        var vmType = ResolveViewModelType(navigationKey);
        if (vmType == null)
        {
            _logger.LogWarning("Navigation failed: Could not resolve ViewModel type for key '{NavigationKey}'", navigationKey);
            return false;
        }

        // Check if module is in faulted state
        var moduleId = ResolveModuleId(vmType);
        if (moduleId != null && !_executionGuard.CanExecute(moduleId))
        {
            var healthInfo = _executionGuard.GetHealthInfo(moduleId);
            _logger.LogWarning(
                "Navigation blocked: Module {ModuleId} is in {State} state. Last error: {Error}",
                moduleId, healthInfo.State, healthInfo.LastError?.Message ?? "N/A");
            return false;
        }

        // Get or create ViewModel based on instance mode
        var viewModel = GetOrCreateViewModel(vmType, navigationKey, instanceMode, options.ForceNewInstance);
        if (viewModel == null)
        {
            _logger.LogWarning(
                "Navigation failed: Could not create ViewModel '{ViewModelType}' for key '{NavigationKey}'. Module: {ModuleId}",
                vmType.Name, navigationKey, moduleId ?? "Host");
            return false;
        }

        // Apply parameters if provided
        if (options.Parameters != null && viewModel is INavigationAware navAware)
        {
            navAware.OnNavigatedTo(options.Parameters);
        }

        // Create view
        object? view;
        try
        {
            view = _uiFactory.CreateView(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating view for ViewModel '{ViewModelType}'", vmType.Name);
            return false;
        }
        
        if (view == null)
        {
            _logger.LogWarning(
                "Navigation failed: Could not create view for ViewModel '{ViewModelType}'. Check IViewRegistry mappings.",
                vmType.Name);
            return false;
        }
        
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
        var moduleId = ResolveModuleId(vmType);
        
        // Use execution guard for module ViewModels
        if (moduleId != null)
        {
            var result = _executionGuard.ExecuteSafe(
                moduleId,
                () => CreateViewModelCore(vmType),
                fallback: null,
                caller: $"CreateViewModel:{vmType.Name}");
            
            if (result == null)
            {
                var healthInfo = _executionGuard.GetHealthInfo(moduleId);
                _logger.LogWarning(
                    "ViewModel creation failed for {ViewModelType}. Module {ModuleId} health: {State}, Consecutive faults: {Faults}, Last error: {Error}",
                    vmType.Name, moduleId, healthInfo.State, healthInfo.ConsecutiveFaultCount, 
                    healthInfo.LastError?.Message ?? "N/A");
            }
            
            return result;
        }
        
        // Host ViewModels don't need guard
        return CreateViewModelCore(vmType);
    }

    private object? CreateViewModelCore(Type vmType)
    {
        var moduleProvider = ResolveModuleServiceProvider(vmType);
        
        if (moduleProvider != null)
        {
            // Always use ActivatorUtilities with CompositeServiceProvider to ensure
            // both module and host services can be resolved as dependencies
            return ActivatorUtilities.CreateInstance(moduleProvider, vmType);
        }

        // Host ViewModels - try registered service first, then create
        var vm = _serviceProvider.GetService(vmType);
        if (vm != null)
        {
            return vm;
        }

        return ActivatorUtilities.CreateInstance(_serviceProvider, vmType);
    }

    private string? ResolveModuleId(Type vmType)
    {
        var handle = _runtimeContext.ModuleHandles.FirstOrDefault(h => h.Assemblies.Any(a => a == vmType.Assembly));
        return handle?.RuntimeModule.Descriptor.Id;
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

    /// <summary>
    /// Clears cached navigation instances for a specific module.
    /// </summary>
    public void ClearModuleCache(string moduleId)
    {
        // Find all navigation keys associated with this module
        var keysToRemove = new List<string>();

        // Check menu items to find module's navigation keys
        var allItems = _menuRegistry.GetItems(MenuLocation.Main)
            .Concat(_menuRegistry.GetItems(MenuLocation.Bottom));

        foreach (var item in allItems)
        {
            if (item.ModuleId == moduleId)
            {
                keysToRemove.Add(item.NavigationKey);
            }

            if (item.Children != null)
            {
                keysToRemove.AddRange(item.Children
                    .Where(c => c.ModuleId == moduleId)
                    .Select(c => c.NavigationKey));
            }
        }

        // Also check if module handle has assemblies we can identify
        if (_runtimeContext.TryGetModuleHandle(moduleId, out var handle) && handle != null)
        {
            var moduleAssemblies = handle.Assemblies.ToHashSet();
            
            // Remove cached ViewModels from this module's assemblies
            foreach (var kvp in _singletonViewModels)
            {
                if (kvp.Value != null && moduleAssemblies.Contains(kvp.Value.GetType().Assembly))
                {
                    if (!keysToRemove.Contains(kvp.Key))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }
        }

        // Remove from caches
        foreach (var key in keysToRemove)
        {
            _singletonViewModels.TryRemove(key, out _);
            _singletonViews.TryRemove(key, out _);
        }

        // If current navigation is from this module, navigate away to clear the view
        if (_currentViewModel != null && _runtimeContext.TryGetModuleHandle(moduleId, out var currentHandle) && currentHandle != null)
        {
            var currentAssembly = _currentViewModel.GetType().Assembly;
            if (currentHandle.Assemblies.Contains(currentAssembly))
            {
                _currentViewModel = null;
                _currentView = null;
                _currentNavigationKey = null;
                
                // Notify shell to clear the current view before module unload
                OnViewChanged?.Invoke(null, string.Empty);
            }
        }
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

