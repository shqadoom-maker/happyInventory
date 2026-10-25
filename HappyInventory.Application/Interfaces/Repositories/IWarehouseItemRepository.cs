using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Interfaces.Repositories;

public interface IWarehouseItemRepository
{
    Task<(IReadOnlyList<WarehouseItem> Items, int TotalCount)> GetPagedAsync(
        int warehouseId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    Task<WarehouseItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WarehouseItem?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WarehouseItem>> FindBySkuAsync(
        int warehouseId,
        string skuCode,
        CancellationToken cancellationToken = default);

    Task<Warehouse?> GetWarehouseAsync(int warehouseId, CancellationToken cancellationToken = default);
    Task<bool> AnyDuplicateNameAsync(
        string name,
        int warehouseId,
        int? excludedId,
        CancellationToken cancellationToken = default);

    Task AddAsync(WarehouseItem item, CancellationToken cancellationToken = default);
    void Remove(WarehouseItem item);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
