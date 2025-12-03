using System.ComponentModel.DataAnnotations;

namespace Modulus.Core.Data.Entities;

/// <summary>
/// Represents a key-value application setting stored in the database.
/// </summary>
public class AppSetting
{
    [Key]
    [MaxLength(256)]
    public string Key { get; set; } = string.Empty;
    
    public string Value { get; set; } = string.Empty;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

