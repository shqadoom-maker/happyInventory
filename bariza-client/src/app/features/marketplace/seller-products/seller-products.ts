import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { SellerProductService } from '../../../core/services/seller-product.service';
import { SellerProduct } from '../../../core/models/seller-product.model';

@Component({
  selector: 'app-seller-products',
  imports: [RouterLink],
  templateUrl: './seller-products.html',
  styleUrl: './seller-products.scss',
})
export class SellerProductsPage implements OnInit {
  private readonly service = inject(SellerProductService);
  private readonly router = inject(Router);

  products: SellerProduct[] = [];
  loading = true;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.service.getProducts().subscribe({
      next: (products) => {
        this.products = products;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  edit(product: SellerProduct): void {
    void this.router.navigate(['/shop/seller/products', product.id, 'edit']);
  }

  remove(product: SellerProduct): void {
    if (!confirm('Delete this product?')) {
      return;
    }

    this.service.delete(product.id).subscribe(() => this.load());
  }
}
