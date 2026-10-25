using HappyInventory.Core.Entities;

namespace HappyInventory.Application.Interfaces.Repositories;

/// <summary>
/// Persistence boundary for the eCommerce inventory deduction use case. The
/// stock change and the idempotency record are committed by one SaveChanges call.
/// </summary>
public interface IInventoryUpdateRepository
{
    Task<InventoryUpdateRecord?> GetIdempotencyRecordAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseItem>> FindBySkuAsync(
        int warehouseId,
        string skuCode,
        CancellationToken cancellationToken = default);

    void AddIdempotencyRecord(InventoryUpdateRecord record);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
