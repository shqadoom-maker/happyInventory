import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard',
  },
  {
    path: 'dashboard',
    title: 'titles.dashboard',
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
  },
  {
    path: 'warehouses',
    title: 'titles.warehouses',
    loadChildren: () =>
      import('./features/warehouses/warehouses.routes').then((m) => m.WAREHOUSE_ROUTES),
  },
  {
    path: 'products',
    title: 'titles.products',
    loadChildren: () =>
      import('./features/products/products.routes').then((m) => m.PRODUCT_ROUTES),
  },
  {
    path: 'inventory',
    title: 'titles.inventory',
    loadChildren: () =>
      import('./features/inventory/inventory.routes').then((m) => m.INVENTORY_ROUTES),
  },
  {
    path: 'stock-movements',
    title: 'titles.stockMovements',
    loadChildren: () =>
      import('./features/stock-movements/stock-movements.routes').then(
        (m) => m.STOCK_MOVEMENT_ROUTES,
      ),
  },
  {
    path: 'locations',
    title: 'titles.locations',
    loadComponent: () => import('./features/locations/locations').then((m) => m.Locations),
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];
