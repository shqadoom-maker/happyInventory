import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { ApiError } from '../../core/interceptors/api-error.interceptor';
import { AuthService } from '../../core/services/auth.service';
import { LanguageCode, LanguageService } from '../../core/services/language.service';
import { BrandMark } from '../../shared/brand-mark/brand-mark';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, TranslatePipe, BrandMark],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly translate = inject(TranslateService);
  private readonly languageService = inject(LanguageService);

  protected readonly languages = this.languageService.languages;
  protected readonly currentLanguage = this.languageService.current;
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly showPassword = signal(false);

  protected readonly form = this.formBuilder.nonNullable.group({
    username: ['', [Validators.required, Validators.maxLength(50)]],
    password: ['', [Validators.required]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: () => {
        void this.router.navigateByUrl(this.returnUrl());
      },
      error: (error: unknown) => {
        this.loading.set(false);

        if (error instanceof ApiError && error.status === 401) {
          const details = error.details as { code?: string } | undefined;

          this.error.set(
            this.translate.instant(
              details?.code === 'account_disabled'
                ? 'auth.accountDisabled'
                : 'auth.invalidCredentials',
            ),
          );
          return;
        }

        this.error.set(errorMessage(error, this.translate));
      },
    });
  }

  protected changeLanguage(code: LanguageCode): void {
    void this.languageService.setLanguage(code);
  }

  private returnUrl(): string {
    const requested = this.route.snapshot.queryParamMap.get('returnUrl');

    return requested && requested.startsWith('/') && !requested.startsWith('//')
      ? requested
      : '/dashboard';
  }
}
