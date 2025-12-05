using System.Threading.Tasks;

namespace Modulus.UI.Abstractions;

/// <summary>
/// Interface for navigation guards that can intercept and conditionally prevent navigation.
/// </summary>
public interface INavigationGuard
{
    /// <summary>
    /// Called before leaving the current page. Return false to cancel navigation.
    /// </summary>
    /// <param name="context">Navigation context with source, target, and options.</param>
    /// <returns>True to allow navigation, false to cancel.</returns>
    Task<bool> CanNavigateFromAsync(NavigationContext context);

    /// <summary>
    /// Called before entering the target page. Return false to cancel navigation.
    /// </summary>
    /// <param name="context">Navigation context with source, target, and options.</param>
    /// <returns>True to allow navigation, false to cancel.</returns>
    Task<bool> CanNavigateToAsync(NavigationContext context);
}

