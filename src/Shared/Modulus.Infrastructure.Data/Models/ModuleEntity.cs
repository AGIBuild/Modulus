using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Modulus.UI.Abstractions;

namespace Modulus.Infrastructure.Data.Models;

/// <summary>
/// Database entity for module metadata, aligned with extension.vsixmanifest schema.
/// </summary>
public class ModuleEntity
{
    // ============================================================
    // Manifest Identity Fields (from <Identity> element)
    // ============================================================

    /// <summary>
    /// Unique module identifier (GUID format recommended).
    /// Maps to: Identity@Id
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Module version (SemVer format).
    /// Maps to: Identity@Version
    /// </summary>
    [Required]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Language/locale code (e.g., "en-US").
    /// Maps to: Identity@Language
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Module publisher/author.
    /// Maps to: Identity@Publisher
    /// </summary>
    public string? Publisher { get; set; }

    // ============================================================
    // Manifest Metadata Fields (from <Metadata> element)
    // ============================================================

    /// <summary>
    /// Display name shown in the UI.
    /// Maps to: Metadata/DisplayName
    /// </summary>
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the module.
    /// Maps to: Metadata/Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Comma-separated tags for categorization.
    /// Maps to: Metadata/Tags
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Project website URL.
    /// Maps to: Metadata/Website
    /// </summary>
    public string? Website { get; set; }

    // ============================================================
    // Manifest Installation Fields
    // ============================================================

    /// <summary>
    /// JSON array of supported host IDs (e.g., ["Modulus.Host.Avalonia", "Modulus.Host.Blazor"]).
    /// Maps to: Installation/InstallationTarget elements
    /// </summary>
    public string? SupportedHosts { get; set; }

    /// <summary>
    /// JSON array of module dependencies (e.g., [{"id": "...", "version": "[1.0,)"}]).
    /// Maps to: Dependencies elements
    /// </summary>
    public string? Dependencies { get; set; }

    // ============================================================
    // Build/Packaging Fields
    // ============================================================

    /// <summary>
    /// If true, this module is bundled with the host application at build time.
    /// Maps to: Identity@IsBundled
    /// </summary>
    public bool IsBundled { get; set; }

    /// <summary>
    /// Relative path to module directory (e.g., "Modules/EchoPlugin").
    /// </summary>
    [Required]
    public string Path { get; set; } = string.Empty;

    // ============================================================
    // Runtime/Internal Fields
    // ============================================================

    /// <summary>
    /// SHA256 hash of the manifest file for change detection.
    /// </summary>
    public string? ManifestHash { get; set; }

    /// <summary>
    /// Timestamp when the manifest was last validated.
    /// </summary>
    public DateTime? ValidatedAt { get; set; }

    /// <summary>
    /// If true, this module is managed by the system seeder and cannot be uninstalled.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// User preference for enabling/disabling the module.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Desired menu location for this module (Main or Bottom).
    /// </summary>
    public MenuLocation MenuLocation { get; set; } = MenuLocation.Main;

    /// <summary>
    /// Current module state.
    /// </summary>
    public ModuleState State { get; set; } = ModuleState.Ready;

    /// <summary>
    /// Validation errors from install-time validation (JSON array of strings).
    /// Null or empty when module is valid.
    /// </summary>
    public string? ValidationErrors { get; set; }

    /// <summary>
    /// Navigation collection to associated menus.
    /// </summary>
    public virtual ICollection<MenuEntity> Menus { get; set; } = new List<MenuEntity>();
}

