import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { OrderApiService } from '../../../core/services/order-api.service';
import { Order } from '../../../core/models/order.model';

@Component({
  selector: 'app-order-details',
  imports: [DecimalPipe, TranslatePipe],
  templateUrl: './order-details.html',
  styleUrl: './order-details.scss',
})
export class OrderDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly orderApi = inject(OrderApiService);

  order: Order | null = null;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id > 0) {
      this.orderApi.getOrder(id).subscribe((order) => (this.order = order));
    }
  }
}
