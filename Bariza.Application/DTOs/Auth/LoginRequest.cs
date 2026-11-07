using System.ComponentModel.DataAnnotations;

namespace Bariza.Application.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Keeps the refresh cookie after the browser closes when true. The access
    /// token is still stored client-side according to the same choice.
    /// </summary>
    public bool RememberMe { get; set; } = true;
}
