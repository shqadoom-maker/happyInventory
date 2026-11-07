import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { of, Subject, throwError } from 'rxjs';

import {
  MarketplaceCategory,
  MarketplaceProduct,
  MarketplaceProductPage,
} from '../../../core/models/marketplace.model';
import { MarketplaceProductService } from '../../../core/services/marketplace-product.service';
import { MarketplaceProductList } from './marketplace-product-list';

const categories: MarketplaceCategory[] = [
  { id: 1, name: 'Electronics', slug: 'electronics', productCount: 2 },
  { id: 2, name: 'Home & Living', slug: 'home-living', productCount: 1 },
];

const product: MarketplaceProduct = {
  id: 1,
  name: 'Laptop Alpha',
  slug: 'laptop-alpha',
  sku: 'LAP-1',
  price: 1000,
  discountPrice: 900,
  imageUrl: '/images/marketplace/laptop.svg',
  categoryId: 1,
  categoryName: 'Electronics',
  sellerName: 'Nova Tech',
  isAvailable: true,
};

function productPage(overrides: Partial<MarketplaceProductPage> = {}): MarketplaceProductPage {
  return {
    items: [product],
    totalCount: 1,
    pageNumber: 1,
    pageSize: 12,
    totalPages: 1,
    ...overrides,
  };
}

describe('MarketplaceProductList', () => {
  let catalog: {
    getProducts: ReturnType<typeof vi.fn>;
    getCategories: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    catalog = {
      getProducts: vi.fn().mockReturnValue(of(productPage())),
      getCategories: vi.fn().mockReturnValue(of(categories)),
    };

    await TestBed.configureTestingModule({
      imports: [MarketplaceProductList],
      providers: [
        provideRouter([{ path: 'shop/products', component: MarketplaceProductList }]),
        provideTranslateService(),
        { provide: MarketplaceProductService, useValue: catalog },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('renders products and sends URL filters to the backend query', async () => {
    const { harness } = await open(
      '?search=laptop&categoryId=2&minPrice=100&maxPrice=500&sortBy=priceAsc&pageNumber=2&pageSize=24',
    );

    expect(catalog.getProducts).toHaveBeenCalledWith({
      search: 'laptop',
      categoryId: 2,
      minPrice: 100,
      maxPrice: 500,
      sortBy: 'priceAsc',
      pageNumber: 2,
      pageSize: 24,
    });
    expect(catalog.getCategories).toHaveBeenCalled();

    const text = harness.routeNativeElement?.textContent ?? '';
    expect(text).toContain('Laptop Alpha');
    expect(text).toContain('Electronics');
    expect(harness.routeNativeElement?.querySelectorAll('.product-card').length).toBe(1);
  });

  it('shows a loading state while the catalog request is pending', async () => {
    catalog.getProducts.mockReturnValue(new Subject<MarketplaceProductPage>().asObservable());

    const { harness } = await open();

    expect(harness.routeNativeElement?.querySelector('.loading-state')).toBeTruthy();
  });

  it('debounces search input before navigating with the new query', async () => {
    const { component } = await open();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    vi.useFakeTimers();
    (component as unknown as { onSearchInput(value: string): void }).onSearchInput('laptop');

    await vi.advanceTimersByTimeAsync(300);
    expect(navigate).not.toHaveBeenCalled();

    await vi.advanceTimersByTimeAsync(50);
    expect(navigate).toHaveBeenCalledWith(['/shop/products'], {
      queryParams: { search: 'laptop' },
      replaceUrl: true,
    });
  });

  it('resets pagination when category, sort, or page size changes', async () => {
    const { component } = await open('?pageNumber=2&pageSize=24');
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    (component as unknown as { selectCategory(id: number): void }).selectCategory(2);
    expect(navigate).toHaveBeenCalledWith(['/shop/products'], {
      queryParams: { categoryId: 2 },
      replaceUrl: true,
    });

    navigate.mockClear();
    (component as unknown as { onSortChange(value: string): void }).onSortChange('priceDesc');
    expect(navigate).toHaveBeenCalledWith(['/shop/products'], {
      queryParams: { categoryId: 2, sortBy: 'priceDesc' },
      replaceUrl: true,
    });

    navigate.mockClear();
    (component as unknown as { onPageSizeChange(value: string): void }).onPageSizeChange('36');
    expect(navigate).toHaveBeenCalledWith(['/shop/products'], {
      queryParams: { categoryId: 2, sortBy: 'priceDesc', pageSize: 36 },
      replaceUrl: true,
    });
  });

  it('validates the price range before navigating', async () => {
    const { component } = await open();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    (component as unknown as { onMinPriceChange(value: string): void }).onMinPriceChange('200');
    expect(navigate).toHaveBeenCalledWith(['/shop/products'], {
      queryParams: { minPrice: 200 },
      replaceUrl: true,
    });

    navigate.mockClear();
    (component as unknown as { onMaxPriceChange(value: string): void }).onMaxPriceChange('100');

    expect(navigate).not.toHaveBeenCalled();
    expect(
      (component as unknown as { filterError(): string | null }).filterError(),
    ).toBeTruthy();
  });

  it('moves between pages and clamps out-of-range requests', async () => {
    catalog.getProducts.mockReturnValue(
      of(productPage({ totalCount: 30, totalPages: 3 })),
    );
    const { component } = await open();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    (component as unknown as { goToPage(page: number): void }).goToPage(2);
    expect(navigate).toHaveBeenCalledWith(['/shop/products'], {
      queryParams: { pageNumber: 2 },
      replaceUrl: true,
    });

    navigate.mockClear();
    (component as unknown as { goToPage(page: number): void }).goToPage(9);
    expect(navigate).toHaveBeenCalledWith(['/shop/products'], {
      queryParams: { pageNumber: 3 },
      replaceUrl: true,
    });
  });

  it('shows the no-results state when filters return no products', async () => {
    catalog.getProducts.mockReturnValue(
      of(productPage({ items: [], totalCount: 0, totalPages: 0 })),
    );

    const { harness } = await open('?search=does-not-exist');

    expect(harness.routeNativeElement?.querySelector('.empty-state')).toBeTruthy();
    expect(harness.routeNativeElement?.textContent).toContain(
      'marketplace.products.noResultsTitle',
    );
  });

  it('shows the empty catalog state when no filters are active', async () => {
    catalog.getProducts.mockReturnValue(
      of(productPage({ items: [], totalCount: 0, totalPages: 0 })),
    );

    const { harness } = await open();

    expect(harness.routeNativeElement?.querySelector('.empty-state')).toBeTruthy();
    expect(harness.routeNativeElement?.textContent).toContain(
      'marketplace.products.emptyTitle',
    );
  });

  it('shows an error state and retries the request', async () => {
    catalog.getProducts.mockReturnValue(
      throwError(() => new Error('Catalog service unavailable')),
    );

    const { harness } = await open();

    expect(harness.routeNativeElement?.querySelector('.error-state')).toBeTruthy();
    expect(harness.routeNativeElement?.textContent).toContain('Catalog service unavailable');

    catalog.getProducts.mockReturnValue(of(productPage()));
    const retry = harness.routeNativeElement?.querySelector(
      '.error-state button',
    ) as HTMLButtonElement | null;
    retry?.click();
    harness.fixture.detectChanges();

    expect(catalog.getProducts).toHaveBeenCalledTimes(2);
  });

  async function open(query = ''): Promise<{
    harness: RouterTestingHarness;
    component: MarketplaceProductList;
  }> {
    const harness = await RouterTestingHarness.create();
    const component = await harness.navigateByUrl(
      `/shop/products${query}`,
      MarketplaceProductList,
    );

    harness.fixture.detectChanges();

    return {
      harness,
      component: component as MarketplaceProductList,
    };
  }
});
