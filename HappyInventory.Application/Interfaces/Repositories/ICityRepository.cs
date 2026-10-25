using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Interfaces.Repositories;

public interface ICityRepository
{
    Task<IReadOnlyList<City>> GetAllAsync(int? countryId, CancellationToken cancellationToken = default);
    Task<City?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CountryExistsAsync(int countryId, CancellationToken cancellationToken = default);
    Task<bool> AnyDuplicateAsync(string name, int countryId, int? excludedId, CancellationToken cancellationToken = default);
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(City city, CancellationToken cancellationToken = default);
    void Remove(City city);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
