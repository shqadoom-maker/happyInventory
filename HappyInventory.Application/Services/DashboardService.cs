using HappyInventory.Application.DTOs.Dashboard;
using HappyInventory.Application.Interfaces.Repositories;
using HappyInventory.Application.Interfaces.Services;

namespace HappyInventory.Application.Services;

public class DashboardService : IDashboardService
{
    private const int TopItemCount = 10;
    private readonly IDashboardRepository _dashboard;

    public DashboardService(IDashboardRepository dashboard)
    {
        _dashboard = dashboard;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _dashboard.GetSnapshotAsync(cancellationToken);

        // Grouping is done in memory so item names compare case-insensitively
        // across warehouses regardless of the database collation.
        var groupedItems = snapshot.Items
            .GroupBy(item => item.ItemName.Trim().ToLowerInvariant())
            .Select(group => new TopItemDto
            {
                ItemName = group.First().ItemName.Trim(),
                SkuCode = group
                    .Select(item => item.SkuCode)
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
            WarehouseCount = snapshot.Warehouses.Count,
            ItemCount = groupedItems.Count,
            TotalQuantity = groupedItems.Sum(item => item.TotalQuantity),
            Warehouses = snapshot.Warehouses.Select(warehouse => new WarehouseStatusDto
            {
                WarehouseId = warehouse.WarehouseId,
                Name = warehouse.Name,
                CityName = warehouse.CityName,
                CountryName = warehouse.CountryName,
                ItemCount = warehouse.ItemCount,
                TotalQuantity = warehouse.TotalQuantity,
            }).ToList(),
            TopItems = topItems,
            LowItems = lowItems,
        };
    }
}
