import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  InventoryAdjustmentRequest,
  InventoryItem,
} from '../models/inventory.model';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/inventory`;

  /** Stock levels for one warehouse. */
  listByWarehouse(warehouseId: number): Observable<InventoryItem[]> {
    const params = new HttpParams().set('warehouseId', warehouseId);
    return this.http.get<InventoryItem[]>(this.baseUrl, { params });
  }

  get(warehouseId: number, productId: number): Observable<InventoryItem> {
    return this.http.get<InventoryItem>(`${this.baseUrl}/${warehouseId}/${productId}`);
  }

  /**
   * Applies a signed quantity change and records a stock movement.
   * Used by the UI; external systems use the integration endpoint instead.
   */
  adjust(
    warehouseId: number,
    productId: number,
    request: InventoryAdjustmentRequest,
  ): Observable<InventoryItem> {
    return this.http.post<InventoryItem>(
      `${this.baseUrl}/${warehouseId}/${productId}/adjust`,
      request,
    );
  }

  updateReorderLevel(
    warehouseId: number,
    productId: number,
    reorderLevel: number,
  ): Observable<InventoryItem> {
    return this.http.patch<InventoryItem>(
      `${this.baseUrl}/${warehouseId}/${productId}/reorder-level`,
      { reorderLevel },
    );
  }
}
