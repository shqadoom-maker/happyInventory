import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  MarketplaceCategory,
  MarketplaceProduct,
  MarketplaceProductPage,
  MarketplaceProductQuery,
} from '../models/marketplace.model';
import {
  ProductDetails,
  ProductReview,
  ReviewSummary,
  WishlistItem,
} from '../models/product-details.model';

@Injectable({ providedIn: 'root' })
export class MarketplaceProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/marketplace`;

  getProducts(query: MarketplaceProductQuery): Observable<MarketplaceProductPage> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber ?? 1)
      .set('pageSize', query.pageSize ?? 12)
      .set('sortBy', query.sortBy ?? 'newest');

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    if (query.categoryId != null) {
      params = params.set('categoryId', query.categoryId);
    }

    if (query.minPrice != null) {
      params = params.set('minPrice', query.minPrice);
    }

    if (query.maxPrice != null) {
      params = params.set('maxPrice', query.maxPrice);
    }

    return this.http.get<MarketplaceProductPage>(`${this.baseUrl}/products`, { params });
  }

  getCategories(): Observable<MarketplaceCategory[]> {
    return this.http.get<MarketplaceCategory[]>(`${this.baseUrl}/categories`);
  }

  getProductDetails(slug: string): Observable<ProductDetails> {
    return this.http.get<ProductDetails>(`${this.baseUrl}/products/slug/${slug}`);
  }

  getRelatedProducts(id: number): Observable<MarketplaceProduct[]> {
    return this.http.get<MarketplaceProduct[]>(`${this.baseUrl}/products/${id}/related`);
  }

  getReviewSummary(id: number): Observable<ReviewSummary> {
    return this.http.get<ReviewSummary>(`${this.baseUrl}/products/${id}/reviews/summary`);
  }

  getReviews(id: number, page = 1, pageSize = 10): Observable<ProductReview[]> {
    return this.http.get<ProductReview[]>(`${this.baseUrl}/products/${id}/reviews`, {
      params: { page, pageSize },
    });
  }

  addReview(id: number, rating: number, comment?: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/products/${id}/reviews`, { rating, comment });
  }

  getWishlist(): Observable<WishlistItem[]> {
    return this.http.get<WishlistItem[]>(`${environment.apiBaseUrl}/wishlist`);
  }

  addToWishlist(productId: number): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/wishlist/${productId}`, {});
  }

  removeFromWishlist(productId: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/wishlist/${productId}`);
  }
}
