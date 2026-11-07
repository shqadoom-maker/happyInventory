import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';

import { AuthService } from '../../core/services/auth.service';
import { LanguageService } from '../../core/services/language.service';
import { MarketplaceLayout } from './marketplace-layout';

describe('MarketplaceLayout', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MarketplaceLayout],
      providers: [
        provideRouter([]),
        provideTranslateService(),
        {
          provide: AuthService,
          useValue: { isAuthenticated: () => false },
        },
        {
          provide: LanguageService,
          useValue: {
            languages: [
              { code: 'en', label: 'English', dir: 'ltr' },
              { code: 'ar', label: 'العربية', dir: 'rtl' },
            ],
            current: () => 'en',
            setLanguage: () => Promise.resolve(),
          },
        },
      ],
    }).compileComponents();
  });

  it('renders the public header, footer and router outlet', () => {
    const fixture = TestBed.createComponent(MarketplaceLayout);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.site-header')).toBeTruthy();
    expect(element.querySelector('.site-footer')).toBeTruthy();
    expect(element.querySelector('router-outlet')).toBeTruthy();
  });

  it('shows the staff sign-in action to guests', () => {
    const fixture = TestBed.createComponent(MarketplaceLayout);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.header-cta')).toBeTruthy();
    expect(element.querySelector('.drawer-cta')).toBeTruthy();
  });

  it('renders the catalog search, registration link and cart action', () => {
    const fixture = TestBed.createComponent(MarketplaceLayout);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.site-search input')).toBeTruthy();
    expect(element.querySelector('.header-register')).toBeTruthy();
    expect(element.querySelector('.cart-button')).toBeTruthy();
    expect(element.querySelector('.cart-button')?.textContent).toContain('0');
  });
});
