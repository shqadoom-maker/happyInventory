import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { OrderApiService } from '../../../core/services/order-api.service';
import { Order } from '../../../core/models/order.model';

@Component({
  selector: 'app-order-confirmation',
  imports: [RouterLink, DecimalPipe, TranslatePipe],
  templateUrl: './order-confirmation.html',
  styleUrl: './order-confirmation.scss',
})
export class OrderConfirmationPage implements OnInit {
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
