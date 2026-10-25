using System.ComponentModel.DataAnnotations;

namespace happyInventory.DTOs.Warehouses;

public class WarehouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class WarehouseRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CityId { get; set; }

    [Range(1, int.MaxValue)]
    public int CountryId { get; set; }
}
