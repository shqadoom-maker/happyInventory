export interface Warehouse {
  id: number;
  name: string;
  code: string;
  cityId: number;
  cityName?: string;
  countryName?: string;
  address: string;
  capacity: number;
  isActive: boolean;
}

export interface WarehouseRequest {
  name: string;
  code: string;
  cityId: number;
  address: string;
  capacity: number;
  isActive: boolean;
}
