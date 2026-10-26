import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/paged-result.model';
import { InventoryUpdateRecord } from '../models/inventory-update-record.model';

@Injectable({ providedIn: 'root' })
export class StockMovementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/inventory/updates`;

  list(
    warehouseId?: number,
    page = 1,
    pageSize = 10,
    search?: string
  ): Observable<PagedResult<InventoryUpdateRecord>> {
    let params = new HttpParams();

    if (warehouseId) {
      params = params.set('warehouseId', warehouseId.toString());
    }

    params = params.set('page', page.toString());
    params = params.set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }

    return this.http.get<PagedResult<InventoryUpdateRecord>>(this.baseUrl, {
      params,
    });
  }
}
