using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(int userId, DateTime revokedUtc, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a rotation. Returns false when another request rotated the same
    /// token first, which is treated as refresh-token reuse.
    /// </summary>
    Task<bool> TrySaveRotationAsync(CancellationToken cancellationToken = default);
}
