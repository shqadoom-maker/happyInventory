import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { PagedResult } from '../../core/models/paged-result.model';
import { WarehouseItem } from '../../core/models/warehouse-item.model';
import { WarehouseService } from '../../core/services/warehouse.service';
import { WarehouseItemService } from '../../core/services/warehouse-item.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-warehouse-items-list',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './warehouse-items-list.html',
})
export class WarehouseItemsList implements OnInit {
  private readonly warehouseService = inject(WarehouseService);
  private readonly itemService = inject(WarehouseItemService);
  private readonly translate = inject(TranslateService);
  private readonly toast = inject(ToastService);

  readonly warehouseId = input.required<string>();

  protected readonly warehouseName = signal('');
  protected readonly result = signal<PagedResult<WarehouseItem> | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly search = signal('');

  protected readonly page = signal(1);
  protected readonly pageSize = 10;
  protected readonly totalPages = computed(() => this.result()?.totalPages ?? 0);
  protected readonly totalCount = computed(() => this.result()?.totalCount ?? 0);
  protected readonly items = computed(() => this.result()?.items ?? []);

  ngOnInit(): void {
    this.loadWarehouse();
    this.load();
  }

  private loadWarehouse(): void {
    this.warehouseService.get(Number(this.warehouseId())).subscribe({
      next: (warehouse) => this.warehouseName.set(warehouse.name),
      error: () => this.warehouseName.set(''),
    });
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.itemService
      .listByWarehouse(Number(this.warehouseId()), this.page(), this.pageSize, this.search())
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

  protected delete(item: WarehouseItem): void {
    const message = this.translate.instant('warehouseItems.confirmDelete', {
      name: item.itemName,
    });

    this.toast
      .confirm(message, this.translate.instant('common.confirmDeleteTitle'))
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.itemService.delete(item.id).subscribe({
          next: () => {
            this.toast.success(this.translate.instant('common.deletedSuccessfully'));
            if (this.items().length === 1 && this.page() > 1) {
              this.page.set(this.page() - 1);
            }
            this.load();
          },
          error: (err) => {
            this.error.set(errorMessage(err, this.translate));
          },
        });
      });
  }
}
