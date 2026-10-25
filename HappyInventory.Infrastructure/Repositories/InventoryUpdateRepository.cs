using HappyInventory.Application.Common;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Core.Entities;
using HappyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyInventory.Infrastructure.Repositories;

public class InventoryUpdateRepository : IInventoryUpdateRepository
{
    private readonly AppDbContext _db;

    public InventoryUpdateRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<InventoryUpdateRecord?> GetIdempotencyRecordAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return _db.InventoryUpdateRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                record => record.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseItem>> FindBySkuAsync(
        int warehouseId,
        string skuCode,
        CancellationToken cancellationToken = default)
    {
        var normalized = skuCode.Trim().ToLowerInvariant();

        return await _db.WarehouseItems
            .Where(item =>
                item.WarehouseId == warehouseId &&
                item.SKUCode != null &&
                item.SKUCode.ToLower() == normalized)
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public void AddIdempotencyRecord(InventoryUpdateRecord record)
    {
        _db.InventoryUpdateRecords.Add(record);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // The stock change and the idempotency record are saved in the same
            // EF Core transaction. Either both are persisted or neither is.
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Qty is a concurrency token. A concurrent stock update must not be
            // silently overwritten; the caller can retry against fresh stock.
            throw new ConflictException(
                "The warehouse item was changed by another request. Please retry the inventory update.");
        }
        catch (DbUpdateException)
        {
            // This also covers a concurrent request that used the same
            // Idempotency-Key before this transaction could commit it.
            throw new ConflictException(
                "The inventory update conflicts with another request. Retry with the same Idempotency-Key.");
        }
    }
}
