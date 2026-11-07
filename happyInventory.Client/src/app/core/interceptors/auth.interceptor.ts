import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injector, inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

/**
 * Adds the JWT to API calls. A 401 on a normal API request first tries the
 * HttpOnly refresh cookie, retries the original request once, and only ends the
 * session if the refresh token is also rejected.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  // Resolve Router lazily: creating it while the translate loader is still
  // bootstrapping would make Router and TranslateService depend on each other.
  const injector = inject(Injector);
  const auth = injector.get(AuthService);

  const token = auth.token();
  const isApiRequest = request.url.startsWith(environment.apiBaseUrl);
  const isLoginRequest = request.url.endsWith('/auth/login');
  const isRefreshRequest = request.url.endsWith('/auth/refresh');
  const isLogoutRequest = request.url.endsWith('/auth/logout');
  const isAuthEndpoint = isLoginRequest || isRefreshRequest || isLogoutRequest;

  const authenticatedRequest =
    token && isApiRequest && !isAuthEndpoint
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      if (
        !isApiRequest ||
        isAuthEndpoint ||
        !(error instanceof HttpErrorResponse) ||
        error.status !== 401
      ) {
        return throwError(() => error);
      }

      if (!auth.canRefresh()) {
        endSession(injector, auth);
        return throwError(() => error);
      }

      return auth.refreshOnce().pipe(
        catchError((refreshError: unknown) => {
          // A transient refresh failure must not destroy a valid local
          // session. Only a rejected refresh token means the user is signed
          // out.
          if (
            refreshError instanceof HttpErrorResponse &&
            refreshError.status === 401
          ) {
            endSession(injector, auth);
          }

          return throwError(() => refreshError);
        }),
        switchMap((session) => {
          const retried = request.clone({
            setHeaders: { Authorization: `Bearer ${session.token}` },
          });

          return next(retried).pipe(
            catchError((retryError: unknown) => {
              // If the retried request is rejected as well, the new token is
              // unusable; do not loop through refresh again.
              if (
                retryError instanceof HttpErrorResponse &&
                retryError.status === 401
              ) {
                endSession(injector, auth);
              }

              return throwError(() => retryError);
            }),
          );
        }),
      );
    }),
  );
};

function endSession(injector: Injector, auth: AuthService): void {
  const router = injector.get(Router);
  const returnUrl = router.url;

  auth.logout();

  if (!returnUrl.startsWith('/login')) {
    void router.navigate(['/login'], { queryParams: { returnUrl } });
  }
}
