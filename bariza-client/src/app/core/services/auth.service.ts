import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, finalize, firstValueFrom, shareReplay, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ChangePasswordRequest,
  LoginRequest,
  LoginResponse,
  User,
} from '../models/user.model';

const TOKEN_KEY = 'bariza-market.token';
const USER_KEY = 'bariza-market.user';
const SESSION_KEY = 'bariza-market.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  private readonly currentUser = signal<User | null>(null);
  private readonly currentToken = signal<string | null>(null);
  private refreshInProgress: Observable<LoginResponse> | null = null;

  readonly user = this.currentUser.asReadonly();
  readonly token = this.currentToken.asReadonly();
  readonly isAuthenticated = computed(
    () => this.currentToken() !== null && this.currentUser() !== null,
  );
  readonly isAdmin = computed(() => this.currentUser()?.role === 'Admin');

  constructor() {
    this.restoreSession();
  }

  /**
   * Refreshes the signed-in profile before the first route is activated.
   * If the access token has expired, the HttpOnly refresh cookie is used to
   * obtain a new access token instead of logging the user out immediately.
   */
  async initializeSession(): Promise<void> {
    const storage = this.getStoredSessionStorage();

    if (!storage) {
      return;
    }

    const token = storage.getItem(TOKEN_KEY);

    if (token && !this.isExpired(token)) {
      try {
        await firstValueFrom(this.refreshCurrentUser());
      } catch {
        // A rejected token is handled by authInterceptor; transient failures
        // keep the cached profile so the app can still open.
      }
      return;
    }

    try {
      await firstValueFrom(this.refreshOnce());
    } catch (error) {
      // The refresh endpoint is deliberately excluded from the interceptor's
      // automatic retry. Clear the local session only when the server rejected
      // the refresh cookie; a network failure can be retried on the next load.
      if (error instanceof HttpErrorResponse && error.status === 401) {
        this.logout();
      }
    }
  }

  refreshCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/me`).pipe(
      tap((user) => this.updateStoredUser(user)),
    );
  }

  login(request: LoginRequest, remember = true): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(
        `${this.baseUrl}/login`,
        { ...request, rememberMe: remember },
        { withCredentials: true },
      )
      .pipe(tap((response) => this.storeSession(response, remember)));
  }

  changePassword(request: ChangePasswordRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.baseUrl}/change-password`, request, {
        withCredentials: true,
      })
      .pipe(
        tap((response) => this.storeSession(response, this.usesPersistentStorage())),
      );
  }

  /**
   * Refreshes the access token. Concurrent callers share one HTTP request so a
   * burst of 401 responses cannot cause a refresh storm.
   */
  refreshOnce(): Observable<LoginResponse> {
    if (!this.refreshInProgress) {
      this.refreshInProgress = this.http
        .post<LoginResponse>(`${this.baseUrl}/refresh`, {}, { withCredentials: true })
        .pipe(
          tap((response) =>
            this.storeSession(response, this.usesPersistentStorage()),
          ),
          finalize(() => {
            this.refreshInProgress = null;
          }),
          shareReplay({ bufferSize: 1, refCount: false }),
        );
    }

    return this.refreshInProgress;
  }

  canRefresh(): boolean {
    return this.getStoredSessionStorage() !== null;
  }

  logout(): void {
    const hadSession = this.isAuthenticated() || this.canRefresh();

    this.clearSessionState();

    if (hadSession) {
      // Revoke the HttpOnly refresh cookie on the server. This is best-effort:
      // the local session is already cleared even if the request fails.
      this.http
        .post(`${this.baseUrl}/logout`, {}, { withCredentials: true })
        .subscribe({ error: () => undefined });
    }
  }

  private storeSession(response: LoginResponse, remember: boolean): void {
    this.currentToken.set(response.token);
    this.currentUser.set(response.user);

    try {
      this.clearStoredSession();

      // "Remember me" keeps the access token across browser restarts;
      // otherwise the session lives only in this tab. The refresh cookie is
      // HttpOnly and its persistence is decided by the API.
      const storage = remember ? localStorage : sessionStorage;
      storage.setItem(TOKEN_KEY, response.token);
      storage.setItem(USER_KEY, JSON.stringify(response.user));
      storage.setItem(SESSION_KEY, remember ? 'local' : 'session');
    } catch {
      // Keep working for this tab even when storage is unavailable.
    }
  }

  private updateStoredUser(user: User): void {
    this.currentUser.set(user);

    try {
      const storage = this.getStoredSessionStorage();
      storage?.setItem(USER_KEY, JSON.stringify(user));
    } catch {
      // The in-memory profile is still updated when storage is unavailable.
    }
  }

  private restoreSession(): void {
    try {
      const storage =
        this.sessionStorageWithSession() ?? this.localStorageWithSession();

      if (!storage) {
        this.clearSessionState();
        return;
      }

      // Sessions created before refresh-token support have a token but no
      // marker. Backfill the marker so initializeSession can try a refresh.
      if (!storage.getItem(SESSION_KEY)) {
        storage.setItem(
          SESSION_KEY,
          storage === localStorage ? 'local' : 'session',
        );
      }

      const token = storage.getItem(TOKEN_KEY);
      const rawUser = storage.getItem(USER_KEY);

      if (!token) {
        // The refresh cookie may still be valid even when the access token is
        // gone; initializeSession will attempt a refresh.
        this.currentToken.set(null);
        this.currentUser.set(null);
        return;
      }

      if (this.isExpired(token)) {
        // Keep the stored session marker so initializeSession can use the
        // refresh cookie instead of forcing a new login.
        this.currentToken.set(null);
        this.currentUser.set(null);
        return;
      }

      this.currentToken.set(token);

      if (!rawUser) {
        return;
      }

      const user = JSON.parse(rawUser) as User;

      if (!user?.id || !user?.role) {
        this.clearSessionState();
        return;
      }

      this.currentUser.set(user);
    } catch {
      this.clearSessionState();
    }
  }

  private clearSessionState(): void {
    this.currentUser.set(null);
    this.currentToken.set(null);
    this.clearStoredSession();
  }

  private clearStoredSession(): void {
    try {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      localStorage.removeItem(SESSION_KEY);
      sessionStorage.removeItem(TOKEN_KEY);
      sessionStorage.removeItem(USER_KEY);
      sessionStorage.removeItem(SESSION_KEY);
    } catch {
      // Storage can be blocked; the in-memory session is still cleared.
    }
  }

  private usesPersistentStorage(): boolean {
    try {
      return (
        localStorage.getItem(SESSION_KEY) === 'local' ||
        (localStorage.getItem(SESSION_KEY) === null &&
          localStorage.getItem(TOKEN_KEY) !== null)
      );
    } catch {
      return false;
    }
  }

  private getStoredSessionStorage(): Storage | null {
    return this.sessionStorageWithSession() ?? this.localStorageWithSession();
  }

  private sessionStorageWithSession(): Storage | null {
    try {
      return sessionStorage.getItem(SESSION_KEY) !== null ||
        sessionStorage.getItem(TOKEN_KEY) !== null
        ? sessionStorage
        : null;
    } catch {
      return null;
    }
  }

  private localStorageWithSession(): Storage | null {
    try {
      return localStorage.getItem(SESSION_KEY) !== null ||
        localStorage.getItem(TOKEN_KEY) !== null
        ? localStorage
        : null;
    } catch {
      return null;
    }
  }

  private isExpired(token: string): boolean {
    try {
      const payload = token.split('.')[1];

      if (!payload) {
        return true;
      }

      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(
        normalized.length + ((4 - (normalized.length % 4)) % 4),
        '=',
      );
      const decoded = JSON.parse(atob(padded)) as { exp?: number };

      return typeof decoded.exp === 'number' ? decoded.exp * 1000 <= Date.now() : true;
    } catch {
      return true;
    }
  }
}
