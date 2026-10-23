import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

import { errorMessage } from '../../core/error-message';
import { Product } from '../../core/models/product.model';
import { ProductService } from '../../core/services/product.service';

@Component({
  selector: 'app-product-list',
  imports: [RouterLink, DecimalPipe, TranslatePipe],
  templateUrl: './product-list.html',
})
export class ProductList implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly translate = inject(TranslateService);

  protected readonly products = signal<Product[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly search = signal('');

  protected readonly filtered = computed(() => {
    const term = this.search().trim().toLowerCase();

    if (!term) {
      return this.products();
    }

    return this.products().filter((product) =>
      [product.sku, product.name, product.description]
        .filter((value): value is string => !!value)
        .some((value) => value.toLowerCase().includes(term)),
    );
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productService.list().subscribe({
      next: (products) => this.products.set(products),
      error: (error) => {
        this.error.set(errorMessage(error, this.translate));
        this.loading.set(false);
      },
      complete: () => this.loading.set(false),
    });
  }

  protected updateSearch(value: string): void {
    this.search.set(value);
  }

  protected remove(product: Product): void {
    const message = this.translate.instant('products.confirmDelete', {
      name: product.name,
    });

    if (!confirm(message)) {
      return;
    }

    this.error.set(null);

    this.productService.delete(product.id).subscribe({
      next: () => this.load(),
      error: (error) => this.error.set(errorMessage(error, this.translate)),
    });
  }
}
