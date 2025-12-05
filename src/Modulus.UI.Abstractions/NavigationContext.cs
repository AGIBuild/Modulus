namespace Modulus.UI.Abstractions;

/// <summary>
/// Context information passed to navigation guards.
/// </summary>
public class NavigationContext
{
    /// <summary>
    /// The navigation key of the source page (null if first navigation).
    /// </summary>
    public string? FromKey { get; init; }

    /// <summary>
    /// The navigation key of the target page.
    /// </summary>
    public required string ToKey { get; init; }

    /// <summary>
    /// Navigation options for this request.
    /// </summary>
    public NavigationOptions Options { get; init; } = new();
}

