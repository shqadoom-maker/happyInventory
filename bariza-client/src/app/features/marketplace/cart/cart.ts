import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { ShoppingCartService } from '../../../core/services/shopping-cart.service';
import { Cart } from '../../../core/models/order.model';

@Component({
  selector: 'app-cart',
  imports: [RouterLink, DecimalPipe, TranslatePipe],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
})
export class CartPage implements OnInit {
  protected readonly cartService = inject(ShoppingCartService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    void this.cartService.load();
  }

  protected async updateQuantity(itemId: number, quantity: number): Promise<void> {
    if (quantity < 1) {
      return;
    }

    await this.cartService.updateItem(itemId, quantity);
  }

  protected async remove(itemId: number): Promise<void> {
    await this.cartService.removeItem(itemId);
  }

  protected checkout(): void {
    void this.router.navigate(['/shop/checkout']);
  }
}
