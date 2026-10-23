import { Routes } from '@angular/router';

export const PRODUCT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./product-list').then((m) => m.ProductList),
  },
  {
    path: 'new',
    loadComponent: () => import('./product-form').then((m) => m.ProductForm),
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./product-form').then((m) => m.ProductForm),
  },
];
