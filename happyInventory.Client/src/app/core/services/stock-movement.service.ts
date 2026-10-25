import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { StockMovement } from '../models/stock-movement.model';

@Injectable({ providedIn: 'root' })
export class StockMovementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/stock-movements`;

  list(warehouseId?: number): Observable<StockMovement[]> {
    let params = new HttpParams();

    if (warehouseId) {
      params = params.set('warehouseId', warehouseId);
    }

    return this.http.get<StockMovement[]>(this.baseUrl, { params });
  }
}
