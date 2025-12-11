using System.ComponentModel.DataAnnotations;

namespace Modulus.Infrastructure.Data.Models;

/// <summary>
/// Represents a directory scheduled for cleanup.
/// Used when module files are locked during unload and cannot be deleted immediately.
/// </summary>
public class PendingCleanupEntity
{
    /// <summary>
    /// Auto-increment primary key.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Full path to the directory to be cleaned up.
    /// </summary>
    [Required]
    [MaxLength(1024)]
    public string DirectoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Optional: The module ID that was uninstalled.
    /// Used to remove cleanup record if module is reinstalled to same path.
    /// </summary>
    [MaxLength(64)]
    public string? ModuleId { get; set; }

    /// <summary>
    /// When this cleanup was scheduled.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of times cleanup has been attempted.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Last time cleanup was attempted.
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }
}

