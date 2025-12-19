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
        catch (ViewModelConventionViolationException)
        {
            // Hard convention: do not swallow; fail fast so module developers fix their ViewModels.
            throw;
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

        // Find the MenuItem by stable id (preferred) or by legacy target (back-compat)
        var menuItem = FindMenuItemById(navigationKey) ?? FindMenuItemByTarget(navigationKey);
        var stableKey = menuItem?.Id ?? navigationKey;

        var context = new NavigationContext
        {
            FromKey = _currentNavigationKey,
            ToKey = stableKey,
            Options = options
        };

        // Evaluate guards
        if (!await EvaluateGuardsAsync(context))
        {
            _logger.LogWarning("Navigation blocked by guard: {NavigationKey}", navigationKey);
            return false;
        }

        // ViewModel-level interception (current)
        ViewModelBase? fromVm = null;
        if (_currentViewModel != null)
        {
            fromVm = _currentViewModel as ViewModelBase
                ?? throw ViewModelConventionViolationException.ForType("Current ViewModel", _currentViewModel.GetType());
        }

        var canLeave = fromVm == null ? true : await fromVm.CanNavigateFromAsync(context);
        if (!canLeave)
        {
            _logger.LogWarning("Navigation blocked by current ViewModel: {NavigationKey}", navigationKey);
            return false;
        }

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
        var vmType = menuItem != null ? ResolveViewModelType(menuItem) : null;
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
        var viewModel = GetOrCreateViewModel(vmType, stableKey, instanceMode, options.ForceNewInstance);
        if (viewModel == null)
        {
            _logger.LogWarning(
                "Navigation failed: Could not create ViewModel '{ViewModelType}' for key '{NavigationKey}'. Module: {ModuleId}",
                vmType.Name, navigationKey, moduleId ?? "Host");
            return false;
        }

        // ViewModel-level interception (target)
        var toVm = viewModel as ViewModelBase
            ?? throw ViewModelConventionViolationException.ForType("Target ViewModel", viewModel.GetType());

        var canEnter = await toVm.CanNavigateToAsync(context);
        if (!canEnter)
        {
            _logger.LogWarning("Navigation blocked by target ViewModel: {NavigationKey}", navigationKey);
            return false;
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
        var previousViewModel = _currentViewModel;
        _currentNavigationKey = stableKey;
        _currentView = view;
        _currentViewModel = viewModel;

        // Notify shell
        OnViewChanged?.Invoke(view, menuItem?.DisplayName ?? ExtractDisplayName(stableKey));

        // Navigation lifecycle callbacks (only after successful switch)
        if (previousViewModel is ViewModelBase previousVm)
            await previousVm.OnNavigatedFromAsync(context);
        await toVm.OnNavigatedToAsync(context);

        // Raise event
        Navigated?.Invoke(this, new NavigationEventArgs
        {
            FromKey = previousKey,
            ToKey = stableKey,
            View = view,
            ViewModel = viewModel
        });

        return true;
    }

    // Intentionally no helper here: convention violations should be explicit at call sites.

    public Task<bool> NavigateToAsync<TViewModel>(NavigationOptions? options = null) where TViewModel : class
    {
        var vmFullName = typeof(TViewModel).FullName;
        if (string.IsNullOrWhiteSpace(vmFullName))
        {
            return Task.FromResult(false);
        }

        var item = FindMenuItemByTarget(vmFullName);
        if (item == null)
        {
            _logger.LogWarning("NavigateToAsync<{ViewModel}>: no menu item found for ViewModel '{ViewModelType}'.", typeof(TViewModel).Name, vmFullName);
            return Task.FromResult(false);
        }

        return NavigateToAsync(item.Id, options);
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

    private MenuItem? FindMenuItemById(string menuId)
    {
        var allItems = _menuRegistry.GetItems(MenuLocation.Main)
            .Concat(_menuRegistry.GetItems(MenuLocation.Bottom));

        foreach (var item in allItems)
        {
            if (string.Equals(item.Id, menuId, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }

            // Check children
            if (item.Children != null)
            {
                var child = item.Children.FirstOrDefault(c => string.Equals(c.Id, menuId, StringComparison.OrdinalIgnoreCase));
                if (child != null)
                {
                    return child;
                }
            }
        }

        return null;
    }

    private MenuItem? FindMenuItemByTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;

        var allItems = _menuRegistry.GetItems(MenuLocation.Main)
            .Concat(_menuRegistry.GetItems(MenuLocation.Bottom));

        foreach (var item in allItems)
        {
            if (string.Equals(item.NavigationKey, target, StringComparison.Ordinal))
                return item;

            if (item.Children == null) continue;
            var child = item.Children.FirstOrDefault(c => string.Equals(c.NavigationKey, target, StringComparison.Ordinal));
            if (child != null) return child;
        }

        return null;
    }

    private Type? ResolveViewModelType(MenuItem menuItem)
    {
        var typeName = menuItem.NavigationKey;
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        // Module types: resolve from the module handle assemblies without enumerating all types.
        if (!string.IsNullOrWhiteSpace(menuItem.ModuleId) &&
            _runtimeContext.TryGetModuleHandle(menuItem.ModuleId, out var handle) &&
            handle != null)
        {
            foreach (var asm in handle.Assemblies)
            {
                var t = asm.GetType(typeName, throwOnError: false, ignoreCase: false);
                if (t != null) return t;
            }
        }

        // Host types: resolve from known host assemblies without global scans.
        var hostType = typeof(AvaloniaHostModule).Assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
        if (hostType != null) return hostType;

        var entry = System.Reflection.Assembly.GetEntryAssembly();
        var entryType = entry?.GetType(typeName, throwOnError: false, ignoreCase: false);
        if (entryType != null) return entryType;

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
                keysToRemove.Add(item.Id);
            }

            if (item.Children != null)
            {
                keysToRemove.AddRange(item.Children
                    .Where(c => c.ModuleId == moduleId)
                    .Select(c => c.Id));
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

