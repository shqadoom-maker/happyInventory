import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { InventoryUpdateRecord } from '../../core/models/inventory-update-record.model';
import { PagedResult } from '../../core/models/paged-result.model';
import { Warehouse } from '../../core/models/warehouse.model';
import { StockMovementService } from '../../core/services/stock-movement.service';
import { WarehouseService } from '../../core/services/warehouse.service';

@Component({
  selector: 'app-stock-movement-list',
  imports: [DatePipe, TranslatePipe],
  templateUrl: './stock-movement-list.html',
})
export class StockMovementList implements OnInit {
  private readonly stockMovementService = inject(StockMovementService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly translate = inject(TranslateService);

  protected readonly warehouses = signal<Warehouse[]>([]);
  protected readonly records = signal<InventoryUpdateRecord[]>([]);
  protected readonly selectedWarehouseId = signal<number | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  ngOnInit(): void {
    this.warehouseService.listAll().subscribe({
      next: (warehouses) => this.warehouses.set(warehouses),
      error: (err) => this.error.set(errorMessage(err, this.translate)),
    });

    this.load();
  }

  protected onWarehouseChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.selectedWarehouseId.set(value || null);
    this.page.set(1);
    this.load();
  }

  protected onPageChange(newPage: number): void {
    this.page.set(newPage);
    this.load();
  }

  protected load(): void {
    const warehouseId = this.selectedWarehouseId() ?? undefined;

    this.loading.set(true);
    this.error.set(null);

    this.stockMovementService
      .list(warehouseId, this.page(), this.pageSize())
      .subscribe({
        next: (result: PagedResult<InventoryUpdateRecord>) => {
          this.records.set(result.items);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
        },
        error: (err) => {
          this.error.set(errorMessage(err, this.translate));
          this.loading.set(false);
        },
        complete: () => this.loading.set(false),
      });
  }
}
