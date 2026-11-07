import { TestBed } from '@angular/core/testing';

import { AuthService } from '../services/auth.service';
import { rootRedirect } from './root-redirect.guard';

describe('rootRedirect', () => {
  function resolve(isAuthenticated: boolean): string {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => isAuthenticated },
        },
      ],
    });

    return TestBed.runInInjectionContext(() => rootRedirect());
  }

  it('sends guests to the public marketplace', () => {
    expect(resolve(false)).toBe('/shop');
  });

  it('keeps signed-in staff on the dashboard', () => {
    expect(resolve(true)).toBe('/dashboard');
  });
});
