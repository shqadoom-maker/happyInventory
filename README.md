# Happy Inventory

Warehouse inventory management for Happy Company, built with ASP.NET Core .NET 6
Web API, Angular, EF Core 6 and SQLite.

- `happyInventory/` — Web API (CRUD + external integration endpoints, Swagger)
- `happyInventory.Client/` — Angular frontend (Angular CLI 22)
- `docs/ARCHITECTURE.md` — structure, domain model and planned API

## Getting started

Backend:

```sh
dotnet run --project happyInventory
```

Swagger opens at `https://localhost:7157/swagger`.

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

## Languages

The frontend is available in English and Arabic. Static titles and texts live in
`happyInventory.Client/public/i18n/en.json` and `ar.json`; the app uses
`@ngx-translate/core` to switch at runtime. The top-bar switcher stores the
choice in `localStorage` (`happy-inventory.language`) and Arabic switches the
document to RTL.
