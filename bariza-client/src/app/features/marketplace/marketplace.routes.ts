import { Routes } from '@angular/router';

import { authGuard } from '../../core/guards/auth.guard';

/**
 * Public marketplace routes. Product details and registration are explicit
 * placeholders in Phase 2; the catalog itself is fully implemented.
 */
export const MARKETPLACE_ROUTES: Routes = [
  {
    path: '',
    title: 'titles.marketplaceHome',
    loadComponent: () => import('./home/marketplace-home').then((m) => m.MarketplaceHome),
  },
  {
    path: 'products',
    title: 'titles.marketplaceProducts',
    loadComponent: () =>
      import('./products/marketplace-product-list').then((m) => m.MarketplaceProductList),
  },
  {
    path: 'products/:slug',
    title: 'titles.productDetails',
    data: {
      titleKey: 'marketplace.placeholder.productDetailsTitle',
      bodyKey: 'marketplace.placeholder.productDetailsBody',
      icon: 'inventory_2',
      backLink: '/shop/products',
      backLabelKey: 'marketplace.placeholder.backToCatalog',
    },
    loadComponent: () =>
      import('./placeholder/marketplace-placeholder').then((m) => m.MarketplacePlaceholder),
  },
  {
    path: 'register',
    title: 'titles.register',
    data: {
      titleKey: 'marketplace.placeholder.registerTitle',
      bodyKey: 'marketplace.placeholder.registerBody',
      icon: 'person_add',
      backLink: '/shop',
      backLabelKey: 'marketplace.placeholder.backToShop',
    },
    loadComponent: () =>
      import('./placeholder/marketplace-placeholder').then((m) => m.MarketplacePlaceholder),
  },
  {
    path: 'cart',
    title: 'titles.cart',
    loadComponent: () => import('./cart/cart').then((m) => m.CartPage),
  },
  {
    path: 'checkout',
    title: 'titles.checkout',
    canActivate: [authGuard],
    loadComponent: () => import('./checkout/checkout').then((m) => m.CheckoutPage),
  },
  {
    path: 'orders',
    title: 'titles.myOrders',
    canActivate: [authGuard],
    loadComponent: () => import('./orders/my-orders').then((m) => m.MyOrdersPage),
  },
  {
    path: 'orders/:id',
    title: 'titles.orderDetails',
    canActivate: [authGuard],
    loadComponent: () => import('./orders/order-details').then((m) => m.OrderDetailsPage),
  },
  {
    path: 'order-confirmation/:id',
    title: 'titles.orderConfirmation',
    canActivate: [authGuard],
    loadComponent: () => import('./orders/order-confirmation').then((m) => m.OrderConfirmationPage),
  },
  {
    path: 'seller/orders',
    title: 'titles.sellerOrders',
    canActivate: [authGuard],
    loadComponent: () => import('./seller-orders/seller-orders').then((m) => m.SellerOrdersPage),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
