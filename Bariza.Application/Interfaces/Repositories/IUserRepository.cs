using Bariza.Application.Models;
using Bariza.Core.Entities;

namespace Bariza.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);
    Task<UserAuthState?> GetAuthStateAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string normalizedUsername, int? excludedId, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Remove(User user);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
