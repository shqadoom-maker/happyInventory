# BARIZA Market

BARIZA Market is a multi-vendor marketplace built with ASP.NET Core .NET 6
Web API (N-tier), Angular 22, EF Core 6 and SQLite.

## What is implemented

- JWT authentication with rotating refresh tokens in an HttpOnly cookie, a
  separate public marketplace layout at `/shop`, and a protected staff shell.
- Role-based users management (`Admin`, `Management`, `Auditor`), managed inside
  the app (there is no public registration).
- Public marketplace foundation with a guest entry point, public navigation,
  language switching and English/Arabic RTL support, plus the Phase 2 public
  catalog with server-side search, filtering, sorting, pagination, anonymous API
  endpoints, and the Angular catalog page. See `docs/MARKETPLACE.md`.
- Dashboard with summary cards, warehouse inventory counts, and top/bottom item
  charts.
- Countries, cities, warehouses and warehouse-item APIs with their Angular
  screens.
- English and Arabic UI, including runtime language switching and RTL.
- Protected Angular shell with Dashboard, Warehouses, Stock movements and
  Locations screens, plus Users and System logs for administrators.
- Swagger documentation with a Bearer token input.
- Inbound eCommerce/ERP integration at `POST /api/inventory/updates`, which
  deducts stock from the existing warehouse-item record selected by warehouse and
  SKU.
- Admin-only System logs viewer backed by Serilog and SQLite, with level, user,
  text and UTC date filters, paging, expandable structured details and retention
  cleanup.

The stock-movement screen is scaffolded on the client, but its API endpoint is
not implemented yet. Authentication, users, dashboard, locations, warehouses and
warehouse items are fully functional.

The Products and Inventory modules are not part of this solution. The integration
endpoint uses the existing `WarehouseItem` stock record and does not reintroduce
those modules or add a new inventory data model.

## Projects

- `HappyInventory.Core/` — entities and shared enums; no project references.
- `HappyInventory.Application/` — DTOs, service and repository interfaces, and use
  cases, including system-log queries. It depends only on Core.
- `HappyInventory.Infrastructure/` — EF Core `AppDbContext`, configurations,
  migrations, repositories (including `SystemLog` persistence), password hashing
  and JWT creation. It depends on Application and Core.
- `HappyInventory.Api/` — controllers, middleware, Serilog database logging,
  authentication/DI composition, Swagger and configuration. It depends on
  Application, Infrastructure and Core.
- `HappyInventory.Tests/` — unit and SQLite integration tests for the eCommerce
  inventory-update, security, dashboard and external CRUD behavior.
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
history and seed data carry over. The default administrator is seeded from the
`Seed` configuration:

| Email | Password |
| --- | --- |
| `admin@happywarehouse.com` | `Seed:AdminPassword` |

The seeder ensures the default administrator account exists. On a fresh database
it is the first account, so `Seed:AdminPassword` must be configured. If no
administrator exists and no password is configured, startup fails instead of
creating an account with an unknown generated password. The seeder never resets
the password of an existing account; legacy `User` roles are mapped to `Auditor`.
Change the seeded credentials and the JWT signing key before deploying.

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

## Tests

Run the backend automated test suite with:

```sh
dotnet test HappyInventory.Tests/HappyInventory.Tests.csproj
```

The dashboard test uses an in-memory SQLite database so it exercises the same
query translation path as the API.

## Deployment

GitHub Actions and Azure App Service deployment steps are documented in
[`docs/DEPLOY_AZURE.md`](docs/DEPLOY_AZURE.md). The deployment uses one Linux
App Service container that serves both the Angular client and the API, with the
SQLite database persisted under `/home`.

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

## System logs

All application logging is handled by Serilog. Events go to the console and a
daily rolling file under `logs/`, and, when `Logging:Database:Enabled` is true,
also to the `SystemLogs` table in SQLite. The database sink is asynchronous: a
bounded in-memory queue is drained by a hosted background service, so request
threads never wait for a SQLite write. The writer resolves `ISystemLogRepository`
through a scope and stores the UTC timestamp, level, rendered message, message
template, exception, source context, user id/name, request method/path, status
code, trace id and additional structured properties.

Common secret-bearing property names (password, token, authorization, cookie,
API key, JWT, etc.) are redacted before persistence, and long values are
truncated. Logging failures are reported to standard error instead of being
logged through Serilog, so a failing writer cannot recurse or break a request.

The admin-only Log Viewer is available at `/system-logs` and in the side menu for
administrators. It supports:

- level, user, free-text and UTC date-range filters;
- 25-row pages;
- expandable details with exception text, message template, trace id and
  structured properties;
- cleanup of entries older than 7, 30, 90, 180 or 365 days, behind a
  confirmation prompt. There is no delete-all action.

`GET /api/system-logs` returns newest-first entries. Page size is clamped to
1-100 (default 25); levels are `Verbose`, `Debug`, `Information`, `Warning`,
`Error` and `Fatal`; `fromUtc` and `toUtc` are inclusive. The API normalizes
timestamps to UTC and serializes them with a `Z` suffix. The UI renders times in
UTC so server and client time zones cannot disagree.

Database logging is configured under `Logging:Database`:

| Key | Default | Purpose |
| --- | --- | --- |
| `Enabled` | `true` | Enables the SQLite sink and writer/retention services. |
| `MinimumLevel` | `Information` | Minimum level stored in SQLite; console and file logging keep Serilog's global level. |
| `QueueCapacity` | `10000` | Bounded queue size; oldest events are dropped when full rather than blocking requests. |
| `RetentionDays` | `30` | Automatic cleanup age; `0` disables automatic pruning. |

`SystemLogQueue` exposes `Capacity`, `QueuedCount` and `DroppedCount` for
monitoring. When the queue is full it drops the oldest entry, increments
`DroppedCount`, and writes a throttled warning to standard error. The warning
does not use Serilog, so a full queue cannot recursively enqueue another event.

The retention service runs after a one-minute startup delay and then every six
hours. Administrators can trigger the same cleanup manually with
`DELETE /api/system-logs?olderThanDays=30`; the value must be between 1 and 3650
days. The migration that adds the table is `20261026174335_AddSystemLogs`.

The log table stores operational data, including user names, request paths and
stack traces, so both the API and the UI restrict the Log Viewer to `Admin`.

## Authentication and roles

- `POST /api/auth/login` returns an access JWT and the signed-in user profile.
  It also sets a rotating refresh token in an `HttpOnly`, `Secure`,
  `SameSite=Strict` cookie scoped to `/api/auth`.
- `POST /api/auth/refresh` rotates the refresh token and returns a new access
  token. The old token is revoked immediately; replaying an already-rotated
  token is rejected and revokes the user's other active refresh tokens.
- `POST /api/auth/logout` revokes the current refresh token and clears the
  cookie. Password changes and administrator password resets revoke every
  refresh token for the affected user.
- Every other endpoint requires `Authorization: Bearer <token>`. The API uses a
  fallback authorization policy, so new controllers are closed by default.
- Access tokens are validated on every request, and the user is re-checked in
  the database. Deleted or deactivated users are rejected immediately; role
  changes take effect without waiting for the token to expire.
- Each user has exactly one role: `Admin`, `Management` or `Auditor`. Only
  administrators can list, create, edit, change the password of, or delete users.
- Administrator accounts cannot be deleted. Administrators cannot delete,
  deactivate or demote their own account. The default administrator cannot be
  renamed, demoted, deactivated or deleted.
- Inactive accounts cannot sign in. The login page shows a localized
  "account disabled, contact support" message.

JWT settings live in `HappyInventory.Api/appsettings.json` (`Jwt:Issuer`,
`Jwt:Audience`, `Jwt:Key`, `Jwt:ExpiryMinutes`, `Jwt:RefreshTokenExpiryDays`).
Seed credentials live under `Seed`.

## Security configuration

### JWT and database configuration

`Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`, `Jwt:ExpiryMinutes`,
`Jwt:RefreshTokenExpiryDays`, and `ConnectionStrings:Default` are validated
during startup. `Jwt:Key` must be a random string of at least 32 bytes. It is
intentionally left empty in `appsettings.json` and must be provided through a
secure source.

**Local development (User Secrets):**

```bash
# From the HappyInventory.Api directory:
dotnet user-secrets set "Jwt:Key" "<random-32-byte-or-longer-string>"
dotnet user-secrets set "Seed:AdminPassword" "<secure-admin-password>"
```

Generate a strong key, for example:

```bash
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 32)"
```

**Production (environment variables or secret manager):**

```bash
export ConnectionStrings__Default="Data Source=/var/lib/happyinventory/happyInventory.db"
export Jwt__Issuer="happyInventory"
export Jwt__Audience="happyInventory.Client"
export Jwt__Key="<random-32-byte-or-longer-string>"
export Jwt__RefreshTokenExpiryDays="30"
export Seed__AdminPassword="<secure-admin-password>"
```

or configure them in your secret manager (Azure Key Vault, AWS Secrets Manager,
HashiCorp Vault, etc.) and map them to the `Jwt` and `Seed` configuration keys.
The application fails fast if the issuer, audience, key, expiry, or database
connection string is missing or invalid. The signing key is never logged.

### Trusted reverse proxy and rate limiting

Rate limiting uses `HttpContext.Connection.RemoteIpAddress`. The application
does not trust client-supplied `X-Real-IP` or `X-Client-Id` headers. If the API
runs behind a reverse proxy or load balancer, add only that proxy's address to
`ForwardedHeaders`:

```json
"ForwardedHeaders": {
  "KnownProxies": [ "10.0.0.10" ],
  "KnownNetworks": [ "10.0.0.0/8" ],
  "ForwardLimit": 1
}
```

`UseForwardedHeaders` runs before rate limiting and only accepts
`X-Forwarded-For`/`X-Forwarded-Proto` from a configured trusted peer. If the
lists are empty, forwarding headers are ignored and the direct connection IP is
used. This is fail-closed: behind an unconfigured proxy, all callers may share
the proxy's IP for rate limiting.

### Administrator seeding

The default administrator is created only when no matching user exists in the
database. The password is **never reset on subsequent startups**.

- If `Seed:AdminPassword` is provided, that password is hashed and stored.
- If no administrator exists and `Seed:AdminPassword` is not provided, startup
  fails with a clear configuration error. The application does not generate an
  unrecoverable password or print a password to logs.

New accounts and existing accounts can sign in and use the application normally.
On startup the seeder clears the legacy `MustChangePassword` flag for any
remaining records without changing their passwords or inventory data. The
optional self-service endpoint `POST /api/auth/change-password` still requires
the current password and returns a fresh `LoginResponse` token.

### Compromised credentials

The previous default password `P@ssw0rd` and JWT key were present in the source
repository. Treat them as compromised: rotate the JWT signing key and change any
existing administrator passwords immediately after deploying this update.

## API endpoints

| Method | Route | Access |
| --- | --- | --- |
| POST | `/api/auth/login` | Anonymous |
| POST | `/api/auth/refresh` | Anonymous (HttpOnly refresh cookie) |
| POST | `/api/auth/logout` | Anonymous (HttpOnly refresh cookie) |
| GET | `/api/auth/me` | Authenticated |
| GET | `/api/dashboard` | Authenticated |
| GET | `/api/users` | Admin |
| POST | `/api/users` | Admin |
| GET | `/api/users/{id}` | Admin |
| PUT | `/api/users/{id}` | Admin |
| POST | `/api/users/{id}/change-password` | Admin |
| POST | `/api/auth/change-password` | Authenticated (current user) |
| DELETE | `/api/users/{id}` | Admin |
| GET | `/api/system-logs` | Admin |
| GET | `/api/system-logs/{id}` | Admin |
| DELETE | `/api/system-logs?olderThanDays=30` | Admin |
| CRUD | `/api/countries`, `/api/cities` | Authenticated |
| CRUD | `/api/warehouses` | Authenticated |
| GET | `/api/warehouses/{id}/items` | Authenticated |
| CRUD | `/api/warehouse-items` | Authenticated |
| POST | `/api/inventory/updates` | Authenticated (eCommerce/ERP) |

On edit, send `"password": null` (or omit it) to keep the current password.
`POST /api/users/{id}/change-password` is the administrator reset endpoint and
accepts `{ "newPassword": "..." }`; the administrator does not need the user's
current password. The optional self-service endpoint
`POST /api/auth/change-password` accepts
`{ "currentPassword": "...", "newPassword": "..." }` and returns a new token.
Both require a new password of at least 8 characters.

### External API clients

The API is not tied to the Angular UI. An external eCommerce/ERP system, script
or service can use the same JSON/HTTP endpoints:

1. An administrator creates a user account; there is no public registration.
2. The external client calls `POST /api/auth/login` and receives a JWT.
3. It sends `Authorization: Bearer <token>` on subsequent requests.
4. It can use the authenticated CRUD routes for countries, cities, warehouses
   and warehouse items. The eCommerce stock deduction route is
   `POST /api/inventory/updates` and also requires `Idempotency-Key`.

CORS applies only to browser callers; server-to-server clients are not blocked
by the browser origin policy. Allowed origins are configured under
`Cors:AllowedOrigins`. Development defaults to `http://localhost:4200`; add the
production Angular origin through configuration, for example:

```bash
export Cors__AllowedOrigins__0="https://app.example.com"
```

When the list is empty, no cross-origin browser origin is allowed. This is the
correct setting when Angular is served from the same App Service as the API.
Allowed origins also receive `Access-Control-Allow-Credentials: true` so the
browser can send the refresh cookie. Keep the Angular app and API on the same
site when using the default `SameSite=Strict` cookie; a cross-site deployment
would need a deliberate `SameSite=None` plus CSRF protection.
The development launch profile binds to `localhost`. To accept calls from
another machine, start the API with an explicit binding such as
`--urls "https://0.0.0.0:7157"` and use a trusted certificate, or place it
behind a reverse proxy.

## Frontend behaviour

- The login page is the first route. Visiting a protected route while signed out
  redirects to `/login` and returns to the requested page after sign-in.
- The access JWT is stored in `localStorage` when "Remember me" is checked,
  otherwise in `sessionStorage` (the tab session only). The refresh token is
  never exposed to JavaScript; it lives in the API's HttpOnly cookie.
- On startup, an existing session calls `GET /api/auth/me` before route guards
  run, so profile and role changes are reflected without signing out. If the
  access token has expired, the app first calls `POST /api/auth/refresh` using
  the HttpOnly cookie.
- A `401` on a normal API request triggers one shared refresh request and then
  retries the original request. The session is cleared and the user is sent to
  login only when the refresh token is also rejected.
- A successful login goes straight to the dashboard (or the requested return URL).
  The optional `/change-password` page is available to signed-in users but is
  never forced.
- The navigation menu hides the Users and System logs entries for non-admins, and
  `/users` and `/system-logs` are additionally protected by an admin route guard.
- The Dashboard is the welcome page and shows warehouse status and top/bottom
  item charts, with loading, empty and error states.
- The Users list shows each account's role and status and offers Edit, Change
  Password and Delete. Delete is hidden for administrator accounts and for the
  signed-in user.
- The Log Viewer filters by level, user, free text and UTC date range, pages
  newest-first, expands structured details and prunes old entries after
  confirmation. Its timestamps are displayed in UTC.
- The UI is available in English and Arabic with full RTL support.

### Design system

The visual identity is defined once in `src/styles.scss` as CSS custom
properties plus shared component classes; feature styles only add layout.

- **Palette:** deep navy surfaces (`--navy-*`), blue primary actions
  (`--primary`, `--gradient-primary`) and cyan accents (`--accent`), with soft
  white/grey content surfaces and semantic success/warning/danger tokens.
- **Shape and elevation:** `--radius-sm|--radius|--radius-lg` and
  `--shadow-xs|--shadow-sm|--shadow|--shadow-lg`.
- **Components:** `.btn`, `.card`, `.field`, `.table`, `.badge`, `.alert`,
  `.stat` and `.toggle-*` are themed centrally, so every screen inherits the
  same controls.
- **Login page:** a full-screen vector warehouse scene under a navy overlay,
  with a centred light glass card, language switch, show/hide password,
  remember me, password-help panel and copyright. The scene is inline SVG, so
  no binary asset is required.
- **Typography:** Inter (Google Fonts) with a system-font fallback; Arabic
  switches to a local Arabic-friendly stack.

## Languages

The frontend is available in English and Arabic. Static titles and texts live in
`happyInventory.Client/public/i18n/en.json` and `ar.json`; the app uses
`@ngx-translate/core` to switch at runtime. The switcher stores the choice in
`localStorage` (`happy-inventory.language`) and Arabic switches the document to
RTL. Both translation files must keep the same key structure; `en.json` is the
source of truth.
