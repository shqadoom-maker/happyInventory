import { Component, HostListener, OnDestroy, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { AuthService } from '../../core/services/auth.service';
import { LanguageCode, LanguageService } from '../../core/services/language.service';
import { ThemeService } from '../../core/services/theme.service';
import { Logo } from '../../shared/logo/logo';

@Component({
  selector: 'app-marketplace-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe, Logo],
  templateUrl: './marketplace-layout.html',
  styleUrl: './marketplace-layout.scss',
})
export class MarketplaceLayout implements OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly languageService = inject(LanguageService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly themeService = inject(ThemeService);

  protected readonly languages = this.languageService.languages;
  protected readonly currentLanguage = this.languageService.current;
  protected readonly isAuthenticated = this.auth.isAuthenticated;
  protected readonly menuOpen = signal(false);
  protected readonly searchTerm = signal('');
  protected readonly theme = this.themeService.theme;
  protected readonly currentYear = new Date().getFullYear();

  constructor() {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed())
      .subscribe((params) => this.searchTerm.set(params.get('search') ?? ''));
  }

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

  protected toggleTheme(): void {
    this.themeService.toggle();
  }

  protected onSearchInput(value: string): void {
    this.searchTerm.set(value);
  }

  protected submitSearch(event: Event): void {
    event.preventDefault();
    const term = this.searchTerm().trim();

    void this.router.navigate(['/shop/products'], {
      queryParams: term ? { search: term } : {},
    });
    this.closeMenu();
  }

  private setMenuOpen(open: boolean): void {
    this.menuOpen.set(open);
    document.body.classList.toggle('menu-open', open);
  }
}
