---
checked:
  OfflineCPA/src/Backend/Endpoints/LedgerEndpoints.cs: 80a26042df5de3cd4363a4e97018b372a2b36ba6
  docs/adr/0011-local-modular-monolith.md: 4792ef60fdc033c952b3dc6d252ba9138881bb26
  OfflineCPA/src/Backend/Program.cs: 153e78f333ac9b25969edaef934534cbb64e786b
  OfflineCPA/src/Application/Networking/INetworkGate.cs: 77fab7a5d681fe7d25f236857a96c4ab8fe25914
  OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs: 9b776c957e947fa9159fe6cc21371dfa74ae1058
  OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs: 98f6f27b02c94a2298b2860b84e40a66d8f1b56c
  OfflineCPA/src/Application/Interfaces/IStatementImporter.cs: 26c011a1a3334924f6f8f2974c188b6ae09456a1
  OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs: 10ac66ed94a52da92f59d8a9355792bf36828413
  OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs: 068b242b1c9b1ab4ad4773cf925766b1c6e6c717
  OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs: 79af2ba8029ac2126a5a2367c395c30dc210b3ab
  OfflineCPA/src/Infrastructure/Migrations/20261124120000_InitialCreate.cs: eb9d9a85324ec58feec91a70cb926d375c48342b
  OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs: e9a88657b2bea914a76921239982eba4df689c19
  OfflineCPA/docker-compose.yml: 6439009f0b9afa1a6a9268de1ffef78da789df47
  docs/adr/0003-clean-architecture-ports-adapters.md: c9c7256eea1b992b1d2443ea30a1356ec0de5388
  OfflineCPA/frontend/src/App.tsx: 306f2244e219b1665f4b5690625fd8068725ca70
---
# Knowledge Base Overview

Code map for agents. Read [[index]] first, then one module page, then only the files that page cites.

## What is built

The app is one local process. ADR 0011 (accepted) names the modules Ledger, Import, PII/Masking, Vault, TaxMaps, Reporting, and Settings, and keeps domain events in memory (repo: docs/adr/0011-local-modular-monolith.md#L13-L16).

[[ledger]] is the double-entry book: draft, post, reverse, lock, and balances (repo: OfflineCPA/src/Backend/Endpoints/LedgerEndpoints.cs#L9-L71). Rules are on [[double-entry-invariants]]. The audit table is [[event-log]].

[[network-gate]] is the plug-in boundary: the ports in Application, the default-deny `INetworkGate` adapter, and `IAiAssist` with no implementation. Adapters register by module scan (repo: OfflineCPA/src/Application/Networking/INetworkGate.cs#L10-L36) (repo: OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs#L16-L56) (repo: OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs#L8-L22). The types renamed with it: `ILocalFileParser` is now `IStatementImporter` (repo: OfflineCPA/src/Application/Interfaces/IStatementImporter.cs#L6-L10).

[[uploads]] stores a file under a generated name (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L52-L53). [[pii-boundary]] records what that does and does not mask. [[charts-of-accounts]] seeds three JSON kinds through `POST /api/books/setup` (repo: OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs#L13) (repo: OfflineCPA/src/Backend/Program.cs#L127). [[data-and-migrations]] is the EF model and the `InitialCreate` and `LedgerCore` migrations (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L6) (repo: OfflineCPA/src/Infrastructure/Migrations/20261124120000_InitialCreate.cs#L9) (repo: OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs#L17). [[backend-host]] loads `.env`, requires a connection string, and the compose file points the backend at `Host=database` (repo: OfflineCPA/src/Backend/Program.cs#L26-L35) (repo: OfflineCPA/docker-compose.yml#L27).

[[frontend-shell]] is one dashboard. The setup wizard, reconcile, reports, and settings screens are planned in ADR 0003, not built (repo: docs/adr/0003-clean-architecture-ports-adapters.md#L15) (repo: OfflineCPA/frontend/src/App.tsx#L6-L28).

## How to use this map

Read [[index]], then the module page, then only the files that page cites. The `checked:` hashes are `git hash-object` of those files.
