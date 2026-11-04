using System.ComponentModel.DataAnnotations;

namespace HappyInventory.Application.DTOs.Users;

public class ChangePasswordRequest
{
    [Required]
    [MaxLength(200)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(200)]
    public string NewPassword { get; set; } = string.Empty;
}
