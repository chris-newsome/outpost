using System.ComponentModel.DataAnnotations;

namespace FamilyManagement.API.Models;

public sealed class Document
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid FamilyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ContentType { get; set; }

    [Required]
    [MaxLength(400)]
    public string StoragePath { get; set; } = string.Empty; // Supabase Storage path

    [MaxLength(50)]
    public string? UploadedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

