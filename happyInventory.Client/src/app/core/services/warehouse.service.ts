import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/paged-result.model';
import { Warehouse, WarehouseRequest } from '../models/warehouse.model';

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/warehouses`;

  list(page = 1, pageSize = 10, search = ''): Observable<PagedResult<Warehouse>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);

    if (search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<PagedResult<Warehouse>>(this.baseUrl, { params });
  }

  listAll(): Observable<Warehouse[]> {
    return this.http.get<Warehouse[]>(`${this.baseUrl}/all`);
  }

  get(id: number): Observable<Warehouse> {
    return this.http.get<Warehouse>(`${this.baseUrl}/${id}`);
  }

  create(request: WarehouseRequest): Observable<Warehouse> {
    return this.http.post<Warehouse>(this.baseUrl, request);
  }

  update(id: number, request: WarehouseRequest): Observable<Warehouse> {
    return this.http.put<Warehouse>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
