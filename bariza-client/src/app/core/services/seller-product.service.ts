import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CreateSellerProductRequest,
  SellerProduct,
  UpdateSellerProductRequest,
} from '../models/seller-product.model';

@Injectable({ providedIn: 'root' })
export class SellerProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/seller/products`;

  getProducts(): Observable<SellerProduct[]> {
    return this.http.get<SellerProduct[]>(this.baseUrl);
  }

  getCategories(): Observable<{ id: number; name: string }[]> {
    return this.http.get<{ id: number; name: string }[]>(`${this.baseUrl}/categories`);
  }

  create(request: CreateSellerProductRequest): Observable<SellerProduct> {
    return this.http.post<SellerProduct>(this.baseUrl, request);
  }

  update(id: number, request: UpdateSellerProductRequest): Observable<SellerProduct> {
    return this.http.put<SellerProduct>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
