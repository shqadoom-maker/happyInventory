using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Core.Entities;
using HappyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyInventory.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;

    public RefreshTokenRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        await _db.RefreshTokens.AddAsync(token, cancellationToken);
    }

    public Task<RefreshToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _db.RefreshTokens.FirstOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);
    }

    public Task RevokeAllForUserAsync(
        int userId,
        DateTime revokedUtc,
        CancellationToken cancellationToken = default)
    {
        // A set-based update avoids loading every token and avoids the
        // rotation concurrency token for bulk revocation.
        return _db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE RefreshTokens
               SET RevokedUtc = {revokedUtc}
               WHERE UserId = {userId}
                 AND RevokedUtc IS NULL
                 AND ExpiresUtc > {revokedUtc}",
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TrySaveRotationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The original token was revoked by a concurrent refresh request.
            // EF rolls back the whole SaveChanges, including the new token row.
            return false;
        }
    }
}
