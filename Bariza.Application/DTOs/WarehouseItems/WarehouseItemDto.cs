using System.ComponentModel.DataAnnotations;

namespace Bariza.Application.DTOs.WarehouseItems;

public class WarehouseItemDto
{
    public int Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? SKUCode { get; set; }
    public int Qty { get; set; }
    public decimal CostPrice { get; set; }
    public decimal? MSRPPrice { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
}

public class WarehouseItemRequest
{
    [Required]
    [MaxLength(150)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SKUCode { get; set; }

    [Range(1, int.MaxValue)]
    public int Qty { get; set; }

    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal CostPrice { get; set; }

    [Range(typeof(decimal), "0", "1000000000000")]
    public decimal? MSRPPrice { get; set; }

    [Range(1, int.MaxValue)]
    public int WarehouseId { get; set; }
}
