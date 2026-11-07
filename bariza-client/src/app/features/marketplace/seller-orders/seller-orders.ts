import { Component, OnInit, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { OrderApiService } from '../../../core/services/order-api.service';
import { SellerOrder, UpdateOrderStatusRequest } from '../../../core/models/order.model';

@Component({
  selector: 'app-seller-orders',
  imports: [DecimalPipe, TranslatePipe],
  templateUrl: './seller-orders.html',
  styleUrl: './seller-orders.scss',
})
export class SellerOrdersPage implements OnInit {
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
}
