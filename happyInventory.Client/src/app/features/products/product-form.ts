import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { ProductRequest } from '../../core/models/product.model';
import { ProductService } from '../../core/services/product.service';

@Component({
  selector: 'app-product-form',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './product-form.html',
})
export class ProductForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly id = input<string>();

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    sku: ['', [Validators.required, Validators.maxLength(40)]],
    name: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', [Validators.maxLength(500)]],
    unitPrice: [0, [Validators.required, Validators.min(0)]],
    isActive: [true],
  });

  ngOnInit(): void {
    const id = this.id();

    if (!id) {
      return;
    }

    this.loading.set(true);

    this.productService.get(Number(id)).subscribe({
      next: (product) => {
        this.form.patchValue({
          sku: product.sku,
          name: product.name,
          description: product.description ?? '',
          unitPrice: product.unitPrice,
          isActive: product.isActive,
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

    const request: ProductRequest = this.form.getRawValue();
    const id = this.id();

    this.saving.set(true);
    this.error.set(null);

    const operation = id
      ? this.productService.update(Number(id), request)
      : this.productService.create(request);

    operation.subscribe({
      next: () => this.router.navigate(['/products']),
      error: (error) => {
        this.error.set(errorMessage(error, this.translate));
        this.saving.set(false);
      },
    });
  }
}
