using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Interfaces.Repositories;

public interface IWarehouseRepository
{
    Task<(IReadOnlyList<Warehouse> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Warehouse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Warehouse?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task<City?> GetCityWithCountryAsync(int cityId, CancellationToken cancellationToken = default);
    Task<bool> AnyDuplicateNameAsync(string name, int? excludedId, CancellationToken cancellationToken = default);
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default);
    void Remove(Warehouse warehouse);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
