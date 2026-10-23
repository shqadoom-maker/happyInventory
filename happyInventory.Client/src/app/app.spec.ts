import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';

import { App } from './app';

const english = {
  brand: { name: 'Happy Inventory', subtitle: 'Warehouse management' },
  shell: {
    eyebrow: 'Happy Company',
    title: 'Inventory management',
    stack: 'SQLite · EF Core 6',
    api: 'API: ASP.NET Core .NET 6',
  },
  language: { label: 'Language' },
  nav: {
    dashboard: 'Dashboard',
    warehouses: 'Warehouses',
    products: 'Products',
    inventory: 'Inventory',
    stockMovements: 'Stock movements',
    locations: 'Locations',
  },
};

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideTranslateService({ lang: 'en' })],
    }).compileComponents();

    TestBed.inject(TranslateService).setTranslation('en', english);
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the shell title', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Inventory management');
  });
});
