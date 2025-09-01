using System.ComponentModel.DataAnnotations;

namespace FamilyManagement.API.Models;

public sealed class Family
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

