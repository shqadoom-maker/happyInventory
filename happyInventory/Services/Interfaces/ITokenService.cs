using happyInventory.DTOs.Users;

namespace happyInventory.Services.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresUtc) CreateToken(UserDto user);
}
