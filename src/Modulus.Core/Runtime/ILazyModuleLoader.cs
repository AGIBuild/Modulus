using System.Threading;
using System.Threading.Tasks;

namespace Modulus.Core.Runtime;

/// <summary>
/// Service for lazy loading modules on-demand.
/// Modules are loaded only when needed (e.g., when user navigates to a module's page).
/// </summary>
public interface ILazyModuleLoader
{
    /// <summary>
    /// Ensures a module is loaded. If already loaded, returns immediately.
    /// </summary>
    /// <param name="moduleId">The module ID to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if module is loaded successfully or was already loaded.</returns>
    Task<bool> EnsureModuleLoadedAsync(string moduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a module is currently loaded.
    /// </summary>
    /// <param name="moduleId">The module ID to check.</param>
    /// <returns>True if the module is loaded.</returns>
    bool IsModuleLoaded(string moduleId);

    /// <summary>
    /// Gets the module ID associated with a navigation key (route).
    /// </summary>
    /// <param name="navigationKey">The navigation key (ViewModel type or route).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Module ID if found, null otherwise.</returns>
    Task<string?> GetModuleIdForNavigationKeyAsync(string navigationKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures a module is loaded for a given navigation key.
    /// This combines GetModuleIdForNavigationKeyAsync and EnsureModuleLoadedAsync.
    /// </summary>
    /// <param name="navigationKey">The navigation key (ViewModel type or route).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if module is loaded successfully or was already loaded, false if navigation key not found.</returns>
    Task<bool> EnsureModuleLoadedForNavigationAsync(string navigationKey, CancellationToken cancellationToken = default);
}

