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
    /// Returns true if the given host ID matches (case-insensitive).
    /// </summary>
    public static bool Matches(string hostId, string targetHostId)
    {
        if (string.IsNullOrEmpty(hostId) || string.IsNullOrEmpty(targetHostId))
            return false;

        return string.Equals(hostId, targetHostId, StringComparison.OrdinalIgnoreCase);
    }
}
