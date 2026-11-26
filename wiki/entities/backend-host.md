---
checked:
  OfflineCPA/src/Backend/Program.cs: 631e0233ec079f91233b2c85b031567189a4e932
  OfflineCPA/src/Backend/Configuration/DotEnv.cs: 7592be1aa2f9464e5fca5199c849a4b5f852aa25
  OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs: e57f61396acc5f925a3b7e45d170ed977a741596
  OfflineCPA/docker-compose.yml: 0a79ce20514571bcf91d957214efe822bf930bdc
  OfflineCPA/docker-compose.dev.yml: 85710bca8c67616598ad95d352f47c551a33d1e1
---
# Backend host

`Program.cs` is the composition root. `DotEnv.Load()` runs first and sets variables from `.env` only when the process does not already have them (repo: OfflineCPA/src/Backend/Program.cs#L15) (repo: OfflineCPA/src/Backend/Configuration/DotEnv.cs#L3-L33). A missing `ConnectionStrings:DefaultConnection` throws before the host is built. The message names no password (repo: OfflineCPA/src/Backend/Program.cs#L23-L34).

## Wiring

The host registers the PDF parser, the folder watcher, the chart seeder, the clock, the ledger store and its five use cases, upload options, and `ISecureUploadStore` (repo: OfflineCPA/src/Backend/Program.cs#L36-L48). CORS allows `http://localhost:3000` and `http://localhost:5173` (repo: OfflineCPA/src/Backend/Program.cs#L50-L58). `UseSecureErrorHandling` turns on the exception handler (repo: OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs#L11-L14). Swagger is only in Development (repo: OfflineCPA/src/Backend/Program.cs#L63-L67).

## Compose

The default compose file builds the backend connection string with `Host=database` and passes the password as `PGPASSWORD`. It publishes the API on `127.0.0.1:8080` and does not publish a database port. The backend waits until Postgres is healthy (repo: OfflineCPA/docker-compose.yml#L20-L38). `docker-compose.dev.yml` publishes Postgres on `127.0.0.1:5432` for a host `dotnet run` (repo: OfflineCPA/docker-compose.dev.yml#L1-L6).

## Change it safely

A new port is registered next to the other `AddScoped` lines and called from a thin route. Do not put a password literal in `Program.cs` or in compose. Keep the database off the default compose ports. See [[ledger]], [[uploads]], and [[data-and-migrations]].
