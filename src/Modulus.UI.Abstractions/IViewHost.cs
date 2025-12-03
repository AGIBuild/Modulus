using System.Threading;
using System.Threading.Tasks;

namespace Modulus.UI.Abstractions;

public interface IViewHost
{
    /// <summary>
    /// Shows a view in a window or main content area.
    /// </summary>
    Task ShowViewAsync(object view, string title, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shows a view as a modal dialog.
    /// </summary>
    Task<bool?> ShowDialogAsync(object view, string title, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes an active view.
    /// </summary>
    Task CloseViewAsync(object view, CancellationToken cancellationToken = default);
}
