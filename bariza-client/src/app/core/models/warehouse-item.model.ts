export interface WarehouseItem {
  id: number;
  itemName: string;
  skuCode?: string | null;
  qty: number;
  costPrice: number;
  msrpPrice?: number | null;
  warehouseId: number;
  warehouseName: string;
}

export interface WarehouseItemRequest {
  itemName: string;
  skuCode?: string | null;
  qty: number;
  costPrice: number;
  msrpPrice?: number | null;
  warehouseId: number;
}
