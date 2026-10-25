import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, User } from '../models/user.model';

const TOKEN_KEY = 'happy-inventory.token';
const USER_KEY = 'happy-inventory.user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  private readonly currentUser = signal<User | null>(null);
  private readonly currentToken = signal<string | null>(null);

  readonly user = this.currentUser.asReadonly();
  readonly token = this.currentToken.asReadonly();
  readonly isAuthenticated = computed(
    () => this.currentToken() !== null && this.currentUser() !== null,
  );
  readonly isAdmin = computed(() => this.currentUser()?.role === 'Admin');

  constructor() {
    this.restoreSession();
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/login`, request).pipe(
      tap((response) => this.storeSession(response)),
    );
  }

  logout(): void {
    this.currentUser.set(null);
    this.currentToken.set(null);

    try {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
    } catch {
      // Storage can be blocked; the in-memory session is still cleared.
    }
  }

  private storeSession(response: LoginResponse): void {
    this.currentToken.set(response.token);
    this.currentUser.set(response.user);

    try {
      localStorage.setItem(TOKEN_KEY, response.token);
      localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    } catch {
      // Keep working for this tab even when storage is unavailable.
    }
  }

  private restoreSession(): void {
    try {
      const token = localStorage.getItem(TOKEN_KEY);
      const rawUser = localStorage.getItem(USER_KEY);

      if (!token || !rawUser || this.isExpired(token)) {
        this.logout();
        return;
      }

      const user = JSON.parse(rawUser) as User;

      if (!user?.id || !user?.role) {
        this.logout();
        return;
      }

      this.currentToken.set(token);
      this.currentUser.set(user);
    } catch {
      this.logout();
    }
  }

  private isExpired(token: string): boolean {
    try {
      const payload = token.split('.')[1];

      if (!payload) {
        return true;
      }

      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const decoded = JSON.parse(atob(normalized)) as { exp?: number };

      return typeof decoded.exp === 'number' ? decoded.exp * 1000 <= Date.now() : true;
    } catch {
      return true;
    }
  }
}
