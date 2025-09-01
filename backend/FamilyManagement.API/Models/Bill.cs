using System.ComponentModel.DataAnnotations;

namespace FamilyManagement.API.Models;

public sealed class Bill
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid FamilyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Vendor { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    public DateTimeOffset DueDate { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "pending"; // pending, paid, overdue

    [MaxLength(100)]
    public string? Category { get; set; }

    public bool Recurring { get; set; }

    public Guid? SubscriptionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

