import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { ShoppingCartService } from '../../../core/services/shopping-cart.service';
import { OrderApiService } from '../../../core/services/order-api.service';
import { ShippingAddressRequest } from '../../../core/models/order.model';

@Component({
  selector: 'app-checkout',
  imports: [FormsModule, DecimalPipe, TranslatePipe],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
})
export class CheckoutPage implements OnInit {
  protected readonly cartService = inject(ShoppingCartService);
  private readonly orderApi = inject(OrderApiService);
  private readonly router = inject(Router);

  shipping: ShippingAddressRequest = {
    fullName: '',
    phone: '',
    country: '',
    city: '',
    address: '',
  };

  submitting = false;
  error: string | null = null;

  ngOnInit(): void {
    void this.cartService.load();
  }

  async submit(): Promise<void> {
    this.submitting = true;
    this.error = null;

    try {
      const order = await firstValueFrom(this.orderApi.checkout(this.shipping));
      await this.cartService.clear();
      void this.router.navigate(['/shop/order-confirmation', order?.id]);
    } catch (error) {
      this.error = error instanceof Error ? error.message : 'Checkout failed.';
    } finally {
      this.submitting = false;
    }
  }
}
