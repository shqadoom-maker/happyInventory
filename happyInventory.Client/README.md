# Happy Inventory — Angular client

Angular 22 frontend for the Happy Inventory Web API. See the repository
[README](../README.md) and [architecture notes](../docs/ARCHITECTURE.md).

## Scripts

```sh
npm install       # once
npm start         # dev server on http://localhost:4200
npm run build     # production bundle in dist/
npm test          # unit tests (Vitest)
```

The development server proxies `/api` to `https://localhost:7157`, configured in
`proxy.conf.json`, so the ASP.NET Core API must be running for data to load.

## Layout

```
src/app/
├─ core/
│  ├─ interceptors/   HTTP error normalisation (ApiError)
│  ├─ models/         API contracts mirrored from the backend DTOs
│  └─ services/       One typed HttpClient service per resource
├─ features/          Lazy-loaded screens (dashboard, warehouses, products,
│                     inventory, stock movements, locations)
├─ app.routes.ts      Top-level routes
├─ app.config.ts      Router + HttpClient providers
└─ styles.scss        Shared design tokens and UI classes
```

Add a new screen by creating a folder under `features/`, exporting a `Routes`
constant, and registering it lazily in `app.routes.ts`.
