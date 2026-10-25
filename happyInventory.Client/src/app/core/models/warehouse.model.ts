export interface Warehouse {
  id: number;
  name: string;
  address: string;
  cityId: number;
  cityName: string;
  countryId: number;
  countryName: string;
  itemCount: number;
}

export interface WarehouseRequest {
  name: string;
  address: string;
  cityId: number;
  countryId: number;
}
