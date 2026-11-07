using Bariza.Application.DTOs.Dashboard;
using Bariza.Application.Interfaces.Repositories;
using Bariza.Application.Interfaces.Services;

namespace Bariza.Application.Services;

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

        // The repository already groups items case-insensitively, so we only
        // need to sort and take the top/bottom items in memory.
        var topItems = snapshot.Items
            .OrderByDescending(item => item.Qty)
            .ThenBy(item => item.ItemName)
            .Take(TopItemCount)
            .Select(item => new TopItemDto
            {
                ItemName = item.ItemName,
                SkuCode = item.SkuCode,
                TotalQuantity = item.Qty,
                WarehouseCount = item.WarehouseCount,
            })
            .ToList();

        var lowItems = snapshot.Items
            .OrderBy(item => item.Qty)
            .ThenBy(item => item.ItemName)
            .Take(TopItemCount)
            .Select(item => new TopItemDto
            {
                ItemName = item.ItemName,
                SkuCode = item.SkuCode,
                TotalQuantity = item.Qty,
                WarehouseCount = item.WarehouseCount,
            })
            .ToList();

        return new DashboardDto
        {
            WarehouseCount = snapshot.Warehouses.Count,
            ItemCount = snapshot.Items.Count,
            TotalQuantity = snapshot.Items.Sum(item => item.Qty),
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
