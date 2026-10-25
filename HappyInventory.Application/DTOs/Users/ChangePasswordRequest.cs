using System.ComponentModel.DataAnnotations;

namespace HappyInventory.Application.DTOs.Users;

public class ChangePasswordRequest
{
    [Required]
    [MinLength(8)]
    [MaxLength(200)]
    public string NewPassword { get; set; } = string.Empty;
}
