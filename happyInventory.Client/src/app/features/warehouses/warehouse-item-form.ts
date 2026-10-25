import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { WarehouseItemRequest } from '../../core/models/warehouse-item.model';
import { WarehouseService } from '../../core/services/warehouse.service';
import { WarehouseItemService } from '../../core/services/warehouse-item.service';

@Component({
  selector: 'app-warehouse-item-form',
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './warehouse-item-form.html',
})
export class WarehouseItemForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly itemService = inject(WarehouseItemService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly warehouseId = input.required<string>();
  readonly id = input<string>();

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly warehouseName = signal('');

  protected readonly form = this.formBuilder.nonNullable.group({
    itemName: ['', [Validators.required, Validators.maxLength(150)]],
    skuCode: [''],
    qty: [1, [Validators.required, Validators.min(1)]],
    costPrice: [0, [Validators.required, Validators.min(0)]],
    msrpPrice: [null as number | null],
  });

  ngOnInit(): void {
    this.loadWarehouse();

    const itemId = this.id();
    if (itemId) {
      this.loadItem(Number(itemId));
    }
  }

  private loadWarehouse(): void {
    this.warehouseService.get(Number(this.warehouseId())).subscribe({
      next: (warehouse) => this.warehouseName.set(warehouse.name),
      error: () => this.warehouseName.set(''),
    });
  }

  private loadItem(id: number): void {
    this.loading.set(true);

    this.itemService.get(id).subscribe({
      next: (item) => {
        this.form.patchValue({
          itemName: item.itemName,
          skuCode: item.skuCode ?? '',
          qty: item.qty,
          costPrice: item.costPrice,
          msrpPrice: item.msrpPrice ?? null,
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

    const { itemName, skuCode, qty, costPrice, msrpPrice } = this.form.getRawValue();

    const request: WarehouseItemRequest = {
      itemName: itemName.trim(),
      skuCode: skuCode?.trim() ? skuCode.trim() : null,
      qty: Number(qty),
      costPrice: Number(costPrice),
      msrpPrice: msrpPrice == null || (msrpPrice as unknown) === '' ? null : Number(msrpPrice),
      warehouseId: Number(this.warehouseId()),
    };

    this.loading.set(true);
    this.error.set(null);

    const itemId = this.id();
    const request$ =
      this.isEdit() && itemId
        ? this.itemService.update(Number(itemId), request)
        : this.itemService.create(request);

    request$.subscribe({
      next: () => this.router.navigate(['/warehouses', this.warehouseId(), 'items']),
      error: (err) => {
        this.error.set(errorMessage(err, this.translate));
        this.loading.set(false);
      },
    });
  }
}
