using Bariza.Application.Common;
using Bariza.Application.DTOs.Integrations;
using Bariza.Application.Interfaces.Repositories;
using Bariza.Core.Entities;
using Bariza.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bariza.Infrastructure.Repositories;

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

    public async Task<(IReadOnlyList<InventoryUpdateRecordDto> Items, int TotalCount)> GetPagedAsync(
        int? warehouseId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _db.InventoryUpdateRecords
            .AsNoTracking()
            .Join(
                _db.WarehouseItems,
                record => record.WarehouseItemId,
                item => item.Id,
                (record, item) => new { record, item })
            .Join(
                _db.Warehouses,
                x => x.item.WarehouseId,
                warehouse => warehouse.Id,
                (x, warehouse) => new InventoryUpdateRecordDto
                {
                    IdempotencyKey = x.record.IdempotencyKey,
                    WarehouseId = x.record.WarehouseId,
                    WarehouseName = warehouse.Name,
                    SKUCode = x.record.SKUCode,
                    ItemName = x.record.ItemName,
                    Quantity = x.record.Quantity,
                    PreviousQuantity = x.record.PreviousQuantity,
                    RemainingQuantity = x.record.RemainingQuantity,
                    CreatedUtc = x.record.CreatedUtc,
                });

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        var term = search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(x =>
                x.IdempotencyKey.ToLower().Contains(term) ||
                x.SKUCode.ToLower().Contains(term) ||
                x.ItemName.ToLower().Contains(term) ||
                x.WarehouseName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
