using CommunityToolkit.Mvvm.ComponentModel;

namespace Modulus.UI.Abstractions;

public abstract class ViewModelBase : ObservableObject, IViewModel
{
    public virtual string Title { get; protected set; } = string.Empty;
}
