# Happy Inventory — Solution Structure

Warehouse inventory management for Happy Company. Warehouses live in different
countries and cities; stock is tracked per warehouse and can be updated by
internal users (Angular UI) or by external systems (eCommerce, ERP, etc.).

## Technology

| Layer | Choice |
| --- | --- |
| Backend | ASP.NET Core .NET 6 Web API, N-tier |
| Frontend | Angular 22 (`happyInventory.Client`, Angular CLI 22.2.4) |
| Database | SQLite (`HappyInventory.Api/happyInventory.db`) |
| ORM | EF Core 6 with migrations |
| Authentication | JWT Bearer (HS256), PBKDF2-SHA256 password hashing |
| Localization | `@ngx-translate/core` + `@ngx-translate/http-loader` |

## Projects and dependency direction

```
happyInventory/
├─ happyInventory.slnx                 Solution (N-tier projects only)
├─ docs/                               Design notes
├─ HappyInventory.Core/                Domain layer
│  ├─ Entities/                        User, Country, City, Warehouse, WarehouseItem
│  └─ Enums/                           UserRoles
├─ HappyInventory.Application/         Application layer
│  ├─ Common/                          JwtOptions, AdminPolicyOptions, PagedResult,
│  │                                   ConflictException
│  ├─ DTOs/                            Auth, Users, Countries, Cities, Warehouses,
│  │                                   WarehouseItems, Dashboard, Integrations
│  ├─ Interfaces/Repositories/         Persistence contracts
│  ├─ Interfaces/Services/             Use-case contracts
│  ├─ Models/                          UserAuthState, DashboardSnapshot
│  ├─ Services/                        User, country, city, warehouse, item,
│  │                                   dashboard and inventory-update use cases
│  └─ DependencyInjection.cs           AddApplication()
├─ HappyInventory.Infrastructure/      Infrastructure layer
│  ├─ Data/                            AppDbContext, DbSeeder, configurations
│  ├─ Migrations/                      EF Core migrations
│  ├─ Repositories/                    EF Core repository implementations
│  ├─ Security/                        PasswordHasher, TokenService
│  └─ DependencyInjection.cs           AddInfrastructure()
├─ HappyInventory.Api/                 Presentation/composition layer
│  ├─ Controllers/                     Auth, Users, Dashboard, Countries, Cities,
│  │                                   Warehouses, WarehouseItems, InventoryUpdates
│  ├─ Middleware/                      ExceptionHandlingMiddleware
│  ├─ Properties/launchSettings.json   Local ports and Swagger launch profile
│  ├─ Program.cs                       DI composition, auth, CORS, Swagger, startup seed
│  └─ appsettings.json                 Connection string, JWT, seed credentials
├─ HappyInventory.Tests/               Unit tests for the integration use case
├─ happyInventory.Client/              Angular application
├─ happyInventory/                     Legacy monolith, not in the solution
└─ docs/ARCHITECTURE.md                This document
```

The project references enforce the layering:

- `HappyInventory.Core` has no project references.
- `HappyInventory.Application` references `HappyInventory.Core`.
- `HappyInventory.Infrastructure` references `HappyInventory.Application` and
  `HappyInventory.Core`.
- `HappyInventory.Api` references `HappyInventory.Application`,
  `HappyInventory.Infrastructure` and `HappyInventory.Core`.
- `HappyInventory.Tests` references `HappyInventory.Application` and
  `HappyInventory.Core`.

Application code never references Infrastructure. The API is the composition
root: it calls `AddApplication()` and `AddInfrastructure()`, then injects the
application service interfaces into controllers.

Request flow:

```
HTTP
  → Controller (HappyInventory.Api)
  → I...Service (HappyInventory.Application)
  → I...Repository (HappyInventory.Application)
  → Repository implementation (HappyInventory.Infrastructure)
  → EF Core / SQLite
```

DTOs cross the HTTP boundary. Controllers never access `AppDbContext` and never
return database entities. Domain entities stay inside Application and
Infrastructure. Application exceptions are translated to HTTP problem responses by
`ExceptionHandlingMiddleware`.

## Security model

### Authentication

- `POST /api/auth/login` is the only `[AllowAnonymous]` endpoint.
- Credentials are checked against the active user with a normalized username and
  a PBKDF2-SHA256 hash (100,000 iterations, per-password salt).
- A successful login returns an HS256 JWT with `NameIdentifier`, `Name` and
  `Role` claims, plus the user profile and expiry.
- JWT issuer, audience, signing key and lifetime come from `Jwt` configuration.
  The key must be at least 32 bytes; validation allows 30 seconds of clock skew.
- `JwtBearerEvents.OnTokenValidated` re-reads the user through
  `IUserRepository` on every request. Deleted or inactive users are rejected, and
  the role claim is replaced with the current database role so promotions and
  demotions apply immediately.

### Authorization

- A fallback policy requires an authenticated user for every endpoint that does
  not opt out.
- `/api/users` uses the `Admin` role (`AdminOnly` policy / `[Authorize(Roles=...)]`).
- Each user has exactly one role, `Admin`, `Management` or `Auditor`, stored as a
  string.
- Administrator accounts cannot be deleted. Administrators cannot deactivate,
  demote or delete their own account. The default administrator cannot be
  renamed, demoted, deactivated or deleted. The UI also hides the delete action
  for administrator accounts and for the current user.
- Inactive accounts are rejected at login and on every authenticated request.
  The login page maps the `account_disabled` error code to a localized message.
- The inbound eCommerce endpoint uses the same JWT fallback policy. External
  systems need a bearer token; no separate API-key scheme was introduced.

### Default account

The database is migrated and seeded on startup from `Seed` configuration:

| Email | Password | Role | Full name |
| --- | --- | --- | --- |
| `admin@happywarehouse.com` | `P@ssw0rd` | `Admin` | System Administrator |

On a fresh database this is the first account. On an existing database the
seeder migrates the legacy seeded `admin` account to the new email and resets its
password once, and maps any legacy `User` role to `Auditor`. If neither account
exists, it creates the default administrator. There is no public registration.
All other accounts are created by an administrator. Change the seed password and
JWT key for any real deployment.

## Implemented API surface

| Method | Route | Caller |
| --- | --- | --- |
| POST | `/api/auth/login` | Anonymous |
| GET | `/api/auth/me` | Authenticated |
| GET | `/api/dashboard` | Authenticated |
| GET | `/api/users` | Admin |
| POST | `/api/users` | Admin |
| GET | `/api/users/{id}` | Admin |
| PUT | `/api/users/{id}` | Admin |
| POST | `/api/users/{id}/change-password` | Admin |
| DELETE | `/api/users/{id}` | Admin |
| CRUD | `/api/countries`, `/api/cities` | Authenticated |
| CRUD | `/api/warehouses` | Authenticated |
| GET | `/api/warehouses/{id}/items` | Authenticated |
| CRUD | `/api/warehouse-items` | Authenticated |
| POST | `/api/inventory/updates` | Authenticated (eCommerce/ERP) |

`PUT /api/users/{id}` accepts `password: null` to keep the current password.
`POST /api/users/{id}/change-password` accepts `{ "newPassword": "..." }`.
Swagger is enabled in Development at `/swagger` and includes a Bearer scheme.

## Warehouse domain

| Entity | Status | Purpose |
| --- | --- | --- |
| `Country` | Implemented | Country where a warehouse is located |
| `City` | Implemented | City inside a country |
| `Warehouse` | Implemented | Physical warehouse with a unique name, address, city and country |
| `WarehouseItem` | Implemented | Item stored in a warehouse: name unique per warehouse, optional SKU, integer quantity, cost and optional MSRP |
| `StockMovement` | Planned | Audit trail of every quantity change, with source and reference |

There is no `Product` or `Inventory` entity in the solution. The existing
`WarehouseItem` is the stock record used by the warehouse UI and by the
eCommerce integration. Warehouse and warehouse-item lists are paged at 10 rows.
The dashboard aggregates item names across warehouses case-insensitively and
shows the warehouse status, top 10 and bottom 10 items by available quantity.

The Angular service for the planned stock-movement resource already uses the
following route, but the API controller and entity are not implemented yet:

| Method | Route | Caller |
| --- | --- | --- |
| GET | `/api/stock-movements?warehouseId={id}` | UI / external |

## eCommerce integration contract

The system diagram defines an online eCommerce system that calls the Happy
Inventory API over JSON/HTTP to update inventories and deduct warehouse stock.
The documented inbound route is:

```text
POST /api/inventory/updates
```

The endpoint lives in `HappyInventory.Api/Controllers/InventoryUpdatesController.cs`.
It delegates to `IInventoryUpdateService` in the Application layer, which uses
`IInventoryUpdateRepository` to read the stock record and persist both the
deduction and its idempotency record. The EF Core implementation is
`HappyInventory.Infrastructure/Repositories/InventoryUpdateRepository.cs`.

Required header:

```text
Idempotency-Key: order-12345
```

Request DTO (`InventoryUpdateRequest`):

```json
{
  "warehouseId": 1,
  "skuCode": "SKU-123",
  "quantity": 2
}
```

Response DTO (`InventoryUpdateResponse`, `200 OK`):

```json
{
  "warehouseItemId": 9,
  "warehouseId": 1,
  "skuCode": "SKU-123",
  "itemName": "Widget",
  "previousQuantity": 10,
  "remainingQuantity": 8
}
```

Rules:

- `Idempotency-Key` is required, must be 1-100 characters, and must be unique per
  order line.
- `warehouseId` must be positive.
- `skuCode` is required and matched case-insensitively inside the warehouse.
- `quantity` must be positive and is the amount deducted.
- No match returns `404`.
- More than one match returns `409` because the update would be ambiguous.
- A deduction larger than the available quantity returns `409`.
- A successful deduction is saved and returns the previous and remaining
  quantities.

### Atomicity and idempotency

`WarehouseItem.Qty` carries `[ConcurrencyCheck]`. EF Core therefore generates an
update whose `WHERE` clause includes the quantity that was read. If another
request changes the row first, the update affects zero rows and
`SaveChangesAsync` throws `DbUpdateConcurrencyException`; the repository maps it
to `ConflictException` (`409`). Two customers therefore cannot both buy the last
item.

The `InventoryUpdateRecords` table stores the idempotency key, the request
fingerprint (`WarehouseId`, `SKUCode`, `Quantity`) and the response values. The
stock change and the new record are added to the same `AppDbContext` and saved by
one `SaveChangesAsync` call, so EF Core commits both in one transaction. If the
stock update fails, the idempotency record is not persisted; if the record insert
fails (for example, a concurrent request used the same key), the stock change is
rolled back.

On retry, the service looks up the key before touching stock:

- same key and same body: returns the stored previous/remaining quantities with
  `200` and does not deduct again;
- same key and different body: throws `ConflictException` (`409`);
- missing key at the API boundary: `400`.

The migration that adds the table is
`20261025155519_AddInventoryUpdateIdempotency`.

HTTP error mapping is centralized in `ExceptionHandlingMiddleware`:

| Exception | Status |
| --- | --- |
| `InvalidOperationException` | `400 Bad Request` |
| `KeyNotFoundException` | `404 Not Found` |
| `ConflictException` | `409 Conflict` |
| Unexpected exception | `500 Internal Server Error` |

The endpoint is authenticated by the existing JWT fallback policy. No separate
integration API key or configuration section exists. The integration does not
create a complete eCommerce application, does not add Product/Inventory entities,
and does not write a stock-movement audit row because that module remains
unimplemented. Order creation and outbound eCommerce calls remain the eCommerce
system's responsibility; this endpoint only applies the inventory deduction
idempotently.

## Frontend structure and routing

- `/login` is the first route and is wrapped in `guestGuard`; a signed-in user is
  redirected away from it.
- All other routes are children of the protected `Shell` component
  (`authGuard`), which renders the language-aware navigation drawer (left in
  English, right in Arabic), top bar, signed-in user and language switcher.
- `/users` has its own lazy route tree and an additional `adminGuard`; the
  navigation drawer only shows the Users item for admins. The list shows role and
  active status and offers Edit, Change Password and Delete; Delete is hidden for
  administrator accounts and for the signed-in user.
- The Dashboard loads `/api/dashboard` and renders summary cards plus
  dependency-free CSS bar charts for warehouse status and top/bottom items.
- `App` is a thin host containing only `<router-outlet />`.
- `AuthService` exposes the session as signals, persists the JWT in
  `localStorage`, restores it until expiry, and logs out on demand.
- `authInterceptor` adds the bearer token to `/api` calls. A `401` (except from
  login) clears the session and redirects to `/login` with a `returnUrl`.
- The login page maps the API's `account_disabled` error code to a localized
  message that tells the user to contact support.
- `apiErrorInterceptor` normalizes HTTP failures into a translated `ApiError`.
  Both interceptors resolve their dependencies lazily where needed: `Router` in
  `authInterceptor` and `TranslateService` in `apiErrorInterceptor`. This avoids
  a circular dependency with the translation loader and initial navigation.

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
  letter-spacing/uppercase on Arabic labels). Component layouts use logical
  properties such as `inset-inline-end` so they mirror correctly.

To add a string, add the key to both JSON files and use
`{{ 'namespace.key' | translate }}` in templates, or
`translate.instant('namespace.key', params)` in component code.

## Database

The connection string defaults to `Data Source=happyInventory.db`, relative to the
API process working directory. When the API is started from
`HappyInventory.Api`, the file is `HappyInventory.Api/happyInventory.db`. The file
is git-ignored (`*.db`).

The migrations were moved to `HappyInventory.Infrastructure/Migrations` with the
same migration IDs, so an existing database keeps its migration history.

```sh
dotnet ef migrations add <Name> -p HappyInventory.Infrastructure -s HappyInventory.Api
dotnet ef database update -p HappyInventory.Infrastructure -s HappyInventory.Api
```

`DbSeeder.SeedAsync` runs pending migrations and ensures the default administrator
exists. On a fresh database it creates `admin@happywarehouse.com`; on an existing
database it migrates the legacy seeded `admin` account and maps legacy `User`
roles to `Auditor`. Startup fails if the configured JWT key is missing or shorter
than 32 bytes.

## Running

```sh
dotnet run --project HappyInventory.Api
cd happyInventory.Client && npm start
```

Backend build and tests:

```sh
dotnet build happyInventory.slnx
dotnet test HappyInventory.Tests/HappyInventory.Tests.csproj
```

Angular production build/tests:

```sh
cd happyInventory.Client && npm run build && npm test -- --watch=false
```

The `happyInventory/` folder is the old monolith. It is intentionally excluded
from `happyInventory.slnx`; use `HappyInventory.Api` instead.
