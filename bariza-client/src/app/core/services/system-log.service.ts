import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/paged-result.model';
import {
  SystemLog,
  SystemLogCleanupResult,
  SystemLogFilter,
} from '../models/system-log.model';

@Injectable({ providedIn: 'root' })
export class SystemLogService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/system-logs`;

  list(filter: SystemLogFilter = {}): Observable<PagedResult<SystemLog>> {
    let params = new HttpParams()
      .set('page', (filter.page ?? 1).toString())
      .set('pageSize', (filter.pageSize ?? 25).toString());

    if (filter.level) {
      params = params.set('level', filter.level);
    }

    if (filter.search) {
      params = params.set('search', filter.search);
    }

    if (filter.fromUtc) {
      params = params.set('fromUtc', filter.fromUtc);
    }

    if (filter.toUtc) {
      params = params.set('toUtc', filter.toUtc);
    }

    if (filter.userId) {
      params = params.set('userId', filter.userId);
    }

    return this.http.get<PagedResult<SystemLog>>(this.baseUrl, { params });
  }

  get(id: number): Observable<SystemLog> {
    return this.http.get<SystemLog>(`${this.baseUrl}/${id}`);
  }

  cleanup(olderThanDays: number): Observable<SystemLogCleanupResult> {
    const params = new HttpParams().set('olderThanDays', olderThanDays.toString());
    return this.http.delete<SystemLogCleanupResult>(this.baseUrl, { params });
  }
}
