using happyInventory.Data;
using happyInventory.DTOs.Dashboard;
using happyInventory.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace happyInventory.Services;

public class DashboardService : IDashboardService
{
    private const int TopItemCount = 10;
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Select(warehouse => new WarehouseStatusDto
            {
                WarehouseId = warehouse.Id,
                Name = warehouse.Name,
                CityName = warehouse.City != null ? warehouse.City.Name : string.Empty,
                CountryName = warehouse.Country != null ? warehouse.Country.Name : string.Empty,
                ItemCount = warehouse.Items.Count,
                TotalQuantity = warehouse.Items.Sum(item => (int?)item.Qty) ?? 0,
            })
            .OrderBy(warehouse => warehouse.Name)
            .ToListAsync(cancellationToken);

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

        // Grouping is done in memory so item names compare case-insensitively
        // across warehouses regardless of the database collation.
        var groupedItems = itemRows
            .GroupBy(item => item.ItemName.Trim().ToLowerInvariant())
            .Select(group => new TopItemDto
            {
                ItemName = group.First().ItemName.Trim(),
                SkuCode = group
                    .Select(item => item.SKUCode)
                    .FirstOrDefault(sku => !string.IsNullOrWhiteSpace(sku)),
                TotalQuantity = group.Sum(item => item.Qty),
                WarehouseCount = group.Select(item => item.WarehouseId).Distinct().Count(),
            })
            .ToList();

        var topItems = groupedItems
            .OrderByDescending(item => item.TotalQuantity)
            .ThenBy(item => item.ItemName)
            .Take(TopItemCount)
            .ToList();

        var lowItems = groupedItems
            .OrderBy(item => item.TotalQuantity)
            .ThenBy(item => item.ItemName)
            .Take(TopItemCount)
            .ToList();

        return new DashboardDto
        {
            WarehouseCount = warehouses.Count,
            ItemCount = groupedItems.Count,
            TotalQuantity = groupedItems.Sum(item => item.TotalQuantity),
            Warehouses = warehouses,
            TopItems = topItems,
            LowItems = lowItems,
        };
    }
}
