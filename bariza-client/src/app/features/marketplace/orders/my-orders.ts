import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { OrderApiService } from '../../../core/services/order-api.service';
import { Order } from '../../../core/models/order.model';

@Component({
  selector: 'app-my-orders',
  imports: [RouterLink, DatePipe, DecimalPipe, TranslatePipe],
  templateUrl: './my-orders.html',
  styleUrl: './my-orders.scss',
})
export class MyOrdersPage implements OnInit {
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
}
