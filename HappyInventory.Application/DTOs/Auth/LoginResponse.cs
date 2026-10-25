using HappyInventory.Application.DTOs.Users;

namespace HappyInventory.Application.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresUtc { get; set; }

    public UserDto User { get; set; } = new();
}
