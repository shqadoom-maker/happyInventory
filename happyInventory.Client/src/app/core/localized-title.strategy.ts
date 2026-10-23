import { Injectable, inject } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

/** Route titles are translation keys; the browser tab follows the active language. */
@Injectable({ providedIn: 'root' })
export class LocalizedTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly translate = inject(TranslateService);

  private currentKey: string | null = null;

  constructor() {
    super();
    this.translate.onLangChange.subscribe(() => this.applyTitle());
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this.currentKey = this.buildTitle(snapshot) ?? null;
    this.applyTitle();
  }

  private applyTitle(): void {
    if (!this.currentKey) {
      return;
    }

    const page = this.translate.instant(this.currentKey);
    const brand = this.translate.instant('brand.name');
    this.title.setTitle(`${page} | ${brand}`);
  }
}
