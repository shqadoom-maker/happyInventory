using System.ComponentModel.DataAnnotations;

namespace HappyInventory.Application.DTOs.Integrations;

/// <summary>
/// Inbound eCommerce inventory update. The quantity is deducted from the
/// existing warehouse-item stock identified by warehouse and SKU.
/// </summary>
public class InventoryUpdateRequest
{
    [Range(1, int.MaxValue)]
    public int WarehouseId { get; set; }

    [Required]
    [MaxLength(50)]
    public string SKUCode { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
