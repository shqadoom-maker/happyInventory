export interface Dashboard {
  warehouseCount: number;
  itemCount: number;
  totalQuantity: number;
  warehouses: WarehouseStatus[];
  topItems: DashboardItem[];
  lowItems: DashboardItem[];
}

export interface WarehouseStatus {
  warehouseId: number;
  name: string;
  cityName: string;
  countryName: string;
  itemCount: number;
  totalQuantity: number;
}

export interface DashboardItem {
  itemName: string;
  skuCode?: string | null;
  totalQuantity: number;
  warehouseCount: number;
}
