export type MarketplaceSort = 'newest' | 'priceAsc' | 'priceDesc' | 'nameAsc';

export interface MarketplaceProduct {
  id: number;
  name: string;
  slug: string;
  sku?: string | null;
  price: number;
  discountPrice?: number | null;
  imageUrl?: string | null;
  categoryId: number;
  categoryName: string;
  sellerName?: string | null;
  isAvailable: boolean;
}

export interface MarketplaceProductPage {
  items: MarketplaceProduct[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface MarketplaceCategory {
  id: number;
  name: string;
  slug: string;
  productCount: number;
}

export interface MarketplaceProductQuery {
  search?: string;
  categoryId?: number;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: MarketplaceSort;
  pageNumber?: number;
  pageSize?: number;
}
