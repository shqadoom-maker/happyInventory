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
        var warehouseRows = await _db.Warehouses
            .AsNoTracking()
            .Select(warehouse => new
            {
                warehouse.Id,
                warehouse.Name,
                CityName = warehouse.City != null ? warehouse.City.Name : string.Empty,
                CountryName = warehouse.Country != null ? warehouse.Country.Name : string.Empty,
                ItemCount = warehouse.Items.Count,
                TotalQuantity = warehouse.Items.Sum(item => (int?)item.Qty) ?? 0,
            })
            .OrderBy(warehouse => warehouse.Name)
            .ToListAsync(cancellationToken);

        var warehouses = warehouseRows
            .Select(warehouse => new WarehouseSummary(
                warehouse.Id,
                warehouse.Name,
                warehouse.CityName,
                warehouse.CountryName,
                warehouse.ItemCount,
                warehouse.TotalQuantity))
            .ToList();

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

        var items = itemRows
            .Select(item => new WarehouseItemSummary(
                item.ItemName,
                item.SKUCode,
                item.Qty,
                item.WarehouseId))
            .ToList();

        return new DashboardSnapshot(warehouses, items);
    }
}
