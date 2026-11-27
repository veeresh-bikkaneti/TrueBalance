---
checked:
  OfflineCPA/src/Backend/Endpoints/LedgerEndpoints.cs: 80a26042df5de3cd4363a4e97018b372a2b36ba6
  OfflineCPA/src/Domain/Entities/JournalEntry.cs: 60a5b3d1e5b424ba8d60bd599e5a6072b3de7e87
  OfflineCPA/src/Backend/Program.cs: 153e78f333ac9b25969edaef934534cbb64e786b
  OfflineCPA/src/Domain/Entities/Posting.cs: 53956565729f8b7d009c0bcc9997481f9ea6e4f5
  OfflineCPA/src/Domain/Entities/Period.cs: bcc4fe128753b85180c4567b93e875337cb7df6c
  OfflineCPA/src/Domain/Enums/JournalEntryStatus.cs: 8eb3b37ef9b17fad255980f39c475a6dfcde9fd3
  OfflineCPA/src/Application/Interfaces/ILedgerStore.cs: 6e17c978134fa42d918dad9f2d92d34e2d2823ac
  OfflineCPA/src/Infrastructure/Data/EfLedgerStore.cs: 4cd65b156acdb9137bc2e5656d1663c61fb300fa
  OfflineCPA/src/Application/Ledger/CreateDraftEntry.cs: c4828307290c306fcc5540383dea320b11c4f330
  OfflineCPA/src/Application/Ledger/PostEntry.cs: 1a33aa5abaf015344c6996da8ccd00adcbcca53e
  OfflineCPA/src/Application/Ledger/ReverseEntry.cs: 15513cf210f49717466fb92da07acbd5d295bcbd
  OfflineCPA/src/Application/Ledger/LockPeriod.cs: 5847bbfe4d6d13869463ab78e55d4d8751fff324
  OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerTests.cs: 32143217f410c0b14792b66a476804be626dbab6
  OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs: c241452235c4fd1d5dbe64d134c98008fc5cf610
  docs/architecture/modules.md: 3d9497c0c96c00fe368eac6fba2caba23dd4e726
---
# Ledger

The ledger is the double-entry book: draft an entry, post it, reverse it, lock a period, and read account balances (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L45-L131) (repo: OfflineCPA/src/Backend/Endpoints/LedgerEndpoints.cs#L9-L71).

## Types and files

`JournalEntry` holds the header, postings, and status. `Posting` is one signed line. `Period` is the date window an entry belongs to (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L7-L22) (repo: OfflineCPA/src/Domain/Entities/Posting.cs#L6-L13) (repo: OfflineCPA/src/Domain/Entities/Period.cs#L6-L14). Status is `Draft` (0), `Posted` (1), or `Reversed` (2) (repo: OfflineCPA/src/Domain/Enums/JournalEntryStatus.cs#L3-L8).

`ILedgerStore` is the port: find and add entries, find a period, read balances, append an audit row, and save (repo: OfflineCPA/src/Application/Interfaces/ILedgerStore.cs#L5-L14). `EfLedgerStore` is the Postgres adapter. Balances sum posting amounts on entries whose status is Posted or Reversed (repo: OfflineCPA/src/Infrastructure/Data/EfLedgerStore.cs#L8-L40).

## Entry points

Use cases are `CreateDraftEntry`, `PostEntry`, `ReverseEntry`, `LockPeriod`, and `GetAccountBalances`. Create dispatches domain events and then calls `SaveChanges`. Post, reverse, and lock do the same (repo: OfflineCPA/src/Application/Ledger/CreateDraftEntry.cs#L32-L34) (repo: OfflineCPA/src/Application/Ledger/PostEntry.cs#L29-L30) (repo: OfflineCPA/src/Application/Ledger/ReverseEntry.cs#L31-L32) (repo: OfflineCPA/src/Application/Ledger/LockPeriod.cs#L27-L28).

Routes, all thin, map a rule failure to HTTP 400. Until a later prompt there is no auth, so the user id is the constant `local` (repo: OfflineCPA/src/Backend/Endpoints/LedgerEndpoints.cs#L9-L71):

- `POST /api/ledger/entries`
- `POST /api/ledger/entries/{id}/post`
- `POST /api/ledger/entries/{id}/reverse`
- `POST /api/ledger/periods/{id}/lock`
- `GET /api/ledger/balances`

## Invariants and tests

Balance and lock rules are on [[double-entry-invariants]]. The audit row for each event is on [[event-log]].

Behavior tests live in `LedgerTests` (reject unbalanced posts, reversals, locked periods, audit rows) (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerTests.cs#L11-L15). Namespace rules live in `LedgerArchitectureTests` (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerArchitectureTests.cs#L22-L108).

## Change it safely

Put a new rule on the entity method, then on the use case that calls it, then on `EfLedgerStore` if the query changes, and add a fact in `LedgerTests`. A new type that touches `JournalEntry`, `Posting`, `Period`, `LedgerAuditRecord`, or `IDomainEvent` must stay in a namespace from the module table (repo: docs/architecture/modules.md#L7-L19). Posted entries are not edited in place. `JournalEntry` has no public property setters (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerTests.cs#L65-L73).
