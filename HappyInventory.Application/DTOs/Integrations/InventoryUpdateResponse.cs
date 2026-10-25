namespace HappyInventory.Application.DTOs.Integrations;

public class InventoryUpdateResponse
{
    public int WarehouseItemId { get; set; }
    public int WarehouseId { get; set; }
    public string SKUCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int PreviousQuantity { get; set; }
    public int RemainingQuantity { get; set; }
}
