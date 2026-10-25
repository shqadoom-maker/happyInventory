using System.ComponentModel.DataAnnotations;

namespace happyInventory.DTOs.Users;

public class UpdateUserRequest
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Admin, Management or Auditor. A user has exactly one role.</summary>
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty;

    /// <summary>Optional. When empty the current password is kept.</summary>
    [MinLength(8)]
    [MaxLength(200)]
    public string? Password { get; set; }

    public bool IsActive { get; set; } = true;
}
