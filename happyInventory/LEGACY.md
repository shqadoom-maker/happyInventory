# Legacy monolith

This folder is the original single-project ASP.NET Core API. It is kept only for
reference and for the development SQLite database that was copied into
`HappyInventory.Api/happyInventory.db`.

It is **not** part of `happyInventory.slnx` and is **not** the application to run.
The canonical N-tier application is `HappyInventory.Api`:

```sh
dotnet run --project HappyInventory.Api
```

The eCommerce integration endpoint is implemented in the N-tier projects under
`HappyInventory.Api`, `HappyInventory.Application` and
`HappyInventory.Infrastructure`.
