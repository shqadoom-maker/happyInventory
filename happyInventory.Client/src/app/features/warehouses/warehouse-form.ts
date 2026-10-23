import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { City } from '../../core/models/city.model';
import { WarehouseRequest } from '../../core/models/warehouse.model';
import { CityService } from '../../core/services/city.service';
import { WarehouseService } from '../../core/services/warehouse.service';

@Component({
  selector: 'app-warehouse-form',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './warehouse-form.html',
})
export class WarehouseForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly warehouseService = inject(WarehouseService);
  private readonly cityService = inject(CityService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  /** Bound from the route with withComponentInputBinding(). */
  readonly id = input<string>();

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly cities = signal<City[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    code: ['', [Validators.required, Validators.maxLength(30)]],
    cityId: [0, [Validators.required, Validators.min(1)]],
    address: ['', [Validators.maxLength(250)]],
    capacity: [0, [Validators.required, Validators.min(0)]],
    isActive: [true],
  });

  ngOnInit(): void {
    this.cityService.list().subscribe({
      next: (cities) => this.cities.set(cities),
      error: (error) => this.error.set(errorMessage(error, this.translate)),
    });

    const id = this.id();

    if (id) {
      this.loading.set(true);

      this.warehouseService.get(Number(id)).subscribe({
        next: (warehouse) => {
          this.form.patchValue({
            name: warehouse.name,
            code: warehouse.code,
            cityId: warehouse.cityId,
            address: warehouse.address,
            capacity: warehouse.capacity,
            isActive: warehouse.isActive,
          });
        },
        error: (error) => this.error.set(errorMessage(error, this.translate)),
        complete: () => this.loading.set(false),
      });
    }
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: WarehouseRequest = this.form.getRawValue();
    const id = this.id();

    this.saving.set(true);
    this.error.set(null);

    const operation = id
      ? this.warehouseService.update(Number(id), request)
      : this.warehouseService.create(request);

    operation.subscribe({
      next: () => this.router.navigate(['/warehouses']),
      error: (error) => {
        this.error.set(errorMessage(error, this.translate));
        this.saving.set(false);
      },
    });
  }
}
