import { Routes } from '@angular/router';

export const STOCK_MOVEMENT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./stock-movement-list').then((m) => m.StockMovementList),
  },
];
