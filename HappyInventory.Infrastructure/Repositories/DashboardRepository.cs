using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Models;
using HappyInventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HappyInventory.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly AppDbContext _db;

    public DashboardRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        // Warehouse-level aggregation is performed in SQL to avoid loading
        // all items into memory. Correlated subqueries are efficient for
        // per-warehouse counts and sums.
        var warehouseRows = await _db.Warehouses
            .AsNoTracking()
            .OrderBy(warehouse => warehouse.Name)
            .Select(warehouse => new WarehouseSummary(
                warehouse.Id,
                warehouse.Name,
                warehouse.City != null ? warehouse.City.Name : string.Empty,
                warehouse.Country != null ? warehouse.Country.Name : string.Empty,
                warehouse.Items.Count,
                warehouse.Items.Sum(item => (int?)item.Qty) ?? 0))
            .ToListAsync(cancellationToken);

        // SQLite cannot translate ToLowerInvariant inside a GROUP BY key.
        // Project only the four columns needed for the dashboard and group
        // case-insensitively in memory instead.
        var itemRows = await _db.WarehouseItems
            .AsNoTracking()
            .Select(item => new
            {
                item.ItemName,
                item.SKUCode,
                item.Qty,
                item.WarehouseId,
            })
            .ToListAsync(cancellationToken);

        var itemGroups = itemRows
            .GroupBy(item => item.ItemName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new WarehouseItemSummary(
                group.First().ItemName.Trim(),
                group.Select(item => item.SKUCode)
                    .FirstOrDefault(sku => !string.IsNullOrWhiteSpace(sku)),
                group.Sum(item => item.Qty),
                group.Select(item => item.WarehouseId).Distinct().Count()))
            .ToList();

        return new DashboardSnapshot(warehouseRows, itemGroups);
    }
}
