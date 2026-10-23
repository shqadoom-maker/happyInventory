import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

export type LanguageCode = 'en' | 'ar';
export type TextDirection = 'ltr' | 'rtl';

export interface LanguageOption {
  code: LanguageCode;
  label: string;
  dir: TextDirection;
}

const STORAGE_KEY = 'happy-inventory.language';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly document = inject(DOCUMENT);

  readonly languages: readonly LanguageOption[] = [
    { code: 'en', label: 'English', dir: 'ltr' },
    { code: 'ar', label: 'العربية', dir: 'rtl' },
  ];

  readonly current = signal<LanguageCode>('en');

  /** Runs before bootstrap so the first paint is already translated. */
  async initialize(): Promise<void> {
    const saved = this.readSavedLanguage();
    const preferred: LanguageCode =
      saved ?? (navigator.language?.toLowerCase().startsWith('ar') ? 'ar' : 'en');

    await this.setLanguage(preferred);
  }

  async setLanguage(code: LanguageCode): Promise<void> {
    this.current.set(code);
    this.applyDocumentLanguage(code);
    this.saveLanguage(code);

    try {
      await firstValueFrom(this.translate.use(code));
    } catch {
      // Keep the current UI rather than failing bootstrap if a file is missing.
    }
  }

  private applyDocumentLanguage(code: LanguageCode): void {
    const option = this.languages.find((language) => language.code === code) ?? this.languages[0];

    this.document.documentElement.lang = code;
    this.document.documentElement.dir = option.dir;
  }

  private readSavedLanguage(): LanguageCode | null {
    try {
      const value = localStorage.getItem(STORAGE_KEY);
      return value === 'ar' || value === 'en' ? value : null;
    } catch {
      return null;
    }
  }

  private saveLanguage(code: LanguageCode): void {
    try {
      localStorage.setItem(STORAGE_KEY, code);
    } catch {
      // Storage can be blocked; the session still switches language.
    }
  }
}
