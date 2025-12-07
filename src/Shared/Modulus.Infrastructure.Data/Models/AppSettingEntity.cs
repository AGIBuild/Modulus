using System.ComponentModel.DataAnnotations;

namespace Modulus.Infrastructure.Data.Models;

/// <summary>
/// Represents a key-value application setting stored in the database.
/// </summary>
public class AppSettingEntity
{
    [Key]
    [MaxLength(256)]
    public string Key { get; set; } = string.Empty;
    
    public string Value { get; set; } = string.Empty;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

