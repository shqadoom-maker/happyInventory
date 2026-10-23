# Happy Inventory — Solution Structure

Warehouse inventory management for Happy Company. Warehouses live in different
countries and cities; stock is tracked per warehouse and can be updated by
internal users (Angular UI) or by external systems (eCommerce, ERP, etc.).

## Technology

| Layer | Choice |
| --- | --- |
| Backend | ASP.NET Core .NET 6 Web API (`happyInventory`) |
| Frontend | Angular 22 (`happyInventory.Client`, Angular CLI 22.2.4) |
| Database | SQLite (`app.db`, created by EF Core migrations) |
| ORM | EF Core 6 |

## Projects

```
happyInventory/
├─ happyInventory.slnx              Solution (API)
├─ docs/                            Design notes
├─ happyInventory/                  Backend API
│  ├─ Common/                       Shared primitives (ApiResponse, PagedResult, exceptions)
│  ├─ Controllers/                  HTTP endpoints
│  ├─ Data/                         AppDbContext + entity configurations
│  │  └─ Configurations/            IEntityTypeConfiguration<T> classes
│  ├─ DTOs/                         Request/response contracts
│  │  ├─ Warehouses/
│  │  ├─ Products/
│  │  └─ Inventory/
│  ├─ Middleware/                   Error handling, API-key check for external callers
│  ├─ Migrations/                   EF Core migrations
│  ├─ Models/                       Domain entities
│  ├─ Repositories/                 Data access
│  │  └─ Interfaces/
│  └─ Services/                     Business logic
│     └─ Interfaces/
└─ happyInventory.Client/           Angular application
   ├─ proxy.conf.json              Dev proxy: /api -> https://localhost:7157
   ├─ public/i18n/                 en.json + ar.json translation files
   └─ src/
      ├─ environments/             apiBaseUrl per environment
      └─ app/
         ├─ core/
         │  ├─ interceptors/       HTTP error normalisation
         │  ├─ models/             API contracts (warehouse, product, inventory, …)
         │  ├─ services/           Typed HttpClient wrappers per resource
         │  │  └─ language.service.ts  Active language, persistence, <html lang/dir>
         │  └─ localized-title.strategy.ts  Translates browser-tab titles
         ├─ features/
         │  ├─ dashboard/
         │  ├─ warehouses/         list + create/edit form
         │  ├─ products/           list + create/edit form
         │  ├─ inventory/          stock levels + movement form
         │  ├─ stock-movements/    audit trail
         │  └─ locations/          countries + cities
         ├─ app.routes.ts          Lazy-loaded feature routes
         ├─ app.config.ts          Router, HttpClient and translation providers
         └─ styles.scss            Design tokens, shared UI classes, RTL overrides
```

Layering inside the API: `Controller → Service → Repository → AppDbContext → SQLite`.
Controllers only translate HTTP; validation, stock rules and transaction handling
belong in services.

## Domain model (planned)

| Entity | Purpose |
| --- | --- |
| `Country` | Country where a warehouse is located |
| `City` | City inside a country |
| `Warehouse` | Physical warehouse (name, code, city, address, capacity) |
| `Product` | Sellable item (SKU, name, unit price, active flag) |
| `InventoryItem` | Quantity of one product in one warehouse; unique on (WarehouseId, ProductId) |
| `StockMovement` | Audit trail of every quantity change, with source and reference |

`StockMovement` is what makes "receive updates from the outside world regardless
of the source" traceable: both the Angular UI and external systems post movements,
and the current `InventoryItem.Quantity` is the running total.

## API surface (planned)

| Method | Route | Caller |
| --- | --- | --- |
| CRUD | `/api/countries`, `/api/cities` | UI / external |
| CRUD | `/api/warehouses` | UI / external |
| CRUD | `/api/products` | UI / external |
| GET | `/api/warehouses/{id}/inventory` | UI / external |
| GET | `/api/inventory?warehouseId={id}` | UI / external |
| GET | `/api/inventory/{warehouseId}/{productId}` | UI / external |
| POST | `/api/inventory/{warehouseId}/{productId}/adjust` | UI / external |
| PATCH | `/api/inventory/{warehouseId}/{productId}/reorder-level` | UI / external |
| POST | `/api/inventory/updates` | External systems (API key) |
| GET | `/api/stock-movements?warehouseId={id}` | UI / external |

The Angular services in `src/app/core/services` already match these routes.
Swagger is enabled in Development at `/swagger`.

## Frontend configuration

- `src/environments/environment.ts` — production, `apiBaseUrl: '/api'`
- `src/environments/environment.development.ts` — development, `apiBaseUrl: '/api'`
- `proxy.conf.json` — dev server forwards `/api` to `https://localhost:7157`, so no
  CORS configuration is required while developing.

## Localization

English (`en`) and Arabic (`ar`) are supported at runtime:

- `public/i18n/en.json` and `public/i18n/ar.json` hold every static title, label,
  button, placeholder, validation message and empty-state text. Both files must
  keep the same key structure; `en.json` is the source of truth.
- `app.config.ts` configures `provideTranslateService` with the HTTP loader
  (`./i18n/`) and runs `LanguageService.initialize()` before bootstrap, so the
  first paint is already translated.
- `LanguageService` stores the choice in `localStorage`
  (`happy-inventory.language`), falls back to the browser language, and updates
  `<html lang>` / `<html dir>` (`rtl` for Arabic).
- Route `title` values are translation keys (`titles.*`); `LocalizedTitleStrategy`
  resolves them and re-applies the browser-tab title on language change.
- `styles.scss` contains RTL overrides (alignment, Arabic font stack, removed
  letter-spacing/uppercase on Arabic labels).
- Error messages are translated in `apiErrorInterceptor`. It resolves
  `TranslateService` lazily through `Injector` because the translation loader
  itself uses `HttpClient`, which would otherwise create a circular dependency.

To add a string, add the key to both JSON files and use
`{{ 'namespace.key' | translate }}` in templates, or
`translate.instant('namespace.key', params)` in component code.

## Database

```
dotnet ef migrations add InitialCreate -p happyInventory
dotnet ef database update -p happyInventory
```

## Running

```
dotnet run --project happyInventory
cd happyInventory.Client && npm start
```
