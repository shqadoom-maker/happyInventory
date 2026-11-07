using HappyInventory.Application.DTOs.Users;
using HappyInventory.Application.Models;

namespace HappyInventory.Application.Interfaces.Services;

public interface IAuthSessionService
{
    Task<AuthSession> CreateAsync(
        UserDto user,
        bool isPersistent,
        CancellationToken cancellationToken = default);

    Task<AuthSession> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task<bool?> GetPersistenceAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
