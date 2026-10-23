import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injector, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { catchError, throwError } from 'rxjs';

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly details?: unknown,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

/**
 * Turns transport errors into a single ApiError with a translated, readable
 * message so components do not need to branch on HttpErrorResponse.
 */
export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  // Resolved lazily: the translate HTTP loader itself uses HttpClient, so
  // injecting TranslateService here would create a dependency cycle.
  const injector = inject(Injector);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      const translate = injector.get(TranslateService);
      let message: string;

      if (error.status === 0) {
        message = translate.instant('errors.unreachable');
      } else if (error.status === 401 || error.status === 403) {
        message = translate.instant('errors.forbidden');
      } else if (error.status === 404) {
        message = translate.instant('errors.notFound');
      } else if (typeof error.error?.detail === 'string') {
        message = error.error.detail;
      } else if (typeof error.error?.title === 'string') {
        message = error.error.title;
      } else if (typeof error.error?.message === 'string') {
        message = error.error.message;
      } else if (typeof error.error === 'string' && error.error.trim().length > 0) {
        message = error.error;
      } else {
        message = translate.instant('errors.genericStatus', { status: error.status });
      }

      return throwError(() => new ApiError(message, error.status, error.error));
    }),
  );
};
