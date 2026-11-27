---
checked:
  OfflineCPA/src/Backend/Program.cs: ff7bc791b2a78a74057f500567e8ffee7218f04a
  OfflineCPA/src/Backend/Configuration/DotEnv.cs: 7592be1aa2f9464e5fca5199c849a4b5f852aa25
  OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs: 98f6f27b02c94a2298b2860b84e40a66d8f1b56c
  OfflineCPA/src/Application/Networking/INetworkGate.cs: 77fab7a5d681fe7d25f236857a96c4ab8fe25914
  OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs: e57f61396acc5f925a3b7e45d170ed977a741596
  OfflineCPA/docker-compose.yml: 6439009f0b9afa1a6a9268de1ffef78da789df47
  OfflineCPA/docker-compose.dev.yml: 85710bca8c67616598ad95d352f47c551a33d1e1
---
# Backend host

`Program.cs` is the composition root. `DotEnv.Load()` runs first and sets variables from `.env` only when the process does not already have them (repo: OfflineCPA/src/Backend/Program.cs#L14) (repo: OfflineCPA/src/Backend/Configuration/DotEnv.cs#L3-L33). A missing `ConnectionStrings:DefaultConnection` throws before the host is built. The message names no password (repo: OfflineCPA/src/Backend/Program.cs#L26-L35).

## Wiring

The host registers adapters by convention: `AddInfrastructureAdapterModules` finds one `IAdapterModule` per Infrastructure folder, so adding an adapter needs no core edit (repo: OfflineCPA/src/Backend/Program.cs#L35-L48) (repo: OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs#L8-L22). On top of the adapters, the host registers the folder watcher, its five ledger use cases, the statement import use cases, upload options, and no `IAiAssist` (repo: OfflineCPA/src/Backend/Program.cs#L38-L48). The gate and its ports are on [[network-gate]] (repo: OfflineCPA/src/Application/Networking/INetworkGate.cs#L10-L36). CORS allows `http://localhost:3000` and `http://localhost:5173` (repo: OfflineCPA/src/Backend/Program.cs#L51-L64). `UseSecureErrorHandling` turns on the exception handler (repo: OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs#L11-L14). Logging clears the default providers and wraps the console one in the masking redaction provider, and `PiiOptions` binds `Pii:FingerprintKey` and anchors the key file to the app content root (repo: OfflineCPA/src/Backend/Program.cs#L24-L30) (repo: OfflineCPA/src/Backend/Program.cs#L60-L71). The upload route moved to `UploadEndpoints`; see [[pii-masking]] and [[uploads]]. Swagger is only in Development (repo: OfflineCPA/src/Backend/Program.cs#L78-L82).

## Compose

The default compose file builds the backend connection string with `Host=database` and passes the password as `PGPASSWORD`. It publishes the API on `127.0.0.1:8080` and does not publish a database port. The backend waits until Postgres is healthy (repo: OfflineCPA/docker-compose.yml#L20-L40). `docker-compose.dev.yml` publishes Postgres on `127.0.0.1:5432` for a host `dotnet run` (repo: OfflineCPA/docker-compose.dev.yml#L1-L6).

## Change it safely

A new adapter gets its own folder, module class, and thin route; the only host edit remains the route. Do not resolve `HttpClient` outside `INetworkGate`. Do not register a logger provider that skips [[pii-masking]]. Do not put a password literal in `Program.cs` or in compose. Keep the database off the default compose ports. See [[ledger]], [[uploads]], and [[data-and-migrations]].
