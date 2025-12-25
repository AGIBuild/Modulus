namespace Modulus.HostSdk.Abstractions;

/// <summary>
/// Options for creating a Modulus host application via the Host SDK.
/// </summary>
public sealed class ModulusHostSdkOptions
{
    /// <summary>
    /// Host identifier used in module InstallationTarget (e.g. "Modulus.Host.Avalonia").
    /// </summary>
    public required string HostId { get; init; }

    /// <summary>
    /// Host version used for InstallationTarget version range validation.
    /// </summary>
    public required Version HostVersion { get; init; }

    /// <summary>
    /// Path to the host SQLite database file.
    /// </summary>
    public required string DatabasePath { get; init; }

    /// <summary>
    /// Module directories to install/load from.
    /// </summary>
    public List<HostModuleDirectory> ModuleDirectories { get; init; } = new();
}

/// <summary>
/// Represents a directory containing modules to install.
/// </summary>
public sealed record HostModuleDirectory(string Path, bool IsSystem);


