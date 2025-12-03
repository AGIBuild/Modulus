using System.ComponentModel;

namespace Modulus.UI.Abstractions;

/// <summary>
/// Marker interface for view models, extending INotifyPropertyChanged.
/// </summary>
public interface IViewModel : INotifyPropertyChanged
{
    string Title { get; }
}

