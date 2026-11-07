namespace Bariza.Application.DTOs.Integrations;

public class InventoryUpdateResponse
{
    public int WarehouseItemId { get; set; }
    public int WarehouseId { get; set; }
    public string SKUCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int PreviousQuantity { get; set; }
    public int RemainingQuantity { get; set; }

    /// <summary>
    /// True when this response was returned from a stored idempotency record,
    /// so the caller can tell a retry from a newly applied deduction.
    /// </summary>
    public bool Replayed { get; set; }
}
