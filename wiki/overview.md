---
checked:
  OfflineCPA/src/Backend/Endpoints/LedgerEndpoints.cs: 33c34cedec49c541cfae8e468e5484c0df8759dc
  docs/adr/0011-local-modular-monolith.md: 4792ef60fdc033c952b3dc6d252ba9138881bb26
  OfflineCPA/src/Backend/Program.cs: 4000a270a06e4648a68e0d0b9f83186ef7776106
  OfflineCPA/src/Application/Networking/INetworkGate.cs: 77fab7a5d681fe7d25f236857a96c4ab8fe25914
  OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs: 9b776c957e947fa9159fe6cc21371dfa74ae1058
  OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs: 98f6f27b02c94a2298b2860b84e40a66d8f1b56c
  OfflineCPA/src/Application/Interfaces/IStatementImporter.cs: 815216298b5b0524f1ab804254586bce3abc1586
  OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs: 7ece069c6c1c068bcf26fe6e7932bbd8c2551059
  OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs: dfc92b704f4cd8693ce88715c6d40fdf2884d201
  OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs: 8382eb3de930dd971227f05a0cabefa92b192d8a
  OfflineCPA/src/Infrastructure/Migrations/20261124120000_InitialCreate.cs: eb9d9a85324ec58feec91a70cb926d375c48342b
  OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs: e9a88657b2bea914a76921239982eba4df689c19
  OfflineCPA/docker-compose.yml: bf787f349e19e2ea266c10a9dc5f17cf1f114962
  docs/adr/0003-clean-architecture-ports-adapters.md: c9c7256eea1b992b1d2443ea30a1356ec0de5388
  OfflineCPA/frontend/src/App.tsx: 0586494bd4d8b86970a1f26bcdaa1420af6bdafa
---
# Knowledge Base Overview

Code map for agents. Read [[index]] first, then one module page, then only the files that page cites.

## What is built

The app is one local process. ADR 0011 (accepted) names the modules Ledger, Import, PII/Masking, Vault, TaxMaps, Reporting, and Settings, and keeps domain events in memory (repo: docs/adr/0011-local-modular-monolith.md#L13-L16).

[[ledger]] is the double-entry book: draft, post, reverse, lock, and balances (repo: OfflineCPA/src/Backend/Endpoints/LedgerEndpoints.cs#L9-L71). Rules are on [[double-entry-invariants]]. The audit table is [[event-log]].

[[network-gate]] is the plug-in boundary: the ports in Application, the default-deny `INetworkGate` adapter, and `IAiAssist` with no implementation. Adapters register by module scan (repo: OfflineCPA/src/Application/Networking/INetworkGate.cs#L10-L36) (repo: OfflineCPA/src/Infrastructure/Networking/LocalNetworkGate.cs#L16-L56) (repo: OfflineCPA/src/Infrastructure/AdapterModuleRegistration.cs#L8-L22). The types renamed with it: `ILocalFileParser` is now `IStatementImporter` (repo: OfflineCPA/src/Application/Interfaces/IStatementImporter.cs#L10-L16).

[[statement-import]] stages statement rows for review before posting. [[uploads]] seals a file under a generated name (repo: OfflineCPA/src/Infrastructure/Vault/EncryptedFileVault.cs#L107). [[pii-boundary]] records what that does and does not mask. [[charts-of-accounts]] seeds three JSON kinds through `POST /api/books/setup` (repo: OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs#L13) (repo: OfflineCPA/src/Backend/Program.cs#L142). [[data-and-migrations]] is the EF model and the `InitialCreate` and `LedgerCore` migrations (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L6) (repo: OfflineCPA/src/Infrastructure/Migrations/20261124120000_InitialCreate.cs#L9) (repo: OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs#L17). [[backend-host]] loads `.env`, requires a connection string, and the compose file points the backend at `Host=database` (repo: OfflineCPA/src/Backend/Program.cs#L26-L35) (repo: OfflineCPA/docker-compose.yml#L27).

[[frontend-shell]] is an app shell with one screen, Home; each new screen adds its own folder. The setup wizard, reconcile, reports, and settings screens are planned in ADR 0003, not built (repo: docs/adr/0003-clean-architecture-ports-adapters.md#L15) (repo: OfflineCPA/frontend/src/App.tsx#L4-L10).

## How to use this map

Read [[index]], then the module page, then only the files that page cites. The `checked:` hashes are `git hash-object` of those files.
