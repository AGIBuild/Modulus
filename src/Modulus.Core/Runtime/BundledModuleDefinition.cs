using System.Collections.Generic;
using System.Text.Json;
using Modulus.Infrastructure.Data.Models;
using Modulus.UI.Abstractions;
using DbModuleState = Modulus.Infrastructure.Data.Models.ModuleState;

namespace Modulus.Core.Runtime;

/// <summary>
/// Definition of a bundled module for data seeding, aligned with extension.vsixmanifest schema.
/// </summary>
public class BundledModuleDefinition
{
    // ============================================================
    // Manifest Identity Fields
    // ============================================================

    /// <summary>
    /// Unique module identifier (GUID format).
    /// Maps to: Identity@Id
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Module version (SemVer format).
    /// Maps to: Identity@Version
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Language/locale code.
    /// Maps to: Identity@Language
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Module publisher/author.
    /// Maps to: Identity@Publisher
    /// </summary>
    public string? Publisher { get; init; }

    // ============================================================
    // Manifest Metadata Fields
    // ============================================================

    /// <summary>
    /// Display name shown in the UI.
    /// Maps to: Metadata/DisplayName
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Brief description of the module.
    /// Maps to: Metadata/Description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Comma-separated tags for categorization.
    /// Maps to: Metadata/Tags
    /// </summary>
    public string? Tags { get; init; }

    /// <summary>
    /// Project website URL.
    /// Maps to: Metadata/Website
    /// </summary>
    public string? Website { get; init; }

    // ============================================================
    // Manifest Installation Fields
    // ============================================================

    /// <summary>
    /// List of supported host IDs.
    /// Maps to: Installation/InstallationTarget elements
    /// </summary>
    public IReadOnlyList<string> SupportedHosts { get; init; } = [];

    /// <summary>
    /// List of module dependencies.
    /// Maps to: Dependencies elements
    /// </summary>
    public IReadOnlyList<ModuleDependency> Dependencies { get; init; } = [];

    // ============================================================
    // Build/Packaging Fields
    // ============================================================

    /// <summary>
    /// Relative path to module directory.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// If true, this module is bundled with the host application.
    /// Maps to: Identity@IsBundled
    /// </summary>
    public bool IsBundled { get; init; } = true;

    // ============================================================
    // Runtime Fields
    // ============================================================

    /// <summary>
    /// If true, this module is managed by the system and cannot be uninstalled.
    /// System modules can be disabled but not removed.
    /// </summary>
    public bool IsSystem { get; init; } = true;

    /// <summary>
    /// User preference for enabling/disabling the module.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Desired menu location for this module.
    /// </summary>
    public MenuLocation MenuLocation { get; init; } = MenuLocation.Main;

    /// <summary>
    /// Module state.
    /// </summary>
    public DbModuleState State { get; init; } = DbModuleState.Ready;

    /// <summary>
    /// Menus associated with this module, keyed by host ID.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<MenuDefinition>> MenusByHost { get; init; } 
        = new Dictionary<string, IReadOnlyList<MenuDefinition>>();

    /// <summary>
    /// Converts to ModuleEntity for database storage.
    /// </summary>
    public ModuleEntity ToEntity() => new()
    {
        Id = Id,
        Version = Version,
        Language = Language,
        Publisher = Publisher,
        DisplayName = DisplayName,
        Description = Description,
        Tags = Tags,
        Website = Website,
        SupportedHosts = SupportedHosts.Count > 0 ? JsonSerializer.Serialize(SupportedHosts) : null,
        Dependencies = Dependencies.Count > 0 ? JsonSerializer.Serialize(Dependencies) : null,
        Path = Path,
        IsBundled = IsBundled,
        IsSystem = IsSystem,
        IsEnabled = IsEnabled,
        MenuLocation = MenuLocation,
        State = State
    };
}

/// <summary>
/// Module dependency information.
/// </summary>
public class ModuleDependency
{
    /// <summary>
    /// Dependency module ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Version range (e.g., "[1.0,)").
    /// </summary>
    public string? VersionRange { get; init; }
}

/// <summary>
/// Definition of a menu item for data seeding.
/// </summary>
public class MenuDefinition
{
    /// <summary>
    /// Unique menu identifier.
    /// Maps to: Asset@Id
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name shown in the UI.
    /// Maps to: Asset@DisplayName
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Icon name (from IconKind enum).
    /// Maps to: Asset@Icon
    /// </summary>
    public string Icon { get; init; } = IconKind.Folder.ToString();

    /// <summary>
    /// Route for navigation (ViewModel type for Avalonia, URL path for Blazor).
    /// Maps to: Asset@Route
    /// </summary>
    public required string Route { get; init; }

    /// <summary>
    /// Menu location (Main or Bottom).
    /// </summary>
    public MenuLocation Location { get; init; } = MenuLocation.Main;

    /// <summary>
    /// Sort order within the menu location.
    /// Maps to: Asset@Order
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Converts to MenuEntity for database storage.
    /// </summary>
    public MenuEntity ToEntity(string moduleId) => new()
    {
        Id = Id,
        ModuleId = moduleId,
        DisplayName = DisplayName,
        Icon = Icon,
        Route = Route,
        Location = Location,
        Order = Order
    };
}

