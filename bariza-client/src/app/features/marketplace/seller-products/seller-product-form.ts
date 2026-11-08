import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { SellerProductService } from '../../../core/services/seller-product.service';
import { CreateSellerProductRequest, UpdateSellerProductRequest } from '../../../core/models/seller-product.model';

@Component({
  selector: 'app-seller-product-form',
  imports: [RouterLink, FormsModule],
  templateUrl: './seller-product-form.html',
  styleUrl: './seller-product-form.scss',
})
export class SellerProductFormPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(SellerProductService);

  categories: { id: number; name: string }[] = [];
  productId: number | null = null;
  loading = false;
  saving = false;

  model: CreateSellerProductRequest & { isActive: boolean } = {
    name: '',
    slug: '',
    description: '',
    sku: '',
    price: 0,
    discountPrice: null,
    imageUrl: '',
    categoryId: 0,
    isPublished: false,
    isActive: true,
  };

  ngOnInit(): void {
    this.service.getCategories().subscribe((categories) => (this.categories = categories));

    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (id > 0) {
      this.productId = id;
      this.service.getProducts().subscribe((products) => {
        const product = products.find((p) => p.id === id);
        if (product) {
          this.model = {
            name: product.name,
            slug: product.slug,
            description: product.description ?? '',
            sku: product.sku ?? '',
            price: product.price,
            discountPrice: product.discountPrice,
            imageUrl: product.imageUrl ?? '',
            categoryId: product.categoryId,
            isPublished: product.isPublished,
            isActive: product.isActive,
          };
        }
      });
    }
  }

  submit(): void {
    this.saving = true;

    if (this.productId) {
      const request: UpdateSellerProductRequest = { ...this.model };
      this.service.update(this.productId, request).subscribe({
        next: () => void this.router.navigate(['/shop/seller/products']),
        error: () => (this.saving = false),
      });
    } else {
      const request: CreateSellerProductRequest = { ...this.model };
      this.service.create(request).subscribe({
        next: () => void this.router.navigate(['/shop/seller/products']),
        error: () => (this.saving = false),
      });
    }
  }
}
