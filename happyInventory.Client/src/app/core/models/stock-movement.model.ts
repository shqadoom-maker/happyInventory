export type StockMovementType = 'Inbound' | 'Outbound' | 'Adjustment';

export interface StockMovement {
  id: number;
  warehouseId: number;
  warehouseName?: string;
  quantityChange: number;
  type: StockMovementType;
  source: string;
  reference?: string;
  occurredUtc: string;
}
