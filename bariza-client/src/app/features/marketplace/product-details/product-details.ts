import { Component, OnInit, inject, ChangeDetectorRef, AfterViewInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { gsap } from 'gsap';

import { MarketplaceProductService } from '../../../core/services/marketplace-product.service';
import { ShoppingCartService } from '../../../core/services/shopping-cart.service';
import { ProductDetails } from '../../../core/models/product-details.model';

@Component({
  selector: 'app-product-details',
  imports: [RouterLink, DecimalPipe, TranslatePipe],
  templateUrl: './product-details.html',
  styleUrl: './product-details.scss',
})
export class ProductDetailsPage implements OnInit, AfterViewInit {
  private readonly route = inject(ActivatedRoute);
  private readonly catalog = inject(MarketplaceProductService);
  private readonly cart = inject(ShoppingCartService);
  private readonly cdr = inject(ChangeDetectorRef);

  product: ProductDetails | null = null;
  loading = true;
  selectedImage = 0;
  adding = false;

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');

    if (slug) {
      this.catalog.getProductDetails(slug).subscribe({
        next: (product) => {
          this.product = product;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.loading = false;
          this.cdr.detectChanges();
        },
      });
    } else {
      this.loading = false;
    }
  }

  async addToCart(): Promise<void> {
    if (!this.product || !this.product.isAvailable) {
      return;
    }

    this.adding = true;
    try {
      await this.cart.addItem(
        this.product.id,
        this.product.name,
        this.product.imageUrl ?? null,
        this.product.discountPrice ?? this.product.price,
        this.product.sellerName ?? null,
      );
    } finally {
      this.adding = false;
      this.cdr.detectChanges();
    }
  }

  ngAfterViewInit(): void {
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)')?.matches) {
      return;
    }

    gsap.from('.product-details-page > *', {
      y: 18,
      opacity: 0,
      duration: 0.5,
      stagger: 0.08,
      ease: 'power2.out',
      clearProps: 'all',
    });
  }

  selectImage(index: number): void {
    this.selectedImage = index;
  }
}
