import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-marketplace-placeholder',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './marketplace-placeholder.html',
  styleUrl: './marketplace-placeholder.scss',
})
export class MarketplacePlaceholder {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly titleKey = signal('marketplace.placeholder.title');
  protected readonly bodyKey = signal('marketplace.placeholder.body');
  protected readonly icon = signal('construction');
  protected readonly backLink = signal('/shop/products');
  protected readonly backLabelKey = signal('marketplace.placeholder.backToCatalog');
  protected readonly slug = signal<string | null>(null);

  constructor() {
    this.route.data.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((data) => {
      this.titleKey.set(this.readData(data, 'titleKey', this.titleKey()));
      this.bodyKey.set(this.readData(data, 'bodyKey', this.bodyKey()));
      this.icon.set(this.readData(data, 'icon', this.icon()));
      this.backLink.set(this.readData(data, 'backLink', this.backLink()));
      this.backLabelKey.set(this.readData(data, 'backLabelKey', this.backLabelKey()));
    });

    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.slug.set(params.get('slug'));
    });
  }

  private readData(data: Record<string, unknown>, key: string, fallback: string): string {
    const value = data[key];
    return typeof value === 'string' && value.trim() ? value : fallback;
  }
}
