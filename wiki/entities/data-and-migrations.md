---
checked:
  OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs: 79af2ba8029ac2126a5a2367c395c30dc210b3ab
  OfflineCPA/src/Infrastructure/Migrations/20261124120000_InitialCreate.cs: eb9d9a85324ec58feec91a70cb926d375c48342b
  OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs: e9a88657b2bea914a76921239982eba4df689c19
  OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs: f932d7f98d523b9c1a91b37bd75be4e75fe3bd08
  OfflineCPA/src/Backend/Program.cs: 631e0233ec079f91233b2c85b031567189a4e932
  docs/ledger.md: 2eba6c35c8c780c81a1572e5362ff66584445921
---
# Data and migrations

`OfflineCPADbContext` is the EF Core model. Sets are `Accounts`, `Transactions`, `JournalEntries`, `Periods`, `Postings`, and `LedgerAuditRecords` (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L6-L17). Account `Code` is required, max 50, and unique. Posting `Amount` is `numeric(18,2)`. Audit `EventType` is required, max 40 (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L23-L86). Domain event lists are ignored by EF (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L41) (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L63).

## Migrations

`InitialCreate` is the migration class in `20261124120000_InitialCreate.cs` (repo: OfflineCPA/src/Infrastructure/Migrations/20261124120000_InitialCreate.cs#L9). `LedgerCore` drops the old line-level `JournalEntries` table and creates `LedgerAuditRecords`, `Periods`, `JournalEntries`, and `Postings` (repo: OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs#L27-L88). It then adds three triggers: postings of a non-draft entry cannot be changed or deleted, a non-draft header freezes date, memo, and period, and audit rows reject update and delete (repo: OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs#L137-L205).

`Ledger_model_matches_the_snapshot` asserts `HasPendingModelChanges()` is false. The comment says it does not open a database (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs#L65-L73). Startup calls `Database.Migrate()` (repo: OfflineCPA/src/Backend/Program.cs#L247-L251).

## Change it safely

Change the entity and `OnModelCreating` together, add a migration, and keep the snapshot green. Do not edit a migration that has already shipped; add another. Trigger text lives in `LedgerCore` and is not checked by CI against Postgres (repo: docs/ledger.md#L110-L116). See [[ledger]] and [[charts-of-accounts]].
