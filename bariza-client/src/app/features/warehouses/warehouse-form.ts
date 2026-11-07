import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { City } from '../../core/models/city.model';
import { Country } from '../../core/models/country.model';
import { WarehouseRequest } from '../../core/models/warehouse.model';
import { CityService } from '../../core/services/city.service';
import { CountryService } from '../../core/services/country.service';
import { WarehouseService } from '../../core/services/warehouse.service';

@Component({
  selector: 'app-warehouse-form',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './warehouse-form.html',
})
export class WarehouseForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly warehouseService = inject(WarehouseService);
  private readonly countryService = inject(CountryService);
  private readonly cityService = inject(CityService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly id = input<string>();

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly loading = signal(false);
  protected readonly countries = signal<Country[]>([]);
  protected readonly cities = signal<City[]>([]);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    address: ['', [Validators.required, Validators.maxLength(250)]],
    countryId: [null as number | null, [Validators.required]],
    cityId: [null as number | null, [Validators.required]],
  });

  ngOnInit(): void {
    this.loadCountries();

    const id = this.id();
    if (id) {
      this.loadWarehouse(Number(id));
    }
  }

  protected onCountryChange(): void {
    this.form.controls.cityId.setValue(null);
    const countryId = this.form.controls.countryId.value;

    if (!countryId) {
      this.cities.set([]);
      return;
    }

    this.cityService.list(countryId).subscribe({
      next: (cities) => this.cities.set(cities),
      error: (err) => {
        this.error.set(errorMessage(err, this.translate));
      },
    });
  }

  private loadCountries(): void {
    this.countryService.list().subscribe({
      next: (countries) => this.countries.set(countries),
      error: (err) => {
        this.error.set(errorMessage(err, this.translate));
      },
    });
  }

  private loadWarehouse(id: number): void {
    this.loading.set(true);

    this.warehouseService.get(id).subscribe({
      next: (warehouse) => {
        this.form.patchValue({
          name: warehouse.name,
          address: warehouse.address,
          countryId: warehouse.countryId,
          cityId: warehouse.cityId,
        });
        this.cityService.list(warehouse.countryId).subscribe({
          next: (cities) => this.cities.set(cities),
          error: (err) => {
            this.error.set(errorMessage(err, this.translate));
          },
        });
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(errorMessage(err, this.translate));
        this.loading.set(false);
      },
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, address, countryId, cityId } = this.form.getRawValue();

    if (countryId === null || cityId === null) {
      this.form.markAllAsTouched();
      return;
    }

    const request: WarehouseRequest = {
      name: name.trim(),
      address: address.trim(),
      countryId,
      cityId,
    };

    this.loading.set(true);
    this.error.set(null);

    const id = this.id();
    const request$ = this.isEdit() && id
      ? this.warehouseService.update(Number(id), request)
      : this.warehouseService.create(request);

    request$.subscribe({
      next: () => this.router.navigate(['/warehouses']),
      error: (err) => {
        this.error.set(errorMessage(err, this.translate));
        this.loading.set(false);
      },
    });
  }
}
