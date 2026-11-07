import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { LoginResponse, User } from '../models/user.model';
import { authInterceptor } from '../interceptors/auth.interceptor';
import { AuthService } from './auth.service';

const TOKEN_KEY = 'happy-inventory.token';
const USER_KEY = 'happy-inventory.user';
const SESSION_KEY = 'happy-inventory.session';

const user: User = {
  id: 1,
  username: 'admin@happywarehouse.com',
  fullName: 'System Administrator',
  role: 'Admin',
  isActive: true,
  createdUtc: new Date('2026-01-01T00:00:00Z').toISOString(),
  lastLoginUtc: null,
};

describe('AuthService', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
    sessionStorage.clear();
  });

  it('sends rememberMe and credentials on login and stores the session', () => {
    const auth = TestBed.inject(AuthService);
    const response = loginResponse(tokenWithExpiry(3600));

    const result = firstValueFrom(auth.login({ username: 'admin', password: 'pw' }, true));
    const request = httpMock.expectOne('/api/auth/login');

    expect(request.request.method).toBe('POST');
    expect(request.request.withCredentials).toBe(true);
    expect(request.request.body).toEqual({
      username: 'admin',
      password: 'pw',
      rememberMe: true,
    });

    request.flush(response);

    return result.then(() => {
      expect(auth.isAuthenticated()).toBe(true);
      expect(localStorage.getItem(TOKEN_KEY)).toBe(response.token);
      expect(localStorage.getItem(SESSION_KEY)).toBe('local');
    });
  });

  it('refreshes an expired access token using the stored session', async () => {
    localStorage.setItem(TOKEN_KEY, tokenWithExpiry(-60));
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    localStorage.setItem(SESSION_KEY, 'local');

    const auth = TestBed.inject(AuthService);
    expect(auth.isAuthenticated()).toBe(false);

    const initialization = auth.initializeSession();
    const request = httpMock.expectOne('/api/auth/refresh');

    expect(request.request.withCredentials).toBe(true);

    const response = loginResponse(tokenWithExpiry(3600));
    request.flush(response);
    await initialization;

    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.token()).toBe(response.token);
    expect(localStorage.getItem(TOKEN_KEY)).toBe(response.token);
  });

  it('shares one refresh request between concurrent callers', async () => {
    const auth = TestBed.inject(AuthService);
    const first = firstValueFrom(auth.refreshOnce());
    const second = firstValueFrom(auth.refreshOnce());
    const request = httpMock.expectOne('/api/auth/refresh');

    request.flush(loginResponse(tokenWithExpiry(3600)));

    await Promise.all([first, second]);
    expect(auth.isAuthenticated()).toBe(true);
  });

  it('clears local state and revokes the cookie on logout', () => {
    localStorage.setItem(TOKEN_KEY, tokenWithExpiry(3600));
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    localStorage.setItem(SESSION_KEY, 'local');

    const auth = TestBed.inject(AuthService);
    auth.logout();

    const request = httpMock.expectOne('/api/auth/logout');
    expect(request.request.withCredentials).toBe(true);
    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(auth.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
    expect(localStorage.getItem(SESSION_KEY)).toBeNull();
  });

  it('refreshes after a 401 and retries the original request with the new token', async () => {
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpClient);
    const oldToken = tokenWithExpiry(3600);
    const newToken = tokenWithExpiry(3600);

    const login = firstValueFrom(auth.login({ username: 'admin', password: 'pw' }, true));
    httpMock.expectOne('/api/auth/login').flush(loginResponse(oldToken));
    await login;

    const dashboard = firstValueFrom(http.get<{ ok: boolean }>('/api/dashboard'));
    const firstAttempt = httpMock.expectOne('/api/dashboard');

    expect(firstAttempt.request.headers.get('Authorization')).toBe(`Bearer ${oldToken}`);
    firstAttempt.flush({}, { status: 401, statusText: 'Unauthorized' });

    const refresh = httpMock.expectOne('/api/auth/refresh');
    expect(refresh.request.withCredentials).toBe(true);
    refresh.flush(loginResponse(newToken));

    const retry = httpMock.expectOne('/api/dashboard');
    expect(retry.request.headers.get('Authorization')).toBe(`Bearer ${newToken}`);
    retry.flush({ ok: true });

    await expect(dashboard).resolves.toEqual({ ok: true });
  });

  function loginResponse(token: string): LoginResponse {
    return {
      token,
      expiresUtc: new Date(Date.now() + 3_600_000).toISOString(),
      user,
    };
  }
});

function tokenWithExpiry(expiresInSeconds: number): string {
  const header = btoa(JSON.stringify({ alg: 'none', typ: 'JWT' }));
  const payload = btoa(
    JSON.stringify({ exp: Math.floor(Date.now() / 1000) + expiresInSeconds }),
  );

  return `${header}.${payload}.signature`;
}
