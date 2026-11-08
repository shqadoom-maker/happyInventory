export interface SellerProduct {
  id: number;
  name: string;
  slug: string;
  description?: string | null;
  sku?: string | null;
  price: number;
  discountPrice?: number | null;
  imageUrl?: string | null;
  categoryId: number;
  categoryName: string;
  isPublished: boolean;
  isActive: boolean;
  approvalStatus: string;
}

export interface CreateSellerProductRequest {
  name: string;
  slug: string;
  description?: string | null;
  sku?: string | null;
  price: number;
  discountPrice?: number | null;
  imageUrl?: string | null;
  categoryId: number;
  isPublished: boolean;
}

export interface UpdateSellerProductRequest extends CreateSellerProductRequest {
  isActive: boolean;
}
