using HappyInventory.Application.DTOs.Users;

namespace HappyInventory.Application.Interfaces.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresUtc) CreateToken(UserDto user);
}
