import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { City } from '../../core/models/city.model';
import { Country } from '../../core/models/country.model';
import { CityService } from '../../core/services/city.service';
import { CountryService } from '../../core/services/country.service';

@Component({
  selector: 'app-locations',
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './locations.html',
  styleUrl: './locations.scss',
})
export class Locations implements OnInit {
  private readonly countryService = inject(CountryService);
  private readonly cityService = inject(CityService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly translate = inject(TranslateService);

  protected readonly countries = signal<Country[]>([]);
  protected readonly cities = signal<City[]>([]);
  protected readonly error = signal<string | null>(null);
  protected readonly savingCountry = signal(false);
  protected readonly savingCity = signal(false);

  protected readonly countryForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    code: ['', [Validators.required, Validators.maxLength(3)]],
  });

  protected readonly cityForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    countryId: [0, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.countryService.list().subscribe({
      next: (countries) => this.countries.set(countries),
      error: (error) => this.error.set(errorMessage(error, this.translate)),
    });

    this.cityService.list().subscribe({
      next: (cities) => this.cities.set(cities),
      error: (error) => this.error.set(errorMessage(error, this.translate)),
    });
  }

  protected addCountry(): void {
    if (this.countryForm.invalid) {
      this.countryForm.markAllAsTouched();
      return;
    }

    this.savingCountry.set(true);
    this.error.set(null);

    this.countryService.create(this.countryForm.getRawValue()).subscribe({
      next: () => {
        this.countryForm.reset({ name: '', code: '' });
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error, this.translate)),
      complete: () => this.savingCountry.set(false),
    });
  }

  protected addCity(): void {
    if (this.cityForm.invalid) {
      this.cityForm.markAllAsTouched();
      return;
    }

    this.savingCity.set(true);
    this.error.set(null);

    this.cityService.create(this.cityForm.getRawValue()).subscribe({
      next: () => {
        this.cityForm.reset({ name: '', countryId: 0 });
        this.load();
      },
      error: (error) => this.error.set(errorMessage(error, this.translate)),
      complete: () => this.savingCity.set(false),
    });
  }

  protected removeCountry(country: Country): void {
    const message = this.translate.instant('locations.confirmDeleteCountry', {
      name: country.name,
    });

    if (!confirm(message)) {
      return;
    }

    this.error.set(null);

    this.countryService.delete(country.id).subscribe({
      next: () => this.load(),
      error: (error) => this.error.set(errorMessage(error, this.translate)),
    });
  }

  protected removeCity(city: City): void {
    const message = this.translate.instant('locations.confirmDeleteCity', {
      name: city.name,
    });

    if (!confirm(message)) {
      return;
    }

    this.error.set(null);

    this.cityService.delete(city.id).subscribe({
      next: () => this.load(),
      error: (error) => this.error.set(errorMessage(error, this.translate)),
    });
  }
}
