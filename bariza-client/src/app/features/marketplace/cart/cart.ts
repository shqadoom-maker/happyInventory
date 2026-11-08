import { Component, OnInit, inject, AfterViewInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { gsap } from 'gsap';

import { ShoppingCartService } from '../../../core/services/shopping-cart.service';
import { Cart } from '../../../core/models/order.model';

@Component({
  selector: 'app-cart',
  imports: [RouterLink, DecimalPipe, TranslatePipe],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
})
export class CartPage implements OnInit, AfterViewInit {
  protected readonly cartService = inject(ShoppingCartService);
  private readonly router = inject(Router);

  ngOnInit(): void {
    void this.cartService.load();
  }

  ngAfterViewInit(): void {
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches) {
      return;
    }

    gsap.from('.cart-item', {
      y: 16,
      opacity: 0,
      duration: 0.4,
      stagger: 0.06,
      ease: 'power2.out',
      clearProps: 'all',
    });
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
