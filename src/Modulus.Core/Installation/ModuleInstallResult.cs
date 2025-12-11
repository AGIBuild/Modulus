namespace Modulus.Core.Installation;

/// <summary>
/// Result of a module installation operation.
/// </summary>
public class ModuleInstallResult
{
    /// <summary>
    /// Whether the installation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The ID of the installed module (when successful).
    /// </summary>
    public string? ModuleId { get; init; }

    /// <summary>
    /// The installation path of the module (when successful).
    /// </summary>
    public string? InstallPath { get; init; }

    /// <summary>
    /// Error message if installation failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Whether the installation requires user confirmation to overwrite an existing module.
    /// </summary>
    public bool RequiresConfirmation { get; init; }

    /// <summary>
    /// Display name of the module (for UI feedback).
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Version of the module (for UI feedback).
    /// </summary>
    public string? Version { get; init; }

    public static ModuleInstallResult Succeeded(string moduleId, string installPath, string? displayName = null, string? version = null)
        => new() { Success = true, ModuleId = moduleId, InstallPath = installPath, DisplayName = displayName, Version = version };

    public static ModuleInstallResult Failed(string error)
        => new() { Success = false, Error = error };

    public static ModuleInstallResult ConfirmationRequired(string moduleId, string? displayName = null)
        => new() { Success = false, RequiresConfirmation = true, ModuleId = moduleId, DisplayName = displayName };
}


