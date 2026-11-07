export interface City {
  id: number;
  name: string;
  countryId: number;
  countryName?: string;
}

export interface CityRequest {
  name: string;
  countryId: number;
}
