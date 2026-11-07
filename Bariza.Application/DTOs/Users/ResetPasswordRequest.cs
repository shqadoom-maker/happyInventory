using System.ComponentModel.DataAnnotations;

namespace Bariza.Application.DTOs.Users;

/// <summary>
/// Used by administrators to reset another user's password. The administrator
/// does not know the user's current password, so it is intentionally not required.
/// </summary>
public class ResetPasswordRequest
{
    [Required]
    [MinLength(8)]
    [MaxLength(200)]
    public string NewPassword { get; set; } = string.Empty;
}
