import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ChangePasswordRequest,
  LoginRequest,
  LoginResponse,
  User,
} from '../models/user.model';

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

  login(request: LoginRequest, remember = true): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/login`, request).pipe(
      tap((response) => this.storeSession(response, remember)),
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/change-password`, request).pipe(
      tap((response) => this.storeSession(response, this.usesPersistentStorage())),
    );
  }

  logout(): void {
    this.currentUser.set(null);
    this.currentToken.set(null);
    this.clearStoredSession();
  }

  private storeSession(response: LoginResponse, remember: boolean): void {
    this.currentToken.set(response.token);
    this.currentUser.set(response.user);

    try {
      this.clearStoredSession();

      // "Remember me" keeps the JWT across browser restarts; otherwise the
      // session lives only in this tab.
      const storage = remember ? localStorage : sessionStorage;
      storage.setItem(TOKEN_KEY, response.token);
      storage.setItem(USER_KEY, JSON.stringify(response.user));
    } catch {
      // Keep working for this tab even when storage is unavailable.
    }
  }

  private restoreSession(): void {
    try {
      const storage = this.sessionStorageWithToken() ?? this.localStorageWithToken();

      if (!storage) {
        this.logout();
        return;
      }

      const token = storage.getItem(TOKEN_KEY);
      const rawUser = storage.getItem(USER_KEY);

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

  private clearStoredSession(): void {
    try {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      sessionStorage.removeItem(TOKEN_KEY);
      sessionStorage.removeItem(USER_KEY);
    } catch {
      // Storage can be blocked; the in-memory session is still cleared.
    }
  }

  private usesPersistentStorage(): boolean {
    try {
      return localStorage.getItem(TOKEN_KEY) !== null;
    } catch {
      return false;
    }
  }

  private sessionStorageWithToken(): Storage | null {
    return sessionStorage.getItem(TOKEN_KEY) ? sessionStorage : null;
  }

  private localStorageWithToken(): Storage | null {
    return localStorage.getItem(TOKEN_KEY) ? localStorage : null;
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
