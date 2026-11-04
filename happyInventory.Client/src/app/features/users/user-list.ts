import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { ResetPasswordRequest, User } from '../../core/models/user.model';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { ToastService } from '../../core/services/toast.service';

function passwordsMatch(control: AbstractControl): ValidationErrors | null {
  const newPassword = control.get('newPassword')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;

  return newPassword === confirmPassword ? null : { mismatch: true };
}

@Component({
  selector: 'app-user-list',
  imports: [RouterLink, DatePipe, ReactiveFormsModule, TranslatePipe],
  templateUrl: './user-list.html',
  styleUrl: './user-list.scss',
})
export class UserList implements OnInit {
  private readonly userService = inject(UserService);
  private readonly auth = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);

  protected readonly users = signal<User[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly search = signal('');
  protected readonly currentUserId = computed(() => this.auth.user()?.id);

  protected readonly passwordUser = signal<User | null>(null);
  protected readonly passwordSaving = signal(false);
  protected readonly passwordError = signal<string | null>(null);

  protected readonly passwordForm = this.formBuilder.nonNullable.group(
    {
      newPassword: [
        '',
        [Validators.required, Validators.minLength(8), Validators.maxLength(200)],
      ],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatch },
  );

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();

    if (!term) {
      return this.users();
    }

    return this.users().filter((user) =>
      [user.username, user.fullName, user.role]
        .filter((value): value is string => !!value)
        .some((value) => value.toLowerCase().includes(term)),
    );
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.notice.set(null);

    this.userService.list().subscribe({
      next: (users) => this.users.set(users),
      error: (error) => {
        this.error.set(errorMessage(error, this.translate));
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  protected updateSearch(value: string): void {
    this.search.set(value);
  }

  protected roleKey(role: string): string {
    return `roles.${role.toLowerCase()}`;
  }

  protected canDelete(user: User): boolean {
    return user.id !== this.currentUserId() && user.role !== 'Admin';
  }

  protected openChangePassword(user: User): void {
    this.passwordForm.reset({ newPassword: '', confirmPassword: '' });
    this.passwordError.set(null);
    this.notice.set(null);
    this.passwordUser.set(user);
  }

  protected cancelChangePassword(): void {
    if (this.passwordSaving()) {
      return;
    }

    this.passwordUser.set(null);
    this.passwordError.set(null);
    this.passwordForm.reset({ newPassword: '', confirmPassword: '' });
  }

  protected submitChangePassword(): void {
    const user = this.passwordUser();

    if (!user) {
      return;
    }

    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const request: ResetPasswordRequest = {
      newPassword: this.passwordForm.controls.newPassword.value,
    };

    this.passwordSaving.set(true);
    this.passwordError.set(null);

    this.userService.resetPassword(user.id, request).subscribe({
      next: () => {
        this.passwordSaving.set(false);
        this.passwordUser.set(null);
        this.passwordForm.reset({ newPassword: '', confirmPassword: '' });
        this.notice.set(
          this.translate.instant('users.changePassword.success', { name: user.fullName }),
        );
      },
      error: (error) => {
        this.passwordError.set(errorMessage(error, this.translate));
        this.passwordSaving.set(false);
      },
    });
  }

  protected remove(user: User): void {
    if (!this.canDelete(user)) {
      return;
    }

    const message = this.translate.instant('users.confirmDelete', {
      name: user.fullName,
    });

    this.toast
      .confirm(message, this.translate.instant('common.confirmDeleteTitle'))
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.error.set(null);

        this.userService.delete(user.id).subscribe({
          next: () => {
            this.toast.success(this.translate.instant('common.deletedSuccessfully'));
            this.load();
          },
          error: (error) => this.error.set(errorMessage(error, this.translate)),
        });
      });
  }
}
