---
checked:
  OfflineCPA/src/Domain/Ledger/RunningBalance.cs: 141afc5101731ab7b252d8ad24fb8d752ca46dc2
  OfflineCPA/tests/TrueBalance.Tests/Imports/SignLayoutTests.cs: 6fc0b498ce37a79c47cd882d8a6dc76f55485242
  OfflineCPA/src/Infrastructure/Services/CsvStatementImporter.cs: f50a111b1a468fffa2641b95d54a90856271e449
  OfflineCPA/src/Application/Imports/ImportUseCases.cs: 4afc02ee6c7742aac272ea13f94d550df852a2d8
  OfflineCPA/src/Application/Ledger/PostStagedRow.cs: dbe439fe628be79db3f66feafa128bced70f0ed7
  OfflineCPA/src/Application/Interfaces/IStatementImporter.cs: 1257e035bcc803dd116492123dcd5bd7ff30a565
  OfflineCPA/src/Backend/Endpoints/ImportEndpoints.cs: 7c37e697f1552a5cb2c55a1db48b093917d99501
  OfflineCPA/src/Domain/Entities/StagedRow.cs: 5c8c15a06e97a854e7b27ec17e6d641434ec9483
  OfflineCPA/tests/TrueBalance.Tests/Imports/ImportFlowTests.cs: f877403b27212b2c5e220134491fdbbf164b7a65
  OfflineCPA/tests/TrueBalance.Tests/Imports/ImportNoLeakTests.cs: 2ae563b07bca5b4676ef9be31ac73824c010c9ee
---
# Statement import

Think of it as a waiting room: a statement file is queued, parsed into staged rows, reviewed, and only then posted to the [[ledger]] one row at a time.

## Flow

A batch goes Queued, then Staged or Failed; a Failed batch can be tried again. A row starts Pending or Duplicate and ends Posted.

`QueueImport` records a stored document. `ImportStatement` parses it, masks each description, and stages rows without touching the ledger (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L8-L85). A parse failure marks the batch Failed with a reason that holds no file value (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L43-L62). A bank's CSV column mapping is saved by `SaveCsvProfile` (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L127-L192).

All three importers share `IStatementImporter`; PDF results are marked experimental (repo: OfflineCPA/src/Application/Interfaces/IStatementImporter.cs#L10-L22). Row rules live on the entity (repo: OfflineCPA/src/Domain/Entities/StagedRow.cs#L1-L76).

## Sign layouts

A CSV file never decides its own signs. The bank profile holds one of four layouts and a confirmed flag, and `ImportStatement` refuses an unconfirmed CSV batch before parsing it; a layout sent with the request is saved as confirmed (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L95-L118). `SaveCsvProfile` validates and masks the columns and takes any confirmation back (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L127-L193). `PreviewCsvLayout` proposes a layout and writes nothing (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L199-L266).

The running balance is checked in both file orders; any row without a balance means not available (repo: OfflineCPA/src/Domain/Ledger/RunningBalance.cs#L5-L16). A failed check makes every row of the batch need review before posting (repo: OfflineCPA/src/Domain/Entities/StagedRow.cs#L55-L68). The check assumes money in raises the balance, so a credit card statement that lists the amount owed will fail it.

## Duplicates

A row is flagged when an earlier batch on the same account has the same date, amount, and match key, built from the masked description. Identical rows inside one file are left alone (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L67-L72).

## Posting

`PostStagedRow` creates one two-line entry, bank account against the chosen category; money in debits the bank. The draft, the post and the row are saved in one call (repo: OfflineCPA/src/Application/Ledger/PostStagedRow.cs#L13-L43). Routes live under `/api/imports` (repo: OfflineCPA/src/Backend/Endpoints/ImportEndpoints.cs#L21-L54).

## Tests

`ImportFlowTests` covers signs and decimals, duplicates, single posting, and parse errors (repo: OfflineCPA/tests/TrueBalance.Tests/Imports/ImportFlowTests.cs#L58-L170). `ImportNoLeakTests` runs fake values through the real importers (repo: OfflineCPA/tests/TrueBalance.Tests/Imports/ImportNoLeakTests.cs#L14-L19). They use the EF in-memory provider, so Postgres behavior is not covered.

`SignLayoutTests` covers the layouts, preview, refusal and balance check (repo: OfflineCPA/tests/TrueBalance.Tests/Imports/SignLayoutTests.cs#L17-L60).

## Parser limits

A PDF line with two trailing numbers is read as amount, then balance. The preview still starts from a guess: a negative debit is a reversal only when the same file also has positive debits, unless the balance says otherwise. An OFX file with more than one account statement is refused.

## Change it safely

Mask any new text field before it is stored or logged ([[pii-masking]]). A new format is one more `IStatementImporter` ([[uploads]]).
