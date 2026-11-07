import { Component, HostListener, OnDestroy, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { AuthService } from '../core/services/auth.service';
import { LanguageCode, LanguageService } from '../core/services/language.service';
import { Logo } from '../shared/logo/logo';

interface NavItem {
  path: string;
  labelKey: string;
  icon: string;
  exact: boolean;
}

const BASE_NAV_ITEMS: NavItem[] = [
  { path: '/dashboard', labelKey: 'nav.dashboard', icon: 'dashboard', exact: true },
  { path: '/warehouses', labelKey: 'nav.warehouses', icon: 'warehouse', exact: false },
  {
    path: '/stock-movements',
    labelKey: 'nav.stockMovements',
    icon: 'integration_instructions',
    exact: false,
  },
  { path: '/locations', labelKey: 'nav.locations', icon: 'location_on', exact: false },
  { path: '/shop', labelKey: 'nav.marketplace', icon: 'storefront', exact: false },
];

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe, Logo],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell implements OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly languageService = inject(LanguageService);

  protected readonly languages = this.languageService.languages;
  protected readonly currentLanguage = this.languageService.current;
  protected readonly user = this.auth.user;
  protected readonly menuOpen = signal(false);

  protected readonly navItems = computed<NavItem[]>(() => {
    const items = [...BASE_NAV_ITEMS];

    if (this.auth.isAdmin()) {
      items.push({ path: '/users', labelKey: 'nav.users', icon: 'group', exact: false });
      items.push({
        path: '/system-logs',
        labelKey: 'nav.systemLogs',
        icon: 'receipt_long',
        exact: false,
      });
    }

    return items;
  });

  protected readonly initials = computed(() => {
    const user = this.auth.user();
    const source = user?.fullName?.trim() || user?.username || '?';

    return source
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part.charAt(0).toUpperCase())
      .join('');
  });

  protected readonly roleKey = computed(
    () => `roles.${(this.auth.user()?.role ?? 'Auditor').toLowerCase()}`,
  );

  ngOnDestroy(): void {
    document.body.classList.remove('menu-open');
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.menuOpen()) {
      this.closeMenu();
    }
  }

  protected toggleMenu(): void {
    this.setMenuOpen(!this.menuOpen());
  }

  protected closeMenu(): void {
    this.setMenuOpen(false);
  }

  protected changeLanguage(code: LanguageCode): void {
    void this.languageService.setLanguage(code);
  }

  protected logout(): void {
    this.closeMenu();
    this.auth.logout();
    void this.router.navigate(['/login']);
  }

  private setMenuOpen(open: boolean): void {
    this.menuOpen.set(open);
    document.body.classList.toggle('menu-open', open);
  }
}
