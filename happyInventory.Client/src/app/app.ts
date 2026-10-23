import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

import { LanguageCode, LanguageService } from './core/services/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private readonly languageService = inject(LanguageService);

  protected readonly languages = this.languageService.languages;
  protected readonly currentLanguage = this.languageService.current;

  protected readonly navItems = [
    { path: '/dashboard', labelKey: 'nav.dashboard', icon: '▦' },
    { path: '/warehouses', labelKey: 'nav.warehouses', icon: '⌂' },
    { path: '/products', labelKey: 'nav.products', icon: '◫' },
    { path: '/inventory', labelKey: 'nav.inventory', icon: '▤' },
    { path: '/stock-movements', labelKey: 'nav.stockMovements', icon: '⇅' },
    { path: '/locations', labelKey: 'nav.locations', icon: '⌖' },
  ];

  protected changeLanguage(code: LanguageCode): void {
    void this.languageService.setLanguage(code);
  }
}
