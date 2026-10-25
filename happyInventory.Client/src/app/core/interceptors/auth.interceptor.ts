import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injector, inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

/** Adds the JWT to API calls and ends the session when the token is rejected. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  // Resolve Router lazily: creating it while the translate loader is still
  // bootstrapping would make Router and TranslateService depend on each other.
  const injector = inject(Injector);
  const auth = injector.get(AuthService);

  const token = auth.token();
  const isApiRequest = request.url.startsWith(environment.apiBaseUrl);
  const isLoginRequest = request.url.endsWith('/auth/login');

  const authenticatedRequest =
    token && isApiRequest
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isLoginRequest) {
        auth.logout();
        void injector.get(Router).navigate(['/login'], {
          queryParams: { returnUrl: routerUrl(injector) },
        });
      }

      return throwError(() => error);
    }),
  );
};

function routerUrl(injector: Injector): string {
  return injector.get(Router).url;
}
