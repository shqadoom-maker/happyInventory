import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  Order,
  SellerOrder,
  ShippingAddressRequest,
  UpdateOrderStatusRequest,
} from '../models/order.model';

@Injectable({ providedIn: 'root' })
export class OrderApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}`;

  checkout(request: ShippingAddressRequest, idempotencyKey?: string): Observable<Order> {
    return this.http.post<Order>(`${this.baseUrl}/checkout`, request, {
      headers: idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : {},
    });
  }

  getMyOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(`${this.baseUrl}/orders/my-orders`);
  }

  getOrder(id: number): Observable<Order> {
    return this.http.get<Order>(`${this.baseUrl}/orders/${id}`);
  }

  cancelOrder(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/orders/${id}/cancel`, {});
  }

  getSellerOrders(): Observable<SellerOrder[]> {
    return this.http.get<SellerOrder[]>(`${this.baseUrl}/seller/orders`);
  }

  getSellerOrder(id: number): Observable<SellerOrder> {
    return this.http.get<SellerOrder>(`${this.baseUrl}/seller/orders/${id}`);
  }

  updateSellerOrderStatus(id: number, request: UpdateOrderStatusRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/seller/orders/${id}/status`, request);
  }
}
