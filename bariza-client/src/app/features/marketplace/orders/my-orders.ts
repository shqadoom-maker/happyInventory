import { Component, OnInit, inject, AfterViewInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { gsap } from 'gsap';

import { OrderApiService } from '../../../core/services/order-api.service';
import { Order } from '../../../core/models/order.model';

@Component({
  selector: 'app-my-orders',
  imports: [RouterLink, DatePipe, DecimalPipe, TranslatePipe],
  templateUrl: './my-orders.html',
  styleUrl: './my-orders.scss',
})
export class MyOrdersPage implements OnInit, AfterViewInit {
  private readonly orderApi = inject(OrderApiService);
  orders: Order[] = [];
  loading = true;

  ngOnInit(): void {
    this.orderApi.getMyOrders().subscribe({
      next: (orders) => {
        this.orders = orders;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  ngAfterViewInit(): void {
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches) {
      return;
    }

    gsap.from('.order-card', {
      y: 16,
      opacity: 0,
      duration: 0.4,
      stagger: 0.06,
      ease: 'power2.out',
      clearProps: 'all',
    });
  }
}
