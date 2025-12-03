namespace Modulus.Core;

/// <summary>
/// Defines the known host environments supported by the framework.
/// </summary>
public static class HostType
{
    /// <summary>
    /// Represents the Blazor Hybrid host (MAUI/Photino).
    /// </summary>
    public const string Blazor = "BlazorApp";

    /// <summary>
    /// Represents the Avalonia UI host.
    /// </summary>
    public const string Avalonia = "AvaloniaApp";
}

