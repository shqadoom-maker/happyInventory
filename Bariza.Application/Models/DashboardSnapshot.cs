namespace Bariza.Application.Models;

public sealed record WarehouseSummary(
    int WarehouseId,
    string Name,
    string CityName,
    string CountryName,
    int ItemCount,
    int TotalQuantity);

public sealed record WarehouseItemSummary(
    string ItemName,
    string? SkuCode,
    int Qty,
    int WarehouseCount);

public sealed record DashboardSnapshot(
    IReadOnlyList<WarehouseSummary> Warehouses,
    IReadOnlyList<WarehouseItemSummary> Items);
