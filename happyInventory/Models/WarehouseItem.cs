using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace happyInventory.Models;

public class WarehouseItem
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string ItemName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? SKUCode { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Qty { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MSRPPrice { get; set; }

    [Required]
    public int WarehouseId { get; set; }

    public Warehouse? Warehouse { get; set; }
}
