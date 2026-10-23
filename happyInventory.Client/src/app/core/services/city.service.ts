import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { City, CityRequest } from '../models/city.model';

@Injectable({ providedIn: 'root' })
export class CityService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/cities`;

  list(countryId?: number): Observable<City[]> {
    const params = countryId ? new HttpParams().set('countryId', countryId) : undefined;
    return this.http.get<City[]>(this.baseUrl, { params });
  }

  get(id: number): Observable<City> {
    return this.http.get<City>(`${this.baseUrl}/${id}`);
  }

  create(request: CityRequest): Observable<City> {
    return this.http.post<City>(this.baseUrl, request);
  }

  update(id: number, request: CityRequest): Observable<City> {
    return this.http.put<City>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
