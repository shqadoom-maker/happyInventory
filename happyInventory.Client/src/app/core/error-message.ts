import { TranslateService } from '@ngx-translate/core';

import { ApiError } from './interceptors/api-error.interceptor';

export function errorMessage(error: unknown, translate: TranslateService): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return translate.instant('errors.generic');
}
