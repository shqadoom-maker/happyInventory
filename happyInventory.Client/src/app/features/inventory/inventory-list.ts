import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import {
  InventoryAdjustmentRequest,
  InventoryItem,
  StockMovementType,
} from '../../core/models/inventory.model';
import { Warehouse } from '../../core/models/warehouse.model';
import { InventoryService } from '../../core/services/inventory.service';
import { WarehouseService } from '../../core/services/warehouse.service';

@Component({
  selector: 'app-inventory-list',
  imports: [ReactiveFormsModule, DatePipe, TranslatePipe],
  templateUrl: './inventory-list.html',
  styleUrl: './inventory-list.scss',
})
export class InventoryList implements OnInit {
  private readonly inventoryService = inject(InventoryService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly translate = inject(TranslateService);

  protected readonly warehouses = signal<Warehouse[]>([]);
  protected readonly selectedWarehouseId = signal<number | null>(null);
  protected readonly items = signal<InventoryItem[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly adjusting = signal<InventoryItem | null>(null);
  protected readonly saving = signal(false);

  protected readonly adjustmentForm = this.formBuilder.nonNullable.group({
    quantityChange: [0, Validators.required],
    type: ['Inbound' as StockMovementType, Validators.required],
    reference: [''],
  });

  ngOnInit(): void {
    this.warehouseService.list().subscribe({
      next: (warehouses) => {
        this.warehouses.set(warehouses);

        if (warehouses.length > 0) {
          this.selectedWarehouseId.set(warehouses[0].id);
          this.load();
        }
      },
      error: (error) => this.error.set(errorMessage(error, this.translate)),
    });
  }

  protected onWarehouseChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.selectedWarehouseId.set(value || null);
    this.adjusting.set(null);
    this.load();
  }

  protected load(): void {
    const warehouseId = this.selectedWarehouseId();

    if (!warehouseId) {
      this.items.set([]);
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.inventoryService.listByWarehouse(warehouseId).subscribe({
      next: (items) => this.items.set(items),
      error: (error) => {
        this.error.set(errorMessage(error, this.translate));
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  protected startAdjustment(item: InventoryItem): void {
    this.adjusting.set(item);
    this.adjustmentForm.reset({
      quantityChange: 0,
      type: 'Inbound',
      reference: '',
    });
  }

  protected cancelAdjustment(): void {
    this.adjusting.set(null);
  }

  protected submitAdjustment(): void {
    const item = this.adjusting();
    const warehouseId = this.selectedWarehouseId();

    if (!item || !warehouseId) {
      return;
    }

    if (this.adjustmentForm.invalid) {
      this.adjustmentForm.markAllAsTouched();
      return;
    }

    const request: InventoryAdjustmentRequest = this.adjustmentForm.getRawValue();

    if (request.quantityChange === 0) {
      this.error.set(this.translate.instant('inventory.zeroError'));
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    this.inventoryService.adjust(warehouseId, item.productId, request).subscribe({
      next: () => {
        this.adjusting.set(null);
        this.load();
      },
      error: (error) => {
        this.error.set(errorMessage(error, this.translate));
        this.saving.set(false);
      },
      complete: () => this.saving.set(false),
    });
  }

  protected isLowStock(item: InventoryItem): boolean {
    return item.quantity <= item.reorderLevel;
  }
}
