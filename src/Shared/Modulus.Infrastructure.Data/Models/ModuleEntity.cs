using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Modulus.UI.Abstractions;

namespace Modulus.Infrastructure.Data.Models;

public class ModuleEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Version { get; set; } = string.Empty;
    
    /// <summary>
    /// Brief description of the module.
    /// </summary>
    public string? Description { get; set; }

    public string? Author { get; set; }

    public string? Website { get; set; }

    /// <summary>
    /// Relative path to manifest.json (e.g. "Modules/User/PluginA/manifest.json")
    /// </summary>
    [Required]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Fully-qualified type name of the entry component (optional).
    /// </summary>
    public string? EntryComponent { get; set; }

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

    public ModuleState State { get; set; } = ModuleState.Ready;

    public virtual ICollection<MenuEntity> Menus { get; set; } = new List<MenuEntity>();
}

