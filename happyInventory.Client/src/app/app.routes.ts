import { Routes } from '@angular/router';

import { adminGuard } from './core/guards/admin.guard';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'titles.login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login').then((m) => m.Login),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell').then((m) => m.Shell),
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        title: 'titles.dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'warehouses',
        title: 'titles.warehouses',
        loadChildren: () =>
          import('./features/warehouses/warehouses.routes').then((m) => m.WAREHOUSE_ROUTES),
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
        path: 'users',
        title: 'titles.users',
        canActivate: [adminGuard],
        loadChildren: () => import('./features/users/users.routes').then((m) => m.USER_ROUTES),
      },
      {
        path: '**',
        redirectTo: 'dashboard',
      },
    ],
  },
];
