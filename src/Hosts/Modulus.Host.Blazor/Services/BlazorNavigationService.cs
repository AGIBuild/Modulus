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
using UiMenuItem = Modulus.UI.Abstractions.MenuItem;

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
    private IDisposable? _locationChangingRegistration;

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

        // Intercept browser-driven navigations so guards still apply when user clicks links.
        try
        {
            _locationChangingRegistration = _navigationManager.RegisterLocationChangingHandler(OnLocationChangingAsync);
        }
        catch
        {
            // Some NavigationManager implementations may not support LocationChanging; programmatic navigation still uses guards.
            _locationChangingRegistration = null;
        }
    }

    private async ValueTask OnLocationChangingAsync(LocationChangingContext context)
    {
        try
        {
            var uri = new Uri(context.TargetLocation);
            var route = uri.AbsolutePath;

            var targetMenu = FindMenuItemByRoute(route);
            var toKey = targetMenu?.Id ?? route;

            var navContext = new UiNavigationContext
            {
                FromKey = _currentNavigationKey,
                ToKey = toKey,
                Options = new UiNavigationOptions()
            };

            if (!await EvaluateGuardsAsync(navContext))
            {
                context.PreventNavigation();
            }
        }
        catch
        {
            // If guard evaluation fails, do not block navigation (keep browser behavior).
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var uri = new Uri(e.Location);
        var route = uri.AbsolutePath;

        var previousKey = _currentNavigationKey;
        var currentMenu = FindMenuItemByRoute(route);
        _currentNavigationKey = currentMenu?.Id ?? route;

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

        // For Blazor, navigationKey is a stable menu id (preferred) or a raw route (back-compat).
        var route = ResolveRoute(navigationKey);
        if (string.IsNullOrWhiteSpace(route))
        {
            _logger.LogWarning("Navigation failed: could not resolve route for key '{NavigationKey}'", navigationKey);
            return false;
        }

        _navigationManager.NavigateTo(route);

        return true;
    }

    public Task<bool> NavigateToAsync<TViewModel>(UiNavigationOptions? options = null) where TViewModel : class
    {
        // For Blazor host, typed navigation is supported only for known host ViewModels.
        // Modules SHOULD navigate via stable menu ids (NavigateToAsync(string)).
        var route = MapHostViewModelToRoute(typeof(TViewModel));
        if (string.IsNullOrWhiteSpace(route))
            return Task.FromResult(false);

        var item = FindMenuItemByRoute(route);
        if (item == null)
            return Task.FromResult(false);

        return NavigateToAsync(item.Id, options);
    }

    private static string? MapHostViewModelToRoute(Type viewModelType)
    {
        var name = viewModelType.Name;
        return name switch
        {
            "ModuleListViewModel" => "/modules",
            "SettingsViewModel" => "/settings",
            _ => null
        };
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

    private UiMenuItem? FindMenuItemById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var allItems = _menuRegistry.GetItems(MenuLocation.Main)
            .Concat(_menuRegistry.GetItems(MenuLocation.Bottom));

        foreach (var item in allItems)
        {
            if (string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
                return item;

            if (item.Children == null) continue;
            var child = item.Children.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));
            if (child != null) return child;
        }

        return null;
    }

    private UiMenuItem? FindMenuItemByRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route)) return null;

        var allItems = _menuRegistry.GetItems(MenuLocation.Main)
            .Concat(_menuRegistry.GetItems(MenuLocation.Bottom));

        foreach (var item in allItems)
        {
            if (string.Equals(item.NavigationKey, route, StringComparison.OrdinalIgnoreCase))
                return item;

            if (item.Children == null) continue;
            var child = item.Children.FirstOrDefault(c => string.Equals(c.NavigationKey, route, StringComparison.OrdinalIgnoreCase));
            if (child != null) return child;
        }

        return null;
    }

    private UiMenuItem? FindMenuItemByTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;

        var allItems = _menuRegistry.GetItems(MenuLocation.Main)
            .Concat(_menuRegistry.GetItems(MenuLocation.Bottom));

        foreach (var item in allItems)
        {
            // For Blazor view-level menus, NavigationKey is the route; target ViewModel mapping is not used.
            // This hook is kept for consistency across hosts.
            if (string.Equals(item.NavigationKey, target, StringComparison.Ordinal))
                return item;

            if (item.Children == null) continue;
            var child = item.Children.FirstOrDefault(c => string.Equals(c.NavigationKey, target, StringComparison.Ordinal));
            if (child != null) return child;
        }

        return null;
    }

    private string? ResolveRoute(string navigationKey)
    {
        if (string.IsNullOrWhiteSpace(navigationKey)) return null;

        // Back-compat: raw routes
        if (navigationKey.StartsWith("/", StringComparison.Ordinal))
            return navigationKey;

        // Preferred: stable menu id
        var item = FindMenuItemById(navigationKey);
        return item?.NavigationKey;
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
                keysToRemove.Add(item.Id);
            }

            if (item.Children != null)
            {
                keysToRemove.AddRange(item.Children
                    .Where(c => c.ModuleId == moduleId)
                    .Select(c => c.Id));
            }
        }

        // Remove from cache
        foreach (var key in keysToRemove)
        {
            _singletonViewModels.TryRemove(key, out _);
        }
    }
}
