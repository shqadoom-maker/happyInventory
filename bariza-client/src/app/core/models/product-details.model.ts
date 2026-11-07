export interface ProductImage {
  id: number;
  url: string;
  sortOrder: number;
  isPrimary: boolean;
}

export interface ProductVariant {
  id: number;
  name: string;
  value: string;
  priceOverride?: number | null;
  stock: number;
  isActive: boolean;
}

export interface ProductSpecification {
  id: number;
  key: string;
  value: string;
  sortOrder: number;
}

export interface ProductReview {
  id: number;
  userId: number;
  userName: string;
  rating: number;
  comment?: string | null;
  createdUtc: string;
}

export interface ReviewSummary {
  averageRating: number;
  totalReviews: number;
  distribution: Record<number, number>;
}

export interface ProductDetails {
  id: number;
  name: string;
  slug: string;
  sku?: string | null;
  description?: string | null;
  imageUrl?: string | null;
  price: number;
  discountPrice?: number | null;
  categoryId?: number | null;
  categoryName: string;
  sellerProfileId?: number | null;
  sellerName?: string | null;
  sellerLogoUrl?: string | null;
  isAvailable: boolean;
  stockQuantity: number;
  averageRating: number;
  reviewCount: number;
  images: ProductImage[];
  variants: ProductVariant[];
  specifications: ProductSpecification[];
  relatedProducts: any[];
}

export interface WishlistItem {
  id: number;
  productId: number;
  productName: string;
  imageUrl?: string | null;
  price: number;
}
