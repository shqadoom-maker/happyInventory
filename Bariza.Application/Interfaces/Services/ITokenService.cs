using Bariza.Application.DTOs.Users;

namespace Bariza.Application.Interfaces.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresUtc) CreateToken(UserDto user);
}
