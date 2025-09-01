using System.ComponentModel.DataAnnotations;

namespace FamilyManagement.API.Models;

public sealed class Membership
{
    [Required]
    public Guid FamilyId { get; set; }

    [Required]
    [MaxLength(50)]
    public string UserId { get; set; } = string.Empty; // Supabase auth user id (UUID string)

    [MaxLength(20)]
    public string Role { get; set; } = "member"; // owner, admin, member
}

