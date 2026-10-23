import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { StockMovement } from '../../core/models/stock-movement.model';
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
  protected readonly movements = signal<StockMovement[]>([]);
  protected readonly selectedWarehouseId = signal<number | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.warehouseService.list().subscribe({
      next: (warehouses) => this.warehouses.set(warehouses),
      error: (error) => this.error.set(errorMessage(error, this.translate)),
    });

    this.load();
  }

  protected onWarehouseChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.selectedWarehouseId.set(value || null);
    this.load();
  }

  protected load(): void {
    const warehouseId = this.selectedWarehouseId() ?? undefined;

    this.loading.set(true);
    this.error.set(null);

    this.stockMovementService.list(warehouseId).subscribe({
      next: (movements) => this.movements.set(movements),
      error: (error) => {
        this.error.set(errorMessage(error, this.translate));
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  protected movementClass(type: StockMovement['type']): string {
    switch (type) {
      case 'Inbound':
        return 'movement-in';
      case 'Outbound':
        return 'movement-out';
      default:
        return 'movement-adjust';
    }
  }

  protected movementTypeKey(type: StockMovement['type']): string {
    return `movementTypes.${type.toLowerCase()}`;
  }

  protected signedQuantity(movement: StockMovement): string {
    return movement.quantityChange > 0
      ? `+${movement.quantityChange}`
      : `${movement.quantityChange}`;
  }
}
