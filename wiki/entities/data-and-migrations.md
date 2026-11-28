---
checked:
  OfflineCPA/src/Infrastructure/Migrations/20261127185050_TaxLineIds.cs: 02788d132741c47b24d2ac28336dfd1dfb4ff246
  OfflineCPA/src/Infrastructure/Migrations/20261127135749_CardBalanceAndDiscard.cs: f722d2f1aa50070d35441554477d14bcda624505
  OfflineCPA/src/Infrastructure/Migrations/20261127054233_StatementImport.cs: b1a5dc047d31c5c85b864e2fddc5f23f3cf8d2c4
  OfflineCPA/src/Infrastructure/Data/ImportConfigurations.cs: 15164318598785c1d3261ee25db502412b6cccf9
  OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs: 8382eb3de930dd971227f05a0cabefa92b192d8a
  OfflineCPA/src/Infrastructure/Migrations/20261124120000_InitialCreate.cs: eb9d9a85324ec58feec91a70cb926d375c48342b
  OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs: e9a88657b2bea914a76921239982eba4df689c19
  OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs: c241452235c4fd1d5dbe64d134c98008fc5cf610
  OfflineCPA/src/Backend/Program.cs: 742bc67ef2e99c040cdbdaba2b50a1fcda30bf82
  docs/ledger.md: 2eba6c35c8c780c81a1572e5362ff66584445921
---
# Data and migrations

`OfflineCPADbContext` is the EF Core model. Sets are `Accounts`, `Transactions`, `JournalEntries`, `Periods`, `Postings`, `LedgerAuditRecords`, `BookSettings`, and `MileageLog` (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L6-L22). Account `Code` is required, max 50, and unique. Posting `Amount` is `numeric(18,2)`. Audit `EventType` is required, max 40 (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L31-L113). Domain event lists are ignored by EF (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L66) (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L88).

## Migrations

`InitialCreate` is the migration class in `20261124120000_InitialCreate.cs` (repo: OfflineCPA/src/Infrastructure/Migrations/20261124120000_InitialCreate.cs#L9). `LedgerCore` drops the old line-level `JournalEntries` table and creates `LedgerAuditRecords`, `Periods`, `JournalEntries`, and `Postings` (repo: OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs#L27-L88). It then adds three triggers: postings of a non-draft entry cannot be changed or deleted, a non-draft header freezes date, memo, and period, and audit rows reject update and delete (repo: OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs#L137-L205).

`CardBalanceAndDiscard` only adds columns: the balance meaning on a bank profile (0 is `FromAccountType`, so old profiles keep working) and the reset count and time on an import batch (repo: OfflineCPA/src/Infrastructure/Migrations/20261127135749_CardBalanceAndDiscard.cs#L12-L31).

`Ledger_model_matches_the_snapshot` asserts `HasPendingModelChanges()` is false. The comment says it does not open a database (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs#L133-L142). Startup calls `Database.Migrate()` (repo: OfflineCPA/src/Backend/Program.cs#L118-L123).

`StatementImport` adds the tables for import batches, staged rows, and CSV mapping profiles (repo: OfflineCPA/src/Infrastructure/Migrations/20261127054233_StatementImport.cs#L1-L20); their EF configuration lives in its own file (repo: OfflineCPA/src/Infrastructure/Data/ImportConfigurations.cs#L1-L56). See [[statement-import]].

`TaxLineIds` adds `TaxLineId`, `IsMeals`, the mileage log table, and the book settings table. It copies each old tax line number to a 1120-S line ID, then drops `TaxFormLine` (repo: OfflineCPA/src/Infrastructure/Migrations/20261127185050_TaxLineIds.cs#L56-L79). `Down` restores the column; travel and meals come back as other deductions (repo: OfflineCPA/src/Infrastructure/Migrations/20261127185050_TaxLineIds.cs#L113-L139). See [[tax-maps]].

The migration keeps the meals flag: old value 21 sets `IsMeals`, and `Down` brings 21 back (repo: OfflineCPA/src/Infrastructure/Migrations/20261127185050_TaxLineIds.cs#L76) (repo: OfflineCPA/src/Infrastructure/Migrations/20261127185050_TaxLineIds.cs#L132).

## Change it safely

Change the entity and `OnModelCreating` together, add a migration, and keep the snapshot green. Do not edit a migration that has already shipped; add another. Trigger text lives in `LedgerCore` and is not checked by CI against Postgres (repo: docs/ledger.md#L110-L116). See [[ledger]] and [[charts-of-accounts]].
