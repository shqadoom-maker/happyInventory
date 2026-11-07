# HappyInventory Marketplace — Architecture and Phase Plan

This document describes the public marketplace structure that will be built on top
of the existing HappyInventory warehouse platform. It is intentionally additive:
the current admin application, authentication, and inventory APIs remain in place.

## Phase 1 — Marketplace foundation

Phase 1 introduces only the public shell and routing foundation:

- A separate `MarketplaceLayout` for public pages.
- A public home page at `/shop`.
- A root redirect that sends guests to `/shop` and signed-in staff to `/dashboard`.
- Public navigation, language switching, and a staff sign-in link.
- English and Arabic translations.
- No database schema changes and no public catalog API yet.

Phase 2 is now implemented. See the section below.

## Route map

### Phase 1

```text
/                       root redirect
                        guest -> /shop
                        signed-in staff -> /dashboard

/shop                   MarketplaceLayout
  ''                    Marketplace home

/login                  Existing staff login
/change-password        Existing password change

/dashboard              Existing protected admin shell
/warehouses             Existing protected admin shell
/stock-movements        Existing protected admin shell
/locations              Existing protected admin shell
/users                  Existing protected admin shell, Admin only
/system-logs            Existing protected admin shell, Admin only
```

### Future public routes

```text
/shop/products
/shop/products/:slug
/shop/categories
/shop/cart
/shop/checkout
/shop/account
```

### Future seller routes

```text
/seller
/seller/dashboard
/seller/products
/seller/orders
```

## Identity and roles

Current roles:

- `Admin`
- `Management`
- `Auditor`

Future marketplace roles:

- `Seller`
- `Customer`
- `Guest` (not an authenticated role; anonymous visitor)

Recommended identity model:

- Keep `User` as the shared identity/authentication record.
- Add `SellerProfile` and `CustomerProfile` for marketplace-specific data.
- Add Seller/Customer roles only when the corresponding phases are implemented.
- Before adding non-staff roles, apply a `StaffOnly` authorization policy to every
  internal controller. The current fallback policy only requires authentication,
  so customers/sellers would otherwise be able to call internal endpoints.

## Domain model

```text
Category 1---* Product
Product 1---* ProductImage
Product 1---* WarehouseItem
Warehouse 1---* WarehouseItem
SellerProfile 1---* Product
SellerProfile 1---* Warehouse
CustomerProfile 1---* Cart
Cart 1---* CartItem
CustomerProfile 1---* Order
Order 1---* SellerOrder
SellerOrder 1---* OrderItem
Order 1---* Payment
Payment 1---* PaymentTransaction
Product 1---* Review
```

Planned entities:

| Entity | Purpose |
| --- | --- |
| `Category` | Public product categories |
| `Product` | Catalog master data, seller ownership, publishing state |
| `ProductImage` | Product gallery images |
| `SellerProfile` | Store details and approval status |
| `CustomerProfile` | Customer-specific profile data |
| `WarehouseItem` | Stock per product and warehouse |
| `StockMovement` | Immutable stock-change ledger |
| `StockReservation` | Temporary stock reservation during checkout |
| `Cart` / `CartItem` | Server-side cart for authenticated customers |
| `Order` | Parent marketplace order |
| `SellerOrder` | Seller-specific fulfillment subset |
| `OrderItem` | Product snapshot, quantity, unit price, seller |
| `Payment` | Payment session and status |
| `PaymentTransaction` | Provider transaction details |
| `Review` | Verified-purchase rating and review |

## Public API conventions

Public catalog endpoints must be explicitly anonymous because the API uses a
fallback authorization policy that requires authentication by default.

Planned public endpoints:

```text
GET /api/marketplace/products
GET /api/marketplace/products/{slug}
GET /api/marketplace/categories
GET /api/marketplace/sellers/{slug}
```

Public queries must filter by:

- `Product.IsPublished`
- `Product.IsActive`
- `Product.ApprovalStatus == Approved`
- `SellerProfile.Status == Approved`

Public DTOs must never expose:

- `CostPrice`
- Internal warehouse identifiers or private stock details
- Seller private contact information
- Unpublished or unapproved products

## Phase 2 — Public catalog (implemented)

Phase 2 adds the anonymous public catalog without changing authentication or
existing inventory/admin behavior.

### Domain model

- `Category` with active state, sort order, and products.
- `SellerProfile` with approval/active state.
- `Product` with name, slug, SKU, description, price, optional discount price,
  image URL, category, optional seller, published/active flags, approval status,
  and timestamps.
- `ProductApprovalStatus` enum: Draft, PendingApproval, Approved, Rejected.
- Nullable `WarehouseItem.ProductId` optional FK for stock availability.
- EF price value converters store `Product.Price` and `DiscountPrice` as integer
  cents in SQLite while exposing decimals to the application.

### Database

Migration: `20261107192701_AddMarketplaceCatalog`

- Adds `Categories`, `SellerProfiles`, and `Products`.
- Adds nullable `WarehouseItems.ProductId`.
- Adds indexes for slug, SKU, public visibility, category, seller, and created date.
- Demo catalog data is seeded only when `ASPNETCORE_ENVIRONMENT=Development`
  and no products exist.

### Public API

- `GET /api/marketplace/products`
- `GET /api/marketplace/categories`
- Both endpoints are `[AllowAnonymous]`.

Query parameters:

```text
search, categoryId, minPrice, maxPrice, sortBy, pageNumber, pageSize
```

Sort values:

```text
newest, priceAsc, priceDesc, nameAsc
```

Default page size is 12; maximum page size is 60. Response shape:

```json
{
  "items": [],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 12,
  "totalPages": 0
}
```

Visibility rules:

- `Product.IsPublished`
- `Product.IsActive`
- `Product.ApprovalStatus == Approved`
- `Category.IsActive`
- Seller is null or approved and active

Availability is derived from linked `WarehouseItems.Any(Qty > 0)`. Out-of-stock
products remain visible but are marked unavailable. Effective price is
`DiscountPrice ?? Price`.

### Frontend

- Route: `/shop/products`
- Placeholder routes: `/shop/products/:slug` and `/shop/register`
- Marketplace header includes search, sign-in, register, disabled cart icon with
  badge `0`, and responsive navigation.
- Catalog page includes debounced search, categories, price range, sort, page
  size, pagination, loading, empty, no-results, and error states.
- Filters and pagination are synchronized through URL query parameters.
- English and Arabic translations are complete.

### Tests

Backend: `MarketplaceCatalogTests` (16 tests) covers public visibility, search,
category filter, effective-price filter, sorting, pagination, invalid input,
empty results, and categories.

Frontend: `marketplace-product-list.spec.ts` (9 tests) covers rendering, URL query
binding, debounced search, filters, pagination, validation, loading, empty,
no-results, and error/retry states. The marketplace layout spec also covers the
new header actions.

Seller endpoints must resolve the seller from the authenticated JWT identity.
The client must never be allowed to submit its own `SellerId`.

Rules:

- Resolve `SellerProfile` by `UserId`.
- Filter seller products and orders by the resolved seller ID.
- Admin overrides require an explicit admin policy.
- Add integration tests for cross-seller access attempts.

## Stock and checkout

The existing `InventoryUpdateService` remains the integration endpoint for
external inventory deductions. The marketplace checkout will need a stronger
stock model:

- Every stock change creates a `StockMovement`.
- Checkout reserves stock instead of immediately reducing it.
- Payment success commits the reservation.
- Cancellation or expiry releases the reservation.
- Use database transactions and concurrency tokens to prevent overselling.
- Never trust client-submitted prices or quantities.

## Payments

Payment work belongs to a later phase. When implemented:

- Use hosted checkout or provider tokenization.
- Never store full card data or CVV.
- Verify payment webhooks and reject replayed events.
- Use idempotency keys for order creation and payment callbacks.
- Treat the webhook as the source of truth, not the browser return URL.

## Database strategy

The current database is SQLite with EF Core 6. It is suitable for Phase 1 and the
current internal application. Before production orders and payments, plan a
migration to PostgreSQL or SQL Server for concurrency and decimal handling.

All marketplace schema changes must be additive migrations with backfill steps
and must not remove existing warehouse data.

## Phase boundaries

| Phase | Scope |
| --- | --- |
| 1 | Public layout, routing, home page, translations |
| 2 | Product catalog, categories, public APIs, admin product screens |
| 3 | Product details, images, seller information |
| 4 | Guest cart stored locally |
| 5 | Authentication integration, registration, cart merge |
| 6 | Seller registration, profiles, dashboards, admin approval |
| 7 | Checkout, orders, seller order management, inventory updates |
| 8 | Reviews, discounts, notifications, payments, reports |
