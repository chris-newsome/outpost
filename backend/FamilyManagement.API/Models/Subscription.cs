using System.ComponentModel.DataAnnotations;

namespace FamilyManagement.API.Models;

public sealed class Subscription
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid FamilyId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    [MaxLength(20)]
    public string Interval { get; set; } = "monthly"; // weekly, monthly, yearly

    public DateTimeOffset? NextDueDate { get; set; }

    public Guid? LinkedBillId { get; set; }
}

