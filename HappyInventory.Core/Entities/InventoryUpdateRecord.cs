using System.ComponentModel.DataAnnotations;

namespace HappyInventory.Core.Entities;

/// <summary>
/// Stores the outcome of an idempotent eCommerce inventory deduction so a
/// retried request can return the original result without deducting stock again.
/// </summary>
public class InventoryUpdateRecord
{
    [Key]
    [MaxLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;

    public int WarehouseId { get; set; }

    [Required]
    [MaxLength(50)]
    public string SKUCode { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public int WarehouseItemId { get; set; }

    [Required]
    [MaxLength(150)]
    public string ItemName { get; set; } = string.Empty;

    public int PreviousQuantity { get; set; }

    public int RemainingQuantity { get; set; }

    public DateTime CreatedUtc { get; set; }
}
