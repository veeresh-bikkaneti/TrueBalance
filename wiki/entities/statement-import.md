---
checked:
  OfflineCPA/src/Application/Imports/ImportUseCases.cs: eb1d788272f75055e8c02b9f52d4bbf606676502
  OfflineCPA/src/Application/Ledger/PostStagedRow.cs: dbe439fe628be79db3f66feafa128bced70f0ed7
  OfflineCPA/src/Application/Interfaces/IStatementImporter.cs: ba4a0fa808dc243bd3d61a74df52ca6b567b3bf0
  OfflineCPA/src/Backend/Endpoints/ImportEndpoints.cs: b812756eab5e854fe03b6830c640844a6d5dc3b5
  OfflineCPA/src/Domain/Entities/StagedRow.cs: 037c3aee6c0da652b3a5615bd38a21c0c0120102
  OfflineCPA/tests/TrueBalance.Tests/Imports/ImportFlowTests.cs: 255389e0a984da7585e22cbafabd727476a701a9
  OfflineCPA/tests/TrueBalance.Tests/Imports/ImportNoLeakTests.cs: 341c560b952f101d7159ef78735d2a55b23cf4f4
---
# Statement import

Think of it as a waiting room: a statement file is queued, parsed into staged rows, reviewed, and only then posted to the [[ledger]] one row at a time.

## Flow

A batch goes Queued, then Staged or Failed; a Failed batch can be tried again. A row starts Pending or Duplicate and ends Posted.

`QueueImport` records a stored document. `ImportStatement` parses it, masks each description, and stages rows without touching the ledger (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L7-L70). A parse failure marks the batch Failed with a reason that holds no file value (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L35-L54). A bank's CSV column mapping is saved by `SaveCsvProfile` (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L89-L119).

All three importers share `IStatementImporter`; PDF results are marked experimental (repo: OfflineCPA/src/Application/Interfaces/IStatementImporter.cs#L8-L20). Row rules live on the entity (repo: OfflineCPA/src/Domain/Entities/StagedRow.cs#L1-L71).

## Duplicates

A row is flagged when an earlier batch on the same account has the same date, amount, and match key, built from the masked description. Identical rows inside one file are left alone (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L59-L64).

## Posting

`PostStagedRow` creates one two-line entry, bank account against the chosen category; money in debits the bank. The draft, the post and the row are saved in one call (repo: OfflineCPA/src/Application/Ledger/PostStagedRow.cs#L13-L43). Routes live under `/api/imports` (repo: OfflineCPA/src/Backend/Endpoints/ImportEndpoints.cs#L21-L49).

## Tests

`ImportFlowTests` covers signs and decimals, duplicates, single posting, and parse errors (repo: OfflineCPA/tests/TrueBalance.Tests/Imports/ImportFlowTests.cs#L57-L169). `ImportNoLeakTests` runs fake values through the real importers (repo: OfflineCPA/tests/TrueBalance.Tests/Imports/ImportNoLeakTests.cs#L14-L19). They use the EF in-memory provider, so Postgres behavior is not covered.

## Parser limits

A PDF line with two trailing numbers is read as amount, then balance. In CSV, a negative debit is a reversal only when the same file also has positive debits. An OFX file with more than one account statement is refused.

## Change it safely

Mask any new text field before it is stored or logged ([[pii-masking]]). A new format is one more `IStatementImporter` ([[uploads]]).
