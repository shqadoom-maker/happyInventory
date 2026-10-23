import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { Warehouse } from '../../core/models/warehouse.model';
import { WarehouseService } from '../../core/services/warehouse.service';

@Component({
  selector: 'app-warehouse-list',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './warehouse-list.html',
})
export class WarehouseList implements OnInit {
  private readonly warehouseService = inject(WarehouseService);
  private readonly translate = inject(TranslateService);

  protected readonly warehouses = signal<Warehouse[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly search = signal('');

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();

    if (!term) {
      return this.warehouses();
    }

    return this.warehouses().filter((warehouse) =>
      [warehouse.name, warehouse.code, warehouse.cityName, warehouse.countryName]
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

    this.warehouseService.list().subscribe({
      next: (warehouses) => this.warehouses.set(warehouses),
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

  protected remove(warehouse: Warehouse): void {
    const message = this.translate.instant('warehouses.confirmDelete', {
      name: warehouse.name,
    });

    if (!confirm(message)) {
      return;
    }

    this.error.set(null);

    this.warehouseService.delete(warehouse.id).subscribe({
      next: () => this.load(),
      error: (error) => this.error.set(errorMessage(error, this.translate)),
    });
  }
}
