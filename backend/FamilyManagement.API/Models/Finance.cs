using System.ComponentModel.DataAnnotations;

namespace FamilyManagement.API.Models;

public sealed class FinanceItem
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public Guid FamilyId { get; set; }
    [MaxLength(20)]
    public string Provider { get; set; } = "plaid"; // plaid or finicity
    [MaxLength(200)]
    public string ItemId { get; set; } = string.Empty;
    [MaxLength(400)]
    public string? AccessTokenEncrypted { get; set; }
    public DateTimeOffset LinkedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class FinanceAccount
{
    [Key]
    public Guid Id { get; set; }
    [Required]
    public Guid FamilyId { get; set; }
    [Required]
    [MaxLength(200)]
    public string AccountId { get; set; } = string.Empty;
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Subtype { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

