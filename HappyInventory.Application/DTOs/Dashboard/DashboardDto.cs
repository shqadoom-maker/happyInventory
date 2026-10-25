namespace HappyInventory.Application.DTOs.Dashboard;

public class DashboardDto
{
    public int WarehouseCount { get; set; }
    public int ItemCount { get; set; }
    public int TotalQuantity { get; set; }
    public IReadOnlyList<WarehouseStatusDto> Warehouses { get; set; } = Array.Empty<WarehouseStatusDto>();
    public IReadOnlyList<TopItemDto> TopItems { get; set; } = Array.Empty<TopItemDto>();
    public IReadOnlyList<TopItemDto> LowItems { get; set; } = Array.Empty<TopItemDto>();
}

public class WarehouseStatusDto
{
    public int WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string CountryName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int TotalQuantity { get; set; }
}

public class TopItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public string? SkuCode { get; set; }
    public int TotalQuantity { get; set; }
    public int WarehouseCount { get; set; }
}
