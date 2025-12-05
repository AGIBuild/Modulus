namespace Modulus.UI.Abstractions;

/// <summary>
/// Controls how page/view instances are managed during navigation.
/// </summary>
public enum PageInstanceMode
{
    /// <summary>
    /// Use the host's default behavior (typically Singleton).
    /// </summary>
    Default,

    /// <summary>
    /// Reuse the same instance across navigations. State is preserved.
    /// </summary>
    Singleton,

    /// <summary>
    /// Create a new instance on each navigation. Previous instance is discarded.
    /// </summary>
    Transient
}

