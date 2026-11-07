import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/paged-result.model';
import { WarehouseItem, WarehouseItemRequest } from '../models/warehouse-item.model';

@Injectable({ providedIn: 'root' })
export class WarehouseItemService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/warehouse-items`;

  listByWarehouse(
    warehouseId: number,
    page = 1,
    pageSize = 10,
    search = '',
  ): Observable<PagedResult<WarehouseItem>> {
    let params = new HttpParams()
      .set('warehouseId', warehouseId)
      .set('page', page)
      .set('pageSize', pageSize);

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<PagedResult<WarehouseItem>>(this.baseUrl, { params });
  }

  get(id: number): Observable<WarehouseItem> {
    return this.http.get<WarehouseItem>(`${this.baseUrl}/${id}`);
  }

  create(request: WarehouseItemRequest): Observable<WarehouseItem> {
    return this.http.post<WarehouseItem>(this.baseUrl, request);
  }

  update(id: number, request: WarehouseItemRequest): Observable<WarehouseItem> {
    return this.http.put<WarehouseItem>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
