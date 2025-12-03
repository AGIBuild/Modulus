using System;

namespace Modulus.UI.Abstractions;

public interface IUIFactory
{
    /// <summary>
    /// Creates a view instance for the given view model.
    /// </summary>
    object CreateView(object viewModel);

    /// <summary>
    /// Creates a view instance by its key (e.g. for menu items that don't start with a VM).
    /// </summary>
    object CreateView(string viewKey);
}
