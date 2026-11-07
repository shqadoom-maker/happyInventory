import { Routes } from '@angular/router';

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
    path: '**',
    redirectTo: '',
  },
];
