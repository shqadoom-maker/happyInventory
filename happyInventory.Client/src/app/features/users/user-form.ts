import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { CreateUserRequest, UpdateUserRequest, UserRole } from '../../core/models/user.model';
import { UserService } from '../../core/services/user.service';

@Component({
  selector: 'app-user-form',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './user-form.html',
})
export class UserForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly id = input<string>();

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly roles: readonly UserRole[] = ['Admin', 'Management', 'Auditor'];

  protected readonly form = this.formBuilder.nonNullable.group({
    username: ['', [Validators.required, Validators.maxLength(50)]],
    fullName: ['', [Validators.required, Validators.maxLength(120)]],
    role: ['Auditor' as UserRole, [Validators.required]],
    password: ['', [Validators.minLength(8)]],
    isActive: [true],
  });

  ngOnInit(): void {
    const id = this.id();

    if (!id) {
      this.form.controls.password.addValidators(Validators.required);
      this.form.controls.password.updateValueAndValidity();
      return;
    }

    this.loading.set(true);

    this.userService.get(Number(id)).subscribe({
      next: (user) => {
        this.form.patchValue({
          username: user.username,
          fullName: user.fullName,
          role: user.role,
          password: '',
          isActive: user.isActive,
        });
      },
      error: (error) => this.error.set(errorMessage(error, this.translate)),
      complete: () => this.loading.set(false),
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const id = this.id();

    this.saving.set(true);
    this.error.set(null);

    if (id) {
      const request: UpdateUserRequest = {
        username: raw.username,
        fullName: raw.fullName,
        role: raw.role,
        password: raw.password.length > 0 ? raw.password : null,
        isActive: raw.isActive,
      };

      this.userService.update(Number(id), request).subscribe({
        next: () => void this.router.navigate(['/users']),
        error: (error) => {
          this.error.set(errorMessage(error, this.translate));
          this.saving.set(false);
        },
      });

      return;
    }

    const request: CreateUserRequest = {
      username: raw.username,
      fullName: raw.fullName,
      role: raw.role,
      password: raw.password,
      isActive: raw.isActive,
    };

    this.userService.create(request).subscribe({
      next: () => void this.router.navigate(['/users']),
      error: (error) => {
        this.error.set(errorMessage(error, this.translate));
        this.saving.set(false);
      },
    });
  }

  protected roleKey(role: UserRole): string {
    return `roles.${role.toLowerCase()}`;
  }
}
