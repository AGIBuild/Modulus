using System.Threading.Tasks;

namespace Modulus.UI.Abstractions;

/// <summary>
/// Optional contract for ViewModels to participate in navigation interception and lifecycle callbacks.
/// </summary>
public interface INavigationParticipant
{
    /// <summary>
    /// Called before leaving the current page. Return false to cancel navigation.
    /// </summary>
    Task<bool> CanNavigateFromAsync(NavigationContext context);

    /// <summary>
    /// Called before entering the target page. Return false to cancel navigation.
    /// </summary>
    Task<bool> CanNavigateToAsync(NavigationContext context);

    /// <summary>
    /// Called after successful navigation away from the current page.
    /// </summary>
    Task OnNavigatedFromAsync(NavigationContext context);

    /// <summary>
    /// Called after successful navigation to the target page.
    /// </summary>
    Task OnNavigatedToAsync(NavigationContext context);
}


