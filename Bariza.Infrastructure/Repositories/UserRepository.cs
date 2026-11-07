using Bariza.Application.Interfaces.Repositories;
using Bariza.Application.Models;
using Bariza.Core.Entities;
using Bariza.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bariza.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Users
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .ToListAsync(cancellationToken);
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public Task<User?> GetByNormalizedUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken = default)
    {
        return _db.Users.FirstOrDefaultAsync(
            user => user.NormalizedUsername == normalizedUsername,
            cancellationToken);
    }

    public async Task<UserAuthState?> GetAuthStateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var state = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new { user.Id, user.IsActive, user.Role })
            .FirstOrDefaultAsync(cancellationToken);

        return state is null
            ? null
            : new UserAuthState(
                state.Id,
                state.IsActive,
                state.Role);
    }

    public Task<bool> UsernameExistsAsync(
        string normalizedUsername,
        int? excludedId,
        CancellationToken cancellationToken = default)
    {
        return _db.Users.AnyAsync(
            user =>
                user.NormalizedUsername == normalizedUsername &&
                (excludedId == null || user.Id != excludedId),
            cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _db.Users.AddAsync(user, cancellationToken);
    }

    public void Remove(User user)
    {
        _db.Users.Remove(user);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
