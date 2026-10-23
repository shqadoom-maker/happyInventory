import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  protected readonly cards = [
    {
      path: '/warehouses',
      icon: '⌂',
      titleKey: 'nav.warehouses',
      descriptionKey: 'dashboard.cards.warehouses',
    },
    {
      path: '/products',
      icon: '◫',
      titleKey: 'nav.products',
      descriptionKey: 'dashboard.cards.products',
    },
    {
      path: '/inventory',
      icon: '▤',
      titleKey: 'nav.inventory',
      descriptionKey: 'dashboard.cards.inventory',
    },
    {
      path: '/stock-movements',
      icon: '⇅',
      titleKey: 'nav.stockMovements',
      descriptionKey: 'dashboard.cards.stockMovements',
    },
    {
      path: '/locations',
      icon: '⌖',
      titleKey: 'nav.locations',
      descriptionKey: 'dashboard.cards.locations',
    },
  ];
}
