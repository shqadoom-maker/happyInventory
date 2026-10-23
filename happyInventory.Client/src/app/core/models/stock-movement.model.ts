import { StockMovementType } from './inventory.model';

export interface StockMovement {
  id: number;
  warehouseId: number;
  warehouseName?: string;
  productId: number;
  productName?: string;
  quantityChange: number;
  type: StockMovementType;
  source: string;
  reference?: string;
  occurredUtc: string;
}
