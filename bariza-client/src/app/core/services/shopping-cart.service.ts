import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthService } from './auth.service';
import { CartApiService } from './cart-api.service';
import { Cart, CartItem } from '../models/order.model';

const GUEST_CART_KEY = 'bariza-market.guest-cart';

@Injectable({ providedIn: 'root' })
export class ShoppingCartService {
  private readonly auth = inject(AuthService);
  private readonly cartApi = inject(CartApiService);

  readonly cart = signal<Cart>({ id: 0, items: [], totalAmount: 0 });
  readonly loading = signal(false);

  async load(): Promise<void> {
    this.loading.set(true);

    try {
      if (this.auth.isAuthenticated()) {
        const cart = await firstValueFrom(this.cartApi.getCart());
        this.cart.set(cart);
      } else {
        this.cart.set(this.readGuestCart());
      }
    } finally {
      this.loading.set(false);
    }
  }

  async addItem(productId: number, productName: string, imageUrl: string | null, unitPrice: number, sellerName: string | null, quantity = 1): Promise<void> {
    if (this.auth.isAuthenticated()) {
      const cart = await firstValueFrom(this.cartApi.addItem({ productId, quantity }));
      this.cart.set(cart);
      return;
    }

    const cart = this.readGuestCart();
    const existing = cart.items.find((item) => item.productId === productId);

    if (existing) {
      existing.quantity += quantity;
      existing.subtotal = existing.unitPrice * existing.quantity;
    } else {
      cart.items.push({
        id: -productId,
        productId,
        productName,
        imageUrl,
        unitPrice,
        quantity,
        subtotal: unitPrice * quantity,
        sellerName,
        isAvailable: true,
      });
    }

    cart.totalAmount = cart.items.reduce((sum, item) => sum + item.subtotal, 0);
    this.writeGuestCart(cart);
    this.cart.set(cart);
  }

  async updateItem(itemId: number, quantity: number): Promise<void> {
    if (this.auth.isAuthenticated()) {
      const cart = await firstValueFrom(this.cartApi.updateItem(itemId, { quantity }));
      this.cart.set(cart);
      return;
    }

    const cart = this.readGuestCart();
    const item = cart.items.find((entry) => entry.id === itemId);

    if (item) {
      item.quantity = quantity;
      item.subtotal = item.unitPrice * quantity;
      cart.totalAmount = cart.items.reduce((sum, item) => sum + item.subtotal, 0);
      this.writeGuestCart(cart);
      this.cart.set(cart);
    }
  }

  async removeItem(itemId: number): Promise<void> {
    if (this.auth.isAuthenticated()) {
      await firstValueFrom(this.cartApi.removeItem(itemId));
      await this.load();
      return;
    }

    const cart = this.readGuestCart();
    cart.items = cart.items.filter((item) => item.id !== itemId);
    cart.totalAmount = cart.items.reduce((sum, item) => sum + item.subtotal, 0);
    this.writeGuestCart(cart);
    this.cart.set(cart);
  }

  async clear(): Promise<void> {
    if (this.auth.isAuthenticated()) {
      await firstValueFrom(this.cartApi.clear());
    } else {
      localStorage.removeItem(GUEST_CART_KEY);
    }

    this.cart.set({ id: 0, items: [], totalAmount: 0 });
  }

  async syncGuestCartToServer(): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      return;
    }

    const guestCart = this.readGuestCart();

    for (const item of guestCart.items) {
      await firstValueFrom(this.cartApi.addItem({ productId: item.productId, quantity: item.quantity }));
    }

    localStorage.removeItem(GUEST_CART_KEY);
    await this.load();
  }

  private readGuestCart(): Cart {
    try {
      const raw = localStorage.getItem(GUEST_CART_KEY);
      return raw ? (JSON.parse(raw) as Cart) : { id: 0, items: [], totalAmount: 0 };
    } catch {
      return { id: 0, items: [], totalAmount: 0 };
    }
  }

  private writeGuestCart(cart: Cart): void {
    try {
      localStorage.setItem(GUEST_CART_KEY, JSON.stringify(cart));
    } catch {
      // Ignore storage failures.
    }
  }
}
