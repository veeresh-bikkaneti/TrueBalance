---
checked:
  OfflineCPA/src/Backend/Program.cs: 742bc67ef2e99c040cdbdaba2b50a1fcda30bf82
  OfflineCPA/src/Backend/Configuration/DotEnv.cs: 7592be1aa2f9464e5fca5199c849a4b5f852aa25
  OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs: 98f6f27b02c94a2298b2860b84e40a66d8f1b56c
  OfflineCPA/src/Application/Networking/INetworkGate.cs: 77fab7a5d681fe7d25f236857a96c4ab8fe25914
  OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs: e57f61396acc5f925a3b7e45d170ed977a741596
  OfflineCPA/docker-compose.yml: bf787f349e19e2ea266c10a9dc5f17cf1f114962
  OfflineCPA/docker-compose.dev.yml: 85710bca8c67616598ad95d352f47c551a33d1e1
  OfflineCPA/src/Backend/Endpoints/IEndpointModule.cs: 64ea0d433d717f99fda6bfc21be80e8cb85dd809
  OfflineCPA/src/Backend/Endpoints/EndpointModuleRegistration.cs: 09be4350fefe20e05976b9d3535c01b3978e9306
  OfflineCPA/src/Backend/Endpoints/BookEndpoints.cs: a6197c0c20fbe76149e530d6bb394b98e8c8c09e
  OfflineCPA/src/Backend/Endpoints/ReportEndpoints.cs: 90b69c444582c96b837c351fff8483f890301d0c
---
# Backend host

`Program.cs` is the composition root. `DotEnv.Load()` runs first and sets variables from `.env` only when the process does not already have them (repo: OfflineCPA/src/Backend/Program.cs#L16) (repo: OfflineCPA/src/Backend/Configuration/DotEnv.cs#L3-L33). A missing `ConnectionStrings:DefaultConnection` throws before the host is built. The message names no password (repo: OfflineCPA/src/Backend/Program.cs#L31-L39).

## Wiring

The host registers adapters by convention: `AddInfrastructureAdapterModules` finds one `IAdapterModule` per Infrastructure folder, so adding an adapter needs no core edit (repo: OfflineCPA/src/Backend/Program.cs#L44-L47) (repo: OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs#L8-L22). On top of the adapters, the host registers the folder watcher, its five ledger use cases, the statement import use cases, upload options, and no `IAiAssist` (repo: OfflineCPA/src/Backend/Program.cs#L50-L84). The gate and its ports are on [[network-gate]] (repo: OfflineCPA/src/Application/Networking/INetworkGate.cs#L10-L36). CORS allows `http://localhost:3000` and `http://localhost:5173` (repo: OfflineCPA/src/Backend/Program.cs#L86-L95). `UseSecureErrorHandling` turns on the exception handler (repo: OfflineCPA/src/Backend/Extensions/SecureErrorHandling.cs#L11-L14). Logging clears the default providers and wraps the console one in the masking redaction provider, and `PiiOptions` binds `Pii:FingerprintKey` and anchors the key file to the app content root (repo: OfflineCPA/src/Backend/Program.cs#L24-L29) (repo: OfflineCPA/src/Backend/Program.cs#L63-L72). The upload route moved to `UploadEndpoints`; see [[pii-masking]] and [[uploads]]. Swagger is only in Development (repo: OfflineCPA/src/Backend/Program.cs#L101-L105).

## Routes

A new route adds itself: write a class in `Backend/Endpoints` that implements `IEndpointModule`, register anything only it needs in `AddServices` (it may do nothing), and map it in `Map` (repo: OfflineCPA/src/Backend/Endpoints/IEndpointModule.cs#L7-L13). The class needs a public parameterless constructor. `AddEndpointModules` and `MapEndpointModules` find every non-abstract module in the Backend assembly, ordered by full name (repo: OfflineCPA/src/Backend/Endpoints/EndpointModuleRegistration.cs#L8-L35), and `Program.cs` calls each once (repo: OfflineCPA/src/Backend/Program.cs#L49) (repo: OfflineCPA/src/Backend/Program.cs#L114). `BookEndpoints` holds `POST /api/books/setup` (repo: OfflineCPA/src/Backend/Endpoints/BookEndpoints.cs#L13-L31); `ReportEndpoints` holds `GET /api/transactions` and `GET /api/export` (repo: OfflineCPA/src/Backend/Endpoints/ReportEndpoints.cs#L13-L26). The upload, vault, ledger and import routes still use their own `Map*Endpoints` calls (repo: OfflineCPA/src/Backend/Program.cs#L111-L116).

## Compose

The default compose file builds the backend connection string with `Host=database` and passes the password as `PGPASSWORD`. It publishes the API on `127.0.0.1:8080` and does not publish a database port. The backend waits until Postgres is healthy (repo: OfflineCPA/docker-compose.yml#L20-L42). `docker-compose.dev.yml` publishes Postgres on `127.0.0.1:5432` for a host `dotnet run` (repo: OfflineCPA/docker-compose.dev.yml#L1-L6).

## Change it safely

A new adapter gets its own folder and module class. A new route gets its own endpoint module. Neither needs a `Program.cs` edit. Do not resolve `HttpClient` outside `INetworkGate`. Do not register a logger provider that skips [[pii-masking]]. Do not put a password literal in `Program.cs` or in compose. Keep the database off the default compose ports. See [[ledger]], [[uploads]], and [[data-and-migrations]].
