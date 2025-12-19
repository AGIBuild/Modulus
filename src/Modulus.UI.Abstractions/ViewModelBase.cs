using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace Modulus.UI.Abstractions;

public abstract class ViewModelBase : ObservableObject, IViewModel, INavigationParticipant
{
    public virtual string Title { get; protected set; } = string.Empty;

    public virtual Task<bool> CanNavigateFromAsync(NavigationContext context) => Task.FromResult(true);

    public virtual Task<bool> CanNavigateToAsync(NavigationContext context) => Task.FromResult(true);

    public virtual Task OnNavigatedFromAsync(NavigationContext context) => Task.CompletedTask;

    public virtual Task OnNavigatedToAsync(NavigationContext context) => Task.CompletedTask;
}
