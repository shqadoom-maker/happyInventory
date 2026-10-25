# Happy Inventory

Warehouse inventory management for Happy Company, built with ASP.NET Core .NET 6
Web API (N-tier), Angular 22, EF Core 6 and SQLite.

## What is implemented

- JWT authentication with a login-first Angular layout.
- Role-based users management (`Admin`, `Management`, `Auditor`), managed inside
  the app (there is no public registration).
- Dashboard with summary cards, warehouse inventory counts, and top/bottom item
  charts.
- Countries, cities, warehouses and warehouse-item APIs with their Angular
  screens.
- English and Arabic UI, including runtime language switching and RTL.
- Protected Angular shell with Dashboard, Warehouses, Stock movements and
  Locations screens, plus Users for administrators.
- Swagger documentation with a Bearer token input.
- Inbound eCommerce/ERP integration at `POST /api/inventory/updates`, which
  deducts stock from the existing warehouse-item record selected by warehouse and
  SKU.

The stock-movement screen is scaffolded on the client, but its API endpoint is
not implemented yet. Authentication, users, dashboard, locations, warehouses and
warehouse items are fully functional.

The Products and Inventory modules are not part of this solution. The integration
endpoint uses the existing `WarehouseItem` stock record and does not reintroduce
those modules or add a new inventory data model.

## Projects

- `HappyInventory.Core/` — entities and shared enums; no project references.
- `HappyInventory.Application/` — DTOs, service and repository interfaces, and use
  cases. It depends only on Core.
- `HappyInventory.Infrastructure/` — EF Core `AppDbContext`, configurations,
  migrations, repositories, password hashing and JWT creation. It depends on
  Application and Core.
- `HappyInventory.Api/` — controllers, middleware, authentication/DI composition,
  Swagger and configuration. It depends on Application, Infrastructure and Core.
- `HappyInventory.Tests/` — unit tests for the eCommerce inventory-update use case.
- `happyInventory.Client/` — Angular frontend (standalone components, signals).
- `happyInventory/` — legacy monolith, kept for reference only. It is not part of
  `happyInventory.slnx` and is not the application to run.
- `docs/ARCHITECTURE.md` — structure, security model, domain model and API
  contract.

## Getting started

Backend:

```sh
dotnet run --project HappyInventory.Api
```

The API listens on `https://localhost:7157` and `http://localhost:5254`.
Swagger is available at `https://localhost:7157/swagger`.

The database (`HappyInventory.Api/happyInventory.db`) is created and migrated on
startup. The existing development database was copied there so its migration
history and seed data carry over. The default administrator is seeded from
`appsettings.json`:

| Email | Password |
| --- | --- |
| `admin@happywarehouse.com` | `P@ssw0rd` |

The seeder ensures the default administrator account exists. On a fresh database
it is the first account. On an existing database it migrates the legacy seeded
`admin` account to the new email and resets its password once; legacy `User`
roles are mapped to `Auditor`. Change the seeded credentials and the JWT signing
key before deploying.

Frontend:

```sh
cd happyInventory.Client
npm install
npm start
```

The dev server runs at `http://localhost:4200` and proxies `/api` to
`https://localhost:7157` (see `happyInventory.Client/proxy.conf.json`).

The API targets `net6.0`; the installed 6.x SDK is used automatically for
`net6.0` projects.

## eCommerce integration

The system diagram defines an online eCommerce system that calls the Happy
Inventory API over JSON/HTTP to update inventories and deduct warehouse stock.
The documented route for that inbound update is:

```text
POST /api/inventory/updates
```

The endpoint is protected by the existing JWT fallback policy. An external system
must sign in through `POST /api/auth/login` and send
`Authorization: Bearer <token>`. No separate API-key or integration configuration
was introduced.

Required header:

```text
Idempotency-Key: order-12345
```

Request body:

```json
{
  "warehouseId": 1,
  "skuCode": "SKU-123",
  "quantity": 2
}
```

`quantity` is the amount to deduct. `skuCode` is matched case-insensitively
inside the selected warehouse.

The `Idempotency-Key` must be unique per order line and is limited to 100
characters. Sending the same key with the same body replays the stored `200`
response without deducting stock again. Reusing the key with a different body
returns `409 Conflict`. A missing or empty header returns `400 Bad Request`.

Successful response (`200 OK`):

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

Behavior and status codes:

| Status | Meaning |
| --- | --- |
| `200` | Stock was deducted and saved, or an existing idempotent result was replayed. |
| `400` | The request is missing a warehouse, SKU, positive quantity or `Idempotency-Key`. |
| `401` | No valid bearer token was supplied. |
| `404` | No warehouse item matches the warehouse and SKU. |
| `409` | Stock is insufficient, multiple items match the SKU, the key was reused with a different body, or another request changed the stock first. Retry with the same key after a transient conflict. |

The stock deduction and its idempotency record are saved in one EF Core
transaction. `WarehouseItem.Qty` is a concurrency token, so two customers cannot
both buy the last item: the second write fails and returns `409` instead of
overselling. Order creation remains the eCommerce system's responsibility; this
endpoint only applies the inventory deduction.

The endpoint updates `WarehouseItem.Qty` and writes an `InventoryUpdateRecord`.
It does not create Product or Inventory entities, and it does not write a
stock-movement audit row because that module remains unimplemented.

## Authentication and roles

- `POST /api/auth/login` is the only anonymous endpoint. It returns a JWT and the
  signed-in user profile.
- Every other endpoint requires `Authorization: Bearer <token>`. The API uses a
  fallback authorization policy, so new controllers are closed by default.
- Tokens are validated on every request, and the user is re-checked in the
  database. Deleted or deactivated users are rejected immediately; role changes
  take effect without waiting for the token to expire.
- Each user has exactly one role: `Admin`, `Management` or `Auditor`. Only
  administrators can list, create, edit, change the password of, or delete users.
- Administrator accounts cannot be deleted. Administrators cannot delete,
  deactivate or demote their own account. The default administrator cannot be
  renamed, demoted, deactivated or deleted.
- Inactive accounts cannot sign in. The login page shows a localized
  "account disabled, contact support" message.

JWT settings live in `HappyInventory.Api/appsettings.json` (`Jwt:Issuer`,
`Jwt:Audience`, `Jwt:Key`, `Jwt:ExpiryMinutes`). Seed credentials live under
`Seed`.

## API endpoints

| Method | Route | Access |
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

On edit, send `"password": null` (or omit it) to keep the current password.
`POST /api/users/{id}/change-password` accepts `{ "newPassword": "..." }` and
requires a password of at least 8 characters.

## Frontend behaviour

- The login page is the first route. Visiting a protected route while signed out
  redirects to `/login` and returns to the requested page after sign-in.
- The JWT is stored in `localStorage` and restored on reload until it expires.
- A `401` response clears the session and redirects to login.
- The navigation menu hides the Users entry for non-admins, and `/users` is
  additionally protected by an admin route guard.
- The Dashboard is the welcome page and shows warehouse status and top/bottom
  item charts, with loading, empty and error states.
- The Users list shows each account's role and status and offers Edit, Change
  Password and Delete. Delete is hidden for administrator accounts and for the
  signed-in user.
- The UI is available in English and Arabic with full RTL support.

## Languages

The frontend is available in English and Arabic. Static titles and texts live in
`happyInventory.Client/public/i18n/en.json` and `ar.json`; the app uses
`@ngx-translate/core` to switch at runtime. The switcher stores the choice in
`localStorage` (`happy-inventory.language`) and Arabic switches the document to
RTL. Both translation files must keep the same key structure; `en.json` is the
source of truth.
