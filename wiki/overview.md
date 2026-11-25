---
checked:
  docs/adr/0011-local-modular-monolith.md: 4792ef60fdc033c952b3dc6d252ba9138881bb26
  OfflineCPA/src/Backend/Program.cs: 631e0233ec079f91233b2c85b031567189a4e932
  OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs: b7407284bbe0b6fccb23e4623d767c8e499e21f4
  OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs: 068b242b1c9b1ab4ad4773cf925766b1c6e6c717
  OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs: 79af2ba8029ac2126a5a2367c395c30dc210b3ab
  OfflineCPA/src/Infrastructure/Migrations/20261124120000_InitialCreate.cs: eb9d9a85324ec58feec91a70cb926d375c48342b
  OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs: e9a88657b2bea914a76921239982eba4df689c19
  OfflineCPA/docker-compose.yml: 0a79ce20514571bcf91d957214efe822bf930bdc
  docs/adr/0003-clean-architecture-ports-adapters.md: c9c7256eea1b992b1d2443ea30a1356ec0de5388
  OfflineCPA/frontend/src/App.tsx: 306f2244e219b1665f4b5690625fd8068725ca70
---
# Knowledge Base Overview

Code map for agents. Read [[index]] first, then one module page, then only the files that page cites.

## What is built

The app is one local process. ADR 0011 (accepted) names the modules Ledger, Import, PII/Masking, Vault, TaxMaps, Reporting, and Settings, and keeps domain events in memory (repo: docs/adr/0011-local-modular-monolith.md#L13-L16).

[[ledger]] is the double-entry book: draft, post, reverse, lock, and balances (repo: OfflineCPA/src/Backend/Program.cs#L190-L245). Rules are on [[double-entry-invariants]]. The audit table is [[event-log]].

[[uploads]] stores a file under a generated name (repo: OfflineCPA/src/Infrastructure/Services/SecureUploadStore.cs#L52-L53). [[pii-boundary]] records what that does and does not mask. [[charts-of-accounts]] seeds three JSON kinds through `POST /api/books/setup` (repo: OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs#L13) (repo: OfflineCPA/src/Backend/Program.cs#L127). [[data-and-migrations]] is the EF model and the `InitialCreate` and `LedgerCore` migrations (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L6) (repo: OfflineCPA/src/Infrastructure/Migrations/20261124120000_InitialCreate.cs#L9) (repo: OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs#L17). [[backend-host]] loads `.env`, requires a connection string, and the compose file points the backend at `Host=database` (repo: OfflineCPA/src/Backend/Program.cs#L15-L30) (repo: OfflineCPA/docker-compose.yml#L27).

[[frontend-shell]] is one dashboard. The setup wizard, reconcile, reports, and settings screens are planned in ADR 0003, not built (repo: docs/adr/0003-clean-architecture-ports-adapters.md#L15) (repo: OfflineCPA/frontend/src/App.tsx#L6-L28).

## How to use this map

Read [[index]], then the module page, then only the files that page cites. The `checked:` hashes are `git hash-object` of those files.

