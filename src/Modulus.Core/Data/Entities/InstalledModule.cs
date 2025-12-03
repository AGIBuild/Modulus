using System.ComponentModel.DataAnnotations;

namespace Modulus.Core.Data.Entities;

/// <summary>
/// Represents an installed module record in the database.
/// </summary>
public class InstalledModule
{
    [Key]
    [MaxLength(128)]
    public string Id { get; set; } = string.Empty;
    
    [MaxLength(256)]
    public string DisplayName { get; set; } = string.Empty;
    
    [MaxLength(64)]
    public string Version { get; set; } = string.Empty;
    
    [MaxLength(1024)]
    public string PackagePath { get; set; } = string.Empty;
    
    public bool IsEnabled { get; set; } = true;
    
    public bool IsSystem { get; set; }
    
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoadedAt { get; set; }
}

