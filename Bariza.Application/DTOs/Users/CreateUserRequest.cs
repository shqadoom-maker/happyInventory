using System.ComponentModel.DataAnnotations;

namespace Bariza.Application.DTOs.Users;

public class CreateUserRequest
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

    [Required]
    [MinLength(8)]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
