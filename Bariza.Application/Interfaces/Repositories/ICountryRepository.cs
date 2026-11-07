using Bariza.Core.Entities;

namespace Bariza.Application.Interfaces.Repositories;

public interface ICountryRepository
{
    Task<IReadOnlyList<Country>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Country?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AnyDuplicateAsync(string name, string code, int? excludedId, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Country country, CancellationToken cancellationToken = default);
    void Remove(Country country);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
