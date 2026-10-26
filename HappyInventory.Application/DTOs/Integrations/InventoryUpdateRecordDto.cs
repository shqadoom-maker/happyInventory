namespace HappyInventory.Application.DTOs.Integrations;

/// <summary>
/// Read-only summary of a completed eCommerce inventory deduction,
/// used for monitoring integration activity.
/// </summary>
public class InventoryUpdateRecordDto
{
    public string IdempotencyKey { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string SKUCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int PreviousQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public DateTime CreatedUtc { get; set; }
}
