import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import {
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  map,
  merge,
  of,
  switchMap,
  tap,
} from 'rxjs';

import { errorMessage } from '../../../core/error-message';
import {
  MarketplaceCategory,
  MarketplaceProduct,
  MarketplaceProductQuery,
  MarketplaceSort,
} from '../../../core/models/marketplace.model';
import { MarketplaceProductService } from '../../../core/services/marketplace-product.service';
import { ShoppingCartService } from '../../../core/services/shopping-cart.service';

const DEFAULT_PAGE_SIZE = 12;
const SEARCH_DEBOUNCE_MS = 350;

@Component({
  selector: 'app-marketplace-product-list',
  imports: [DecimalPipe, RouterLink, TranslatePipe],
  templateUrl: './marketplace-product-list.html',
  styleUrl: './marketplace-product-list.scss',
})
export class MarketplaceProductList implements OnInit {
  private readonly catalog = inject(MarketplaceProductService);
  private readonly cart = inject(ShoppingCartService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchChanges = new Subject<string>();
  private readonly retryRequests = new Subject<void>();

  protected readonly products = signal<MarketplaceProduct[]>([]);
  protected readonly categories = signal<MarketplaceCategory[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);
  protected readonly pageNumber = signal(1);
  protected readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  protected readonly search = signal('');
  protected readonly searchInput = signal('');
  protected readonly categoryId = signal<number | null>(null);
  protected readonly minPrice = signal<number | null>(null);
  protected readonly maxPrice = signal<number | null>(null);
  protected readonly minPriceInput = signal('');
  protected readonly maxPriceInput = signal('');
  protected readonly sortBy = signal<MarketplaceSort>('newest');
  protected readonly filterError = signal<string | null>(null);
  protected readonly pageSizeOptions = [12, 24, 36, 48, 60];

  protected readonly hasFilters = computed(
    () =>
      this.search().trim().length > 0 ||
      this.categoryId() !== null ||
      this.minPrice() !== null ||
      this.maxPrice() !== null ||
      this.sortBy() !== 'newest',
  );

  protected readonly activeCategory = computed(() => {
    const selected = this.categoryId();
    return selected == null
      ? null
      : this.categories().find((category) => category.id === selected) ?? null;
  });

  private currentQuery: MarketplaceProductQuery = {
    pageNumber: 1,
    pageSize: DEFAULT_PAGE_SIZE,
    sortBy: 'newest',
  };

  constructor() {
    this.searchChanges
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((value) => {
        this.updateQuery({
          search: value.trim() || undefined,
          pageNumber: 1,
        });
      });
  }

  ngOnInit(): void {
    this.loadCategories();

    const routeQuery$ = this.route.queryParamMap.pipe(
      map((params) => this.toQuery(params)),
      distinctUntilChanged((left, right) => this.sameQuery(left, right)),
    );

    merge(
      routeQuery$,
      this.retryRequests.pipe(map(() => this.currentQuery)),
    )
      .pipe(
        tap((query) => {
          this.currentQuery = query;
          this.applyQueryToState(query);
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap((query) =>
          this.catalog.getProducts(query).pipe(
            map((page) => ({ page, error: null as string | null })),
            catchError((requestError: unknown) =>
              of({
                page: null,
                error: errorMessage(requestError, this.translate),
              }),
            ),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ page, error }) => {
        this.loading.set(false);

        if (error) {
          this.error.set(error);
          this.products.set([]);
          this.totalCount.set(0);
          this.totalPages.set(0);
          return;
        }

        if (page) {
          this.products.set(page.items);
          this.totalCount.set(page.totalCount);
          this.totalPages.set(page.totalPages);
          this.pageNumber.set(page.pageNumber);
          this.pageSize.set(page.pageSize);
        }
      });
  }

  protected onSearchInput(value: string): void {
    this.searchInput.set(value);
    this.searchChanges.next(value);
  }

  protected submitSearch(event: Event): void {
    event.preventDefault();
    this.updateQuery({
      search: this.searchInput().trim() || undefined,
      pageNumber: 1,
    });
  }

  protected selectCategory(categoryId: number | null): void {
    this.filterError.set(null);
    this.categoryId.set(categoryId);
    this.updateQuery({ categoryId: categoryId ?? undefined, pageNumber: 1 });
  }

  protected onMinPriceChange(value: string): void {
    this.minPriceInput.set(value);
    const parsed = this.parsePrice(value);

    if (value.trim() && parsed == null) {
      this.filterError.set(this.translate.instant('marketplace.products.invalidPrice'));
      return;
    }

    if (parsed != null && this.maxPrice() != null && parsed > this.maxPrice()!) {
      this.filterError.set(
        this.translate.instant('marketplace.products.invalidPriceRange'),
      );
      return;
    }

    this.filterError.set(null);
    this.minPrice.set(parsed);
    this.updateQuery({ minPrice: parsed ?? undefined, pageNumber: 1 });
  }

  protected onMaxPriceChange(value: string): void {
    this.maxPriceInput.set(value);
    const parsed = this.parsePrice(value);

    if (value.trim() && parsed == null) {
      this.filterError.set(this.translate.instant('marketplace.products.invalidPrice'));
      return;
    }

    if (parsed != null && this.minPrice() != null && parsed < this.minPrice()!) {
      this.filterError.set(
        this.translate.instant('marketplace.products.invalidPriceRange'),
      );
      return;
    }

    this.filterError.set(null);
    this.maxPrice.set(parsed);
    this.updateQuery({ maxPrice: parsed ?? undefined, pageNumber: 1 });
  }

  protected onSortChange(value: string): void {
    const sortBy = this.parseSort(value);
    this.sortBy.set(sortBy);
    this.updateQuery({ sortBy, pageNumber: 1 });
  }

  protected onPageSizeChange(value: string): void {
    const parsed = Number(value);
    const pageSize = Number.isInteger(parsed) && parsed > 0 ? parsed : DEFAULT_PAGE_SIZE;
    this.pageSize.set(pageSize);
    this.updateQuery({ pageSize, pageNumber: 1 });
  }

  protected goToPage(pageNumber: number): void {
    const target = Math.min(Math.max(1, pageNumber), Math.max(1, this.totalPages()));
    if (target === this.pageNumber()) {
      return;
    }

    this.pageNumber.set(target);
    this.updateQuery({ pageNumber: target });
  }

  protected previousPage(): void {
    this.goToPage(this.pageNumber() - 1);
  }

  protected nextPage(): void {
    this.goToPage(this.pageNumber() + 1);
  }

  protected clearFilters(): void {
    this.filterError.set(null);
    this.searchInput.set('');
    void this.router.navigate(['/shop/products']);
  }

  protected retry(): void {
    this.retryRequests.next();
  }

  protected async addToCart(product: MarketplaceProduct): Promise<void> {
    if (!product.isAvailable) {
      return;
    }

    await this.cart.addItem(
      product.id,
      product.name,
      product.imageUrl ?? null,
      product.discountPrice ?? product.price,
      product.sellerName ?? null,
    );
  }

  protected onImageError(event: Event): void {
    const image = event.target as HTMLImageElement;
    if (image.dataset['fallback'] === 'true') {
      return;
    }

    image.dataset['fallback'] = 'true';
    image.src = '/images/marketplace/product-placeholder.svg';
  }

  protected discountPercent(product: MarketplaceProduct): number {
    if (product.discountPrice == null || product.price <= 0) {
      return 0;
    }

    return Math.max(
      0,
      Math.round(((product.price - product.discountPrice) / product.price) * 100),
    );
  }

  private loadCategories(): void {
    this.catalog
      .getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (categories) => this.categories.set(categories),
        error: () => this.categories.set([]),
      });
  }

  private toQuery(params: ParamMap): MarketplaceProductQuery {
    const search = (params.get('search') ?? '').trim();

    return {
      search: search || undefined,
      categoryId: this.parsePositiveInteger(params.get('categoryId')) ?? undefined,
      minPrice: this.parsePrice(params.get('minPrice')) ?? undefined,
      maxPrice: this.parsePrice(params.get('maxPrice')) ?? undefined,
      sortBy: this.parseSort(params.get('sortBy')),
      pageNumber: this.parsePositiveInteger(params.get('pageNumber')) ?? 1,
      pageSize: this.parsePageSize(params.get('pageSize')),
    };
  }

  private applyQueryToState(query: MarketplaceProductQuery): void {
    this.search.set(query.search ?? '');
    this.searchInput.set(query.search ?? '');
    this.categoryId.set(query.categoryId ?? null);
    this.minPrice.set(query.minPrice ?? null);
    this.maxPrice.set(query.maxPrice ?? null);
    this.minPriceInput.set(query.minPrice != null ? String(query.minPrice) : '');
    this.maxPriceInput.set(query.maxPrice != null ? String(query.maxPrice) : '');
    this.sortBy.set(query.sortBy ?? 'newest');
    this.pageNumber.set(query.pageNumber ?? 1);
    this.pageSize.set(query.pageSize ?? DEFAULT_PAGE_SIZE);
  }

  private updateQuery(partial: Partial<MarketplaceProductQuery>): void {
    const query: MarketplaceProductQuery = {
      search: this.search() || undefined,
      categoryId: this.categoryId() ?? undefined,
      minPrice: this.minPrice() ?? undefined,
      maxPrice: this.maxPrice() ?? undefined,
      sortBy: this.sortBy(),
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      ...partial,
    };

    const queryParams: Record<string, string | number> = {};

    if (query.search?.trim()) {
      queryParams['search'] = query.search.trim();
    }
    if (query.categoryId != null) {
      queryParams['categoryId'] = query.categoryId;
    }
    if (query.minPrice != null) {
      queryParams['minPrice'] = query.minPrice;
    }
    if (query.maxPrice != null) {
      queryParams['maxPrice'] = query.maxPrice;
    }
    if ((query.sortBy ?? 'newest') !== 'newest') {
      queryParams['sortBy'] = query.sortBy!;
    }
    if ((query.pageNumber ?? 1) > 1) {
      queryParams['pageNumber'] = query.pageNumber!;
    }
    if ((query.pageSize ?? DEFAULT_PAGE_SIZE) !== DEFAULT_PAGE_SIZE) {
      queryParams['pageSize'] = query.pageSize!;
    }

    void this.router.navigate(['/shop/products'], {
      queryParams,
      replaceUrl: true,
    });
  }

  private parsePositiveInteger(value: string | null): number | null {
    if (value == null || !value.trim()) {
      return null;
    }

    const parsed = Number(value);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
  }

  private parsePrice(value: string | null): number | null {
    if (value == null || !value.trim()) {
      return null;
    }

    const parsed = Number(value);
    return Number.isFinite(parsed) && parsed >= 0 ? parsed : null;
  }

  private parsePageSize(value: string | null): number {
    const parsed = this.parsePositiveInteger(value);
    return parsed != null && parsed <= 60 ? parsed : DEFAULT_PAGE_SIZE;
  }

  private parseSort(value: string | null): MarketplaceSort {
    switch (value) {
      case 'priceAsc':
      case 'priceDesc':
      case 'nameAsc':
        return value;
      default:
        return 'newest';
    }
  }

  private sameQuery(
    left: MarketplaceProductQuery,
    right: MarketplaceProductQuery,
  ): boolean {
    return (
      (left.search ?? '') === (right.search ?? '') &&
      (left.categoryId ?? null) === (right.categoryId ?? null) &&
      (left.minPrice ?? null) === (right.minPrice ?? null) &&
      (left.maxPrice ?? null) === (right.maxPrice ?? null) &&
      (left.sortBy ?? 'newest') === (right.sortBy ?? 'newest') &&
      (left.pageNumber ?? 1) === (right.pageNumber ?? 1) &&
      (left.pageSize ?? DEFAULT_PAGE_SIZE) === (right.pageSize ?? DEFAULT_PAGE_SIZE)
    );
  }
}
