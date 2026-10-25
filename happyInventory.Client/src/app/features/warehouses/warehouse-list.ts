import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { PagedResult } from '../../core/models/paged-result.model';
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

  protected readonly result = signal<PagedResult<Warehouse> | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly search = signal('');

  protected readonly page = signal(1);
  protected readonly pageSize = 10;
  protected readonly totalPages = computed(() => this.result()?.totalPages ?? 0);
  protected readonly totalCount = computed(() => this.result()?.totalCount ?? 0);
  protected readonly warehouses = computed(() => this.result()?.items ?? []);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.warehouseService
      .list(this.page(), this.pageSize, this.search())
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(errorMessage(err, this.translate));
          this.loading.set(false);
        },
      });
  }

  protected updateSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
    this.load();
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }

    this.page.set(page);
    this.load();
  }

  protected prevPage(): void {
    this.goToPage(this.page() - 1);
  }

  protected nextPage(): void {
    this.goToPage(this.page() + 1);
  }

  protected delete(warehouse: Warehouse): void {
    if (!confirm(this.translate.instant('warehouses.confirmDelete', { name: warehouse.name }))) {
      return;
    }

    this.warehouseService.delete(warehouse.id).subscribe({
      next: () => {
        if (this.warehouses().length === 1 && this.page() > 1) {
          this.page.set(this.page() - 1);
        }
        this.load();
      },
      error: (err) => {
        this.error.set(errorMessage(err, this.translate));
      },
    });
  }
}
