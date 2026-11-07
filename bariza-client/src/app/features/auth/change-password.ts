import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { AuthService } from '../../core/services/auth.service';
import { LanguageCode, LanguageService } from '../../core/services/language.service';
import { ToastService } from '../../core/services/toast.service';
import { BrandMark } from '../../shared/brand-mark/brand-mark';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const newPassword = control.get('newPassword')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;

  return !newPassword || !confirmPassword || newPassword === confirmPassword
    ? null
    : { mismatch: true };
}

function newPasswordDiffers(control: AbstractControl): ValidationErrors | null {
  const currentPassword = control.get('currentPassword')?.value;
  const newPassword = control.get('newPassword')?.value;

  return currentPassword && newPassword && currentPassword === newPassword
    ? { samePassword: true }
    : null;
}

@Component({
  selector: 'app-change-password',
  imports: [ReactiveFormsModule, TranslatePipe, BrandMark],
  templateUrl: './change-password.html',
  styleUrl: './change-password.scss',
})
export class ChangePassword {
  private readonly formBuilder = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly languageService = inject(LanguageService);
  private readonly toast = inject(ToastService);

  protected readonly languages = this.languageService.languages;
  protected readonly currentLanguage = this.languageService.current;
  protected readonly user = this.auth.user;
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly submitted = signal(false);
  protected readonly currentYear = new Date().getFullYear();

  protected readonly form = this.formBuilder.nonNullable.group(
    {
      currentPassword: ['', [Validators.required, Validators.maxLength(200)]],
      newPassword: [
        '',
        [Validators.required, Validators.minLength(8), Validators.maxLength(200)],
      ],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: [passwordsMatch, newPasswordDiffers] },
  );

  protected submit(): void {
    this.submitted.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    const { currentPassword, newPassword } = this.form.getRawValue();

    this.auth.changePassword({ currentPassword, newPassword }).subscribe({
      next: () => {
        this.loading.set(false);
        this.toast.success(this.translate.instant('changePassword.success'));
        void this.router.navigateByUrl('/dashboard');
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.error.set(errorMessage(error, this.translate));
      },
    });
  }

  protected showError(
    field: 'currentPassword' | 'newPassword' | 'confirmPassword',
  ): boolean {
    const control = this.form.controls[field];

    return control.invalid && (this.submitted() || (control.dirty && control.touched));
  }

  protected changeLanguage(code: LanguageCode): void {
    void this.languageService.setLanguage(code);
  }

  protected logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
