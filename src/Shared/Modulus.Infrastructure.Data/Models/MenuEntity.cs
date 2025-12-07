using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Modulus.UI.Abstractions;

namespace Modulus.Infrastructure.Data.Models;

public class MenuEntity
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string ModuleId { get; set; } = string.Empty;

    [ForeignKey(nameof(ModuleId))]
    public virtual ModuleEntity Module { get; set; } = null!;

    public string? ParentId { get; set; }

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public string? Route { get; set; }

    public MenuLocation Location { get; set; }

    public int Order { get; set; }
}

