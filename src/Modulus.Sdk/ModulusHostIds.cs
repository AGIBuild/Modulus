namespace Modulus.Sdk;

/// <summary>
/// Standard host identifiers for InstallationTarget.
/// </summary>
public static class ModulusHostIds
{
    /// <summary>
    /// Blazor MAUI hybrid application host.
    /// </summary>
    public const string Blazor = "Modulus.Host.Blazor";

    /// <summary>
    /// Avalonia desktop application host.
    /// </summary>
    public const string Avalonia = "Modulus.Host.Avalonia";

    /// <summary>
    /// Legacy host ID for Blazor (deprecated).
    /// </summary>
    [Obsolete("Use Modulus.Host.Blazor instead.")]
    public const string LegacyBlazorApp = "BlazorApp";

    /// <summary>
    /// Legacy host ID for Avalonia (deprecated).
    /// </summary>
    [Obsolete("Use Modulus.Host.Avalonia instead.")]
    public const string LegacyAvaloniaApp = "AvaloniaApp";

    /// <summary>
    /// Converts legacy host ID to new format.
    /// </summary>
    public static string Normalize(string hostId) => hostId switch
    {
#pragma warning disable CS0618
        LegacyBlazorApp => Blazor,
        LegacyAvaloniaApp => Avalonia,
#pragma warning restore CS0618
        _ => hostId
    };

    /// <summary>
    /// Returns true if the given host ID matches (supports both legacy and new formats).
    /// </summary>
    public static bool Matches(string hostId, string targetHostId)
    {
        if (string.IsNullOrEmpty(hostId) || string.IsNullOrEmpty(targetHostId))
            return false;

        return string.Equals(Normalize(hostId), Normalize(targetHostId), StringComparison.OrdinalIgnoreCase);
    }
}

