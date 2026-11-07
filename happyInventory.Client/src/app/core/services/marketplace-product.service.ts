import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  MarketplaceCategory,
  MarketplaceProductPage,
  MarketplaceProductQuery,
} from '../models/marketplace.model';

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
}
