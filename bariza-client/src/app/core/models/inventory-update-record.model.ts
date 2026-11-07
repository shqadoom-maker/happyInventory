export interface InventoryUpdateRecord {
  idempotencyKey: string;
  warehouseId: number;
  warehouseName: string;
  skuCode: string;
  itemName: string;
  quantity: number;
  previousQuantity: number;
  remainingQuantity: number;
  createdUtc: string;
}
