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

function apiMessage(body: unknown): string | null {
  if (typeof body === 'string') {
    return body.trim() || null;
  }

  if (!body || typeof body !== 'object') {
    return null;
  }

  const problem = body as Record<string, unknown>;

  if (typeof problem['detail'] === 'string' && problem['detail'].trim()) {
    return problem['detail'];
  }

  // ASP.NET Core model validation returns { errors: { Field: ["message"] } }.
  const errors = problem['errors'];

  if (errors && typeof errors === 'object') {
    for (const value of Object.values(errors as Record<string, unknown>)) {
      if (Array.isArray(value)) {
        const first = value.find(
          (entry): entry is string => typeof entry === 'string' && entry.trim().length > 0,
        );

        if (first) {
          return first;
        }
      } else if (typeof value === 'string' && value.trim()) {
        return value;
      }
    }
  }

  if (typeof problem['message'] === 'string' && problem['message'].trim()) {
    return problem['message'];
  }

  if (typeof problem['title'] === 'string' && problem['title'].trim()) {
    return problem['title'];
  }

  return null;
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
      const serverMessage = apiMessage(error.error);
      let message: string;

      if (error.status === 0) {
        message = translate.instant('errors.unreachable');
      } else if (serverMessage) {
        // Preserve API details for every status, including 403 responses from
        // the password-change middleware and validation failures.
        message = serverMessage;
      } else if (error.status === 401 || error.status === 403) {
        message = translate.instant('errors.forbidden');
      } else if (error.status === 404) {
        message = translate.instant('errors.notFound');
      } else {
        message = translate.instant('errors.genericStatus', { status: error.status });
      }

      return throwError(() => new ApiError(message, error.status, error.error));
    }),
  );
};
