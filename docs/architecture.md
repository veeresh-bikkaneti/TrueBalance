# Architecture

Layers (Solid; arrows point at the only thing each layer may depend on):

```
Backend (HTTP host) ──▶ Application ──▶ Domain
Backend ──▶ Infrastructure ──▶ Application
Frontend (React shell) ──▶ Backend API (local only)
```

- **Domain**: entities and rules. No I/O, no framework references.
- **Application**: use cases and ports (`ILedgerStore`, `IStatementImporter`, `IDocumentVault`, `IReportExporter`, `INetworkGate`, `IAiAssist`). Depends only on Domain.
- **Infrastructure**: adapters behind the ports, found and registered by module scan. Only `Application.Networking` and `Infrastructure.Networking` may touch `System.Net.Http`/`System.Net.Sockets`; `LedgerArchitectureTests` enforces it.
- Every outbound HTTP call must come from an adapter via `INetworkGate`, which denies by default, logs each attempt to `LedgerAuditRecords`, and hands out a client locked to the one approved host. `Program.cs` registers no `IAiAssist`; any future one receives tokenized text from the PII layer, never raw values.

## How to add an adapter

1. Create a folder under `OfflineCPA/src/Infrastructure` and put the adapter class in it.
2. Add one module class in that folder implementing `IAdapterModule`; in its `Register`, bind your adapter to its Application port.
3. Restart the host. The module scan (`AddInfrastructureAdapterModules`) finds the module; no core edit is needed.
4. If the adapter needs the network, call `INetworkGate.TryOpenClientAsync` with its adapter name and host; the user enables it in settings, and it never builds its own `HttpClient`.
