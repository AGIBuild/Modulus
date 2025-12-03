using System;
using System.Threading.Tasks;

namespace Modulus.UI.Abstractions;

/// <summary>
/// Service for programmatic navigation with interception capabilities.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigate to a view by its navigation key.
    /// </summary>
    /// <param name="navigationKey">The navigation key (route or viewmodel type name).</param>
    /// <param name="options">Optional navigation options.</param>
    /// <returns>True if navigation succeeded, false if cancelled by a guard.</returns>
    Task<bool> NavigateToAsync(string navigationKey, NavigationOptions? options = null);

    /// <summary>
    /// Navigate to a view by its viewmodel type.
    /// </summary>
    /// <typeparam name="TViewModel">The viewmodel type.</typeparam>
    /// <param name="options">Optional navigation options.</param>
    /// <returns>True if navigation succeeded, false if cancelled by a guard.</returns>
    Task<bool> NavigateToAsync<TViewModel>(NavigationOptions? options = null) where TViewModel : class;

    /// <summary>
    /// Register a navigation guard.
    /// </summary>
    /// <param name="guard">The guard to register.</param>
    void RegisterNavigationGuard(INavigationGuard guard);

    /// <summary>
    /// Unregister a navigation guard.
    /// </summary>
    /// <param name="guard">The guard to unregister.</param>
    void UnregisterNavigationGuard(INavigationGuard guard);

    /// <summary>
    /// The navigation key of the currently displayed view (null if none).
    /// </summary>
    string? CurrentNavigationKey { get; }

    /// <summary>
    /// Raised after successful navigation.
    /// </summary>
    event EventHandler<NavigationEventArgs>? Navigated;
}

