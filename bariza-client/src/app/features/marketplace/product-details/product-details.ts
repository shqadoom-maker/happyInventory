import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';

import { MarketplaceProductService } from '../../../core/services/marketplace-product.service';
import { ProductDetails } from '../../../core/models/product-details.model';

@Component({
  selector: 'app-product-details',
  imports: [RouterLink, DecimalPipe, TranslatePipe],
  templateUrl: './product-details.html',
  styleUrl: './product-details.scss',
})
export class ProductDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly catalog = inject(MarketplaceProductService);

  product: ProductDetails | null = null;
  loading = true;
  selectedImage = 0;

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');

    if (slug) {
      this.catalog.getProductDetails(slug).subscribe({
        next: (product) => {
          this.product = product;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        },
      });
    }
  }

  selectImage(index: number): void {
    this.selectedImage = index;
  }
}
