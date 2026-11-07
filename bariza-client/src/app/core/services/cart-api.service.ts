import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AddCartItemRequest, Cart, UpdateCartItemRequest } from '../models/order.model';

@Injectable({ providedIn: 'root' })
export class CartApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/cart`;

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.baseUrl);
  }

  addItem(request: AddCartItemRequest): Observable<Cart> {
    return this.http.post<Cart>(`${this.baseUrl}/items`, request);
  }

  updateItem(id: number, request: UpdateCartItemRequest): Observable<Cart> {
    return this.http.put<Cart>(`${this.baseUrl}/items/${id}`, request);
  }

  removeItem(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/items/${id}`);
  }

  clear(): Observable<void> {
    return this.http.delete<void>(this.baseUrl);
  }
}
