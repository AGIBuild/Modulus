using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using UiNavigationEventArgs = Modulus.UI.Abstractions.NavigationEventArgs;
using UiNavigationOptions = Modulus.UI.Abstractions.NavigationOptions;
using UiNavigationContext = Modulus.UI.Abstractions.NavigationContext;
using Modulus.UI.Abstractions;

namespace Modulus.Host.Blazor.Services;

/// <summary>
/// Blazor implementation of INavigationService using NavigationManager.
/// </summary>
public class BlazorNavigationService : INavigationService
{
    private readonly NavigationManager _navigationManager;
    private readonly IMenuRegistry _menuRegistry;
    private readonly ILogger<BlazorNavigationService> _logger;
    private readonly List<INavigationGuard> _guards = new();
    private readonly ConcurrentDictionary<string, object> _singletonViewModels = new();
    private readonly object _guardsLock = new();

    private string? _currentNavigationKey;

    public string? CurrentNavigationKey => _currentNavigationKey;

    public event EventHandler<UiNavigationEventArgs>? Navigated;

    public BlazorNavigationService(
        NavigationManager navigationManager,
        IMenuRegistry menuRegistry,
        ILogger<BlazorNavigationService> logger)
    {
        _navigationManager = navigationManager;
        _menuRegistry = menuRegistry;
        _logger = logger;

        // Track navigation changes
        _navigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var uri = new Uri(e.Location);
        var previousKey = _currentNavigationKey;
        _currentNavigationKey = uri.AbsolutePath;

        // Intentionally no logs for normal navigation; only Warning/Error logs for abnormal behavior are emitted elsewhere.

        Navigated?.Invoke(this, new UiNavigationEventArgs
        {
            FromKey = previousKey,
            ToKey = _currentNavigationKey
        });
    }

    public async Task<bool> NavigateToAsync(string navigationKey, UiNavigationOptions? options = null)
    {
        options ??= new UiNavigationOptions();

        // Intentionally no logs for normal navigation requests; only Warning/Error logs for abnormal behavior are emitted.

        var context = new UiNavigationContext
        {
            FromKey = _currentNavigationKey,
            ToKey = navigationKey,
            Options = options
        };

        // Evaluate guards
        if (!await EvaluateGuardsAsync(context))
        {
            _logger.LogWarning(
                "Navigation blocked by guard(s): {From} -> {To}",
                context.FromKey ?? "(null)",
                context.ToKey ?? "(null)");
            return false;
        }

        // For Blazor, navigationKey is typically a route (e.g., "/modules", "/settings")
        // Perform the navigation
        _navigationManager.NavigateTo(navigationKey);

        return true;
    }

    public Task<bool> NavigateToAsync<TViewModel>(UiNavigationOptions? options = null) where TViewModel : class
    {
        // For Blazor, we need to map ViewModel types to routes
        // This is a simplified implementation - in practice, you'd have a route registry
        var vmName = typeof(TViewModel).Name;
        var route = MapViewModelToRoute(vmName);

        return NavigateToAsync(route, options);
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

    private async Task<bool> EvaluateGuardsAsync(UiNavigationContext context)
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

    private string MapViewModelToRoute(string viewModelName)
    {
        // Map common ViewModel names to routes
        return viewModelName.Replace("ViewModel", "") switch
        {
            "ModuleList" or "Modules" => "/modules",
            "Settings" => "/settings",
            "Home" => "/",
            _ => "/" + viewModelName.Replace("ViewModel", "").ToLowerInvariant()
        };
    }

    /// <summary>
    /// Get or create a singleton ViewModel instance.
    /// </summary>
    public T GetOrCreateSingleton<T>(string key, Func<T> factory) where T : class
    {
        return (T)_singletonViewModels.GetOrAdd(key, _ => factory());
    }

    /// <summary>
    /// Clear cached singleton instances.
    /// </summary>
    public void ClearCache()
    {
        _singletonViewModels.Clear();
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

        // Remove from cache
        foreach (var key in keysToRemove)
        {
            _singletonViewModels.TryRemove(key, out _);
        }
    }
}
