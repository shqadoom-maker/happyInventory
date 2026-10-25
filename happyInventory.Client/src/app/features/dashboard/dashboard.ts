import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { Dashboard as DashboardData, DashboardItem } from '../../core/models/dashboard.model';
import { AuthService } from '../../core/services/auth.service';
import { DashboardService } from '../../core/services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  imports: [DecimalPipe, RouterLink, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly translate = inject(TranslateService);
  private readonly auth = inject(AuthService);

  protected readonly user = this.auth.user;
  protected readonly displayName = computed(
    () => this.user()?.fullName || this.user()?.username || '',
  );

  protected readonly data = signal<DashboardData | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly warehouses = computed(() => this.data()?.warehouses ?? []);
  protected readonly topItems = computed(() => this.data()?.topItems ?? []);
  protected readonly lowItems = computed(() => this.data()?.lowItems ?? []);

  protected readonly maxWarehouseItems = computed(() =>
    Math.max(1, ...this.warehouses().map((warehouse) => warehouse.itemCount)),
  );
  protected readonly maxHighQuantity = computed(() =>
    Math.max(1, ...this.topItems().map((item) => item.totalQuantity)),
  );
  protected readonly maxLowQuantity = computed(() =>
    Math.max(1, ...this.lowItems().map((item) => item.totalQuantity)),
  );

  protected readonly cards = [
    {
      path: '/warehouses',
      icon: 'warehouse',
      titleKey: 'nav.warehouses',
      descriptionKey: 'dashboard.cards.warehouses',
    },
    {
      path: '/stock-movements',
      icon: 'swap_vert',
      titleKey: 'nav.stockMovements',
      descriptionKey: 'dashboard.cards.stockMovements',
    },
    {
      path: '/locations',
      icon: 'location_on',
      titleKey: 'nav.locations',
      descriptionKey: 'dashboard.cards.locations',
    },
  ];

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.dashboardService.get().subscribe({
      next: (data) => {
        this.data.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(errorMessage(err, this.translate));
        this.loading.set(false);
      },
    });
  }

  protected warehouseHeight(itemCount: number): number {
    if (itemCount <= 0) {
      return 0;
    }

    return Math.max(6, Math.round((itemCount / this.maxWarehouseItems()) * 100));
  }

  protected barWidth(quantity: number, max: number): number {
    if (quantity <= 0) {
      return 0;
    }

    return Math.max(6, Math.round((quantity / max) * 100));
  }

  protected itemDetails(item: DashboardItem): string {
    const key =
      item.warehouseCount === 1
        ? 'dashboard.analytics.warehouseCountOne'
        : 'dashboard.analytics.warehouseCount';
    const warehouses = this.translate.instant(key, { count: item.warehouseCount });

    return item.skuCode ? `${item.skuCode} · ${warehouses}` : warehouses;
  }
}
