import { inject } from '@angular/core';

import { AuthService } from '../services/auth.service';

/**
 * Conditional root redirect used by the top-level empty route.
 *
 * Guests enter the public marketplace, while signed-in staff keep the existing
 * dashboard landing page. Angular runs redirect functions in an injection
 * context, so the auth service can be resolved here.
 */
export function rootRedirect(): string {
  return inject(AuthService).isAuthenticated() ? '/dashboard' : '/shop';
}
