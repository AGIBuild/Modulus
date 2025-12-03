using System;

namespace Modulus.UI.Abstractions;

/// <summary>
/// Event arguments for navigation events.
/// </summary>
public class NavigationEventArgs : EventArgs
{
    /// <summary>
    /// The navigation key of the previous page (null if first navigation).
    /// </summary>
    public string? FromKey { get; init; }

    /// <summary>
    /// The navigation key of the current page.
    /// </summary>
    public required string ToKey { get; init; }

    /// <summary>
    /// The view instance that was navigated to.
    /// </summary>
    public object? View { get; init; }

    /// <summary>
    /// The viewmodel instance that was navigated to.
    /// </summary>
    public object? ViewModel { get; init; }
}

