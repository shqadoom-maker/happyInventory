using System.ComponentModel.DataAnnotations;

namespace happyInventory.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
