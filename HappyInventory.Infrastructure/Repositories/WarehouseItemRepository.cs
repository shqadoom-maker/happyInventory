using HappyInventory.Application.Common;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Core.Entities;
using HappyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyInventory.Infrastructure.Repositories;

public class WarehouseItemRepository : IWarehouseItemRepository
{
    private readonly AppDbContext _db;

    public WarehouseItemRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<WarehouseItem> Items, int TotalCount)> GetPagedAsync(
        int warehouseId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _db.WarehouseItems
            .AsNoTracking()
            .Include(item => item.Warehouse)
            .Where(item => item.WarehouseId == warehouseId);

        var term = search?.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(item =>
                item.ItemName.ToLower().Contains(term) ||
                (item.SKUCode != null && item.SKUCode.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(item => item.ItemName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<WarehouseItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.WarehouseItems
            .Include(item => item.Warehouse)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<WarehouseItem?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        return _db.WarehouseItems
            .Include(item => item.Warehouse)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
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

    public Task<Warehouse?> GetWarehouseAsync(
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        return _db.Warehouses.FirstOrDefaultAsync(
            warehouse => warehouse.Id == warehouseId,
            cancellationToken);
    }

    public Task<bool> AnyDuplicateNameAsync(
        string name,
        int warehouseId,
        int? excludedId,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.ToLowerInvariant();

        return _db.WarehouseItems.AnyAsync(
            item =>
                (excludedId == null || item.Id != excludedId) &&
                item.WarehouseId == warehouseId &&
                item.ItemName.ToLower() == normalized,
            cancellationToken);
    }

    public async Task AddAsync(WarehouseItem item, CancellationToken cancellationToken = default)
    {
        await _db.WarehouseItems.AddAsync(item, cancellationToken);
    }

    public void Remove(WarehouseItem item)
    {
        _db.WarehouseItems.Remove(item);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Qty is a concurrency token. A concurrent stock update must not be
            // silently overwritten; the caller can retry against fresh stock.
            throw new ConflictException(
                "The warehouse item was changed by another request. Please retry the inventory update.");
        }
    }
}
