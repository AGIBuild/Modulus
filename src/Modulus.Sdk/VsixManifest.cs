using System.Collections.Generic;

namespace Modulus.Sdk;

/// <summary>
/// Root element of extension.vsixmanifest (XML format).
/// Follows VS Extension vsixmanifest 2.0 schema.
/// </summary>
public sealed class VsixManifest
{
    /// <summary>
    /// Schema version (e.g., "2.0.0").
    /// </summary>
    public string Version { get; init; } = "2.0.0";

    /// <summary>
    /// Extension metadata.
    /// </summary>
    public required ManifestMetadata Metadata { get; init; }

    /// <summary>
    /// Installation targets (supported hosts).
    /// </summary>
    public List<InstallationTarget> Installation { get; init; } = new();

    /// <summary>
    /// Extension dependencies.
    /// </summary>
    public List<ManifestDependency> Dependencies { get; init; } = new();

    /// <summary>
    /// Extension assets (assemblies, resources, menus).
    /// </summary>
    public List<ManifestAsset> Assets { get; init; } = new();
}

/// <summary>
/// Extension identity information.
/// </summary>
public sealed class ManifestIdentity
{
    /// <summary>
    /// Unique identifier (GUID recommended).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Version string (SemVer format).
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Publisher/author name.
    /// </summary>
    public required string Publisher { get; init; }

    /// <summary>
    /// Language code (e.g., "en-US").
    /// </summary>
    public string Language { get; init; } = "en-US";
}

/// <summary>
/// Extension metadata.
/// </summary>
public sealed class ManifestMetadata
{
    /// <summary>
    /// Extension identity.
    /// </summary>
    public required ManifestIdentity Identity { get; init; }

    /// <summary>
    /// Display name shown in UI.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Extension description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Path to icon file (relative to manifest).
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Comma-separated tags for categorization.
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// URL for more information.
    /// </summary>
    public string? MoreInfo { get; init; }

    /// <summary>
    /// Path to license file (relative to manifest).
    /// </summary>
    public string? License { get; init; }
}

/// <summary>
/// Target host for installation.
/// </summary>
public sealed class InstallationTarget
{
    /// <summary>
    /// Host identifier (e.g., "Modulus.Host.Blazor", "Modulus.Host.Avalonia").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Version range (NuGet-style, e.g., "[1.0,)").
    /// </summary>
    public string Version { get; init; } = "[1.0,)";
}

/// <summary>
/// Extension dependency.
/// </summary>
public sealed class ManifestDependency
{
    /// <summary>
    /// Dependency extension ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name for error messages.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Required version range.
    /// </summary>
    public string Version { get; init; } = "[1.0,)";
}

/// <summary>
/// Extension asset (assembly, resource, legacy menu fields).
/// </summary>
public sealed class ManifestAsset
{
    /// <summary>
    /// Asset type (e.g., "Modulus.Package", "Modulus.Assembly", "Modulus.Icon").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Path to the asset file (relative to manifest).
    /// Used for assembly and resource assets.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Target host for this asset. Empty means all hosts.
    /// </summary>
    public string? TargetHost { get; init; }
}

