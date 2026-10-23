export interface Product {
  id: number;
  sku: string;
  name: string;
  description?: string;
  unitPrice: number;
  isActive: boolean;
}

export interface ProductRequest {
  sku: string;
  name: string;
  description?: string;
  unitPrice: number;
  isActive: boolean;
}
