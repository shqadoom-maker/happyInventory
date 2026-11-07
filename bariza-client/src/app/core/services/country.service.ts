import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { Country, CountryRequest } from '../models/country.model';

@Injectable({ providedIn: 'root' })
export class CountryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/countries`;

  list(): Observable<Country[]> {
    return this.http.get<Country[]>(this.baseUrl);
  }

  get(id: number): Observable<Country> {
    return this.http.get<Country>(`${this.baseUrl}/${id}`);
  }

  create(request: CountryRequest): Observable<Country> {
    return this.http.post<Country>(this.baseUrl, request);
  }

  update(id: number, request: CountryRequest): Observable<Country> {
    return this.http.put<Country>(`${this.baseUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
