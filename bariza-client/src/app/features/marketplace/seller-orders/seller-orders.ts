import { Component, OnInit, inject, AfterViewInit } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { gsap } from 'gsap';

import { OrderApiService } from '../../../core/services/order-api.service';
import { SellerOrder, UpdateOrderStatusRequest } from '../../../core/models/order.model';

@Component({
  selector: 'app-seller-orders',
  imports: [DecimalPipe, TranslatePipe],
  templateUrl: './seller-orders.html',
  styleUrl: './seller-orders.scss',
})
export class SellerOrdersPage implements OnInit, AfterViewInit {
  private readonly orderApi = inject(OrderApiService);
  orders: SellerOrder[] = [];
  loading = true;

  ngOnInit(): void {
    this.orderApi.getSellerOrders().subscribe({
      next: (orders) => {
        this.orders = orders;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  updateStatus(order: SellerOrder, status: SellerOrder['status']): void {
    const request: UpdateOrderStatusRequest = { status };
    this.orderApi.updateSellerOrderStatus(order.id, request).subscribe(() => {
      order.status = status;
    });
  }

  ngAfterViewInit(): void {
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches) {
      return;
    }

    gsap.from('.seller-order-card', {
      y: 16,
      opacity: 0,
      duration: 0.4,
      stagger: 0.06,
      ease: 'power2.out',
      clearProps: 'all',
    });
  }
}
