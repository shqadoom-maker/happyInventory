export type StockMovementType = 'Inbound' | 'Outbound' | 'Adjustment';

export interface InventoryItem {
  id: number;
  warehouseId: number;
  warehouseName?: string;
  productId: number;
  productName?: string;
  sku?: string;
  quantity: number;
  reorderLevel: number;
  lastUpdatedUtc: string;
}

export interface InventoryAdjustmentRequest {
  quantityChange: number;
  type: StockMovementType;
  reference?: string;
}
