import { Routes } from '@angular/router';

export const WAREHOUSE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./warehouse-list').then((m) => m.WarehouseList),
  },
  {
    path: 'new',
    loadComponent: () => import('./warehouse-form').then((m) => m.WarehouseForm),
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./warehouse-form').then((m) => m.WarehouseForm),
  },
  {
    path: ':warehouseId/items',
    loadComponent: () => import('./warehouse-items-list').then((m) => m.WarehouseItemsList),
  },
  {
    path: ':warehouseId/items/new',
    loadComponent: () => import('./warehouse-item-form').then((m) => m.WarehouseItemForm),
  },
  {
    path: ':warehouseId/items/:id/edit',
    loadComponent: () => import('./warehouse-item-form').then((m) => m.WarehouseItemForm),
  },
];
