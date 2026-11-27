---
checked:
  OfflineCPA/tests/TrueBalance.Tests/Imports/CardBalanceDiscardTests.cs: 1a57495e86ce8fe4af358a22abdf44e4f7ff8b92
  OfflineCPA/src/Domain/Entities/ImportBatch.cs: ed8dea2449750155438c18427a33473863c7f83d
  OfflineCPA/src/Application/Ledger/DiscardStagedBatch.cs: 05d1968589c9e9fbbce4a99cc5316a406f19063e
  OfflineCPA/src/Domain/Ledger/RunningBalance.cs: 7a503963374e18376d00fbeebd049f72a72bd08c
  OfflineCPA/tests/TrueBalance.Tests/Imports/SignLayoutTests.cs: d96e04f415ed8271a750271207945e899142348f
  OfflineCPA/src/Infrastructure/Services/CsvStatementImporter.cs: 694dde98371cc72074c90d97a415fe5f7d300d80
  OfflineCPA/src/Application/Imports/ImportUseCases.cs: ebf042aa23c3d06c4bc0876ef6687a4ff6c544f5
  OfflineCPA/src/Application/Ledger/PostStagedRow.cs: dbe439fe628be79db3f66feafa128bced70f0ed7
  OfflineCPA/src/Application/Interfaces/IStatementImporter.cs: 815216298b5b0524f1ab804254586bce3abc1586
  OfflineCPA/src/Backend/Endpoints/ImportEndpoints.cs: 95871dd1b17a55b0f6591570d2e097c6c93d823e
  OfflineCPA/src/Domain/Entities/StagedRow.cs: ac9b99c78eb91a6da2ac50ca72891c2781ac0f51
  OfflineCPA/tests/TrueBalance.Tests/Imports/ImportFlowTests.cs: 2f74c659589216c4f7a3d2c8938022e364bee7cb
  OfflineCPA/tests/TrueBalance.Tests/Imports/ImportNoLeakTests.cs: ca30e071a437dffe0c14cac59d577daddc43ba12
---
# Statement import

Think of it as a waiting room: a statement file is queued, parsed into staged rows, reviewed, and only then posted to the [[ledger]] one row at a time.

## Flow

A batch goes Queued, then Staged or Failed; a Failed batch can be tried again, and a discarded one returns to Queued. A row starts Pending or Duplicate and ends Posted or Discarded.

`QueueImport` records a stored document. `ImportStatement` parses it, masks each description, and stages rows without touching the ledger (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L9-L89). A parse failure marks the batch Failed with a reason that holds no file value (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L45-L63). A bank's CSV column mapping is saved by `SaveCsvProfile` (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L133-L200).

All three importers share `IStatementImporter`; PDF results are marked experimental (repo: OfflineCPA/src/Application/Interfaces/IStatementImporter.cs#L10-L22). Row rules live on the entity (repo: OfflineCPA/src/Domain/Entities/StagedRow.cs#L1-L81).

## Vault

Import and preview refuse with "Unlock the books first." while the vault is locked, and the batch stays Queued (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L35). Importers read a decrypted stream held in memory (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L49-L52). See [[uploads]].

## Sign layouts

A CSV file never decides its own signs. The bank profile holds one of four layouts and a confirmed flag, and `ImportStatement` refuses an unconfirmed CSV batch before parsing it; a layout sent with the request is saved as confirmed (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L99-L123). `SaveCsvProfile` validates and masks the columns and takes any confirmation back (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L133-L200). `PreviewCsvLayout` proposes a layout and writes nothing (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L209-L288).

The running balance is checked in both file orders; any row without a balance, or a file that fits both signs, means not available (repo: OfflineCPA/src/Domain/Ledger/RunningBalance.cs#L5-L32). A failed check makes every row of the batch need review before posting (repo: OfflineCPA/src/Domain/Entities/StagedRow.cs#L60-L72).

## Balance meaning

A balance column counts money available (previous plus amount) or an amount owed (previous minus amount). The meaning is confirmed with the sign layout and saved on the profile, and a later confirmation that omits it keeps the saved one; `FromAccountType` reads a liability account, such as a credit card, as owed (repo: OfflineCPA/src/Domain/Ledger/RunningBalance.cs#L5-L32). When the check fails but the other meaning would pass, the batch reason says so. That is the same arithmetic as reversed signs, so the reason names both (repo: OfflineCPA/src/Domain/Entities/ImportBatch.cs#L38-L52). The preview takes an optional `bankAccountId`; without one it reads the balance as available and says so (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L209-L288).

## Discard and import again

`DiscardStagedBatch` marks every row Discarded, deletes none, and re-queues the batch so the stored document can be imported with a new layout. It refuses while any posted entry from the batch has not been reversed (repo: OfflineCPA/src/Application/Ledger/DiscardStagedBatch.cs#L11-L35). Discarded rows cannot be posted, do not count as duplicates, and are listed only with `?includeDiscarded=true`, without a suggested category (repo: OfflineCPA/src/Backend/Endpoints/ImportEndpoints.cs#L38-L54).

## Duplicates

A row is flagged when an earlier batch on the same account has the same date, amount, and match key, built from the masked description. Identical rows inside one file are left alone (repo: OfflineCPA/src/Application/Imports/ImportUseCases.cs#L71-L76).

## Posting

`PostStagedRow` creates one two-line entry, bank account against the chosen category; money in debits the bank. The draft, the post and the row are saved in one call (repo: OfflineCPA/src/Application/Ledger/PostStagedRow.cs#L13-L43). Routes live under `/api/imports` (repo: OfflineCPA/src/Backend/Endpoints/ImportEndpoints.cs#L22-L71).

## Tests

`ImportFlowTests` covers signs and decimals, duplicates, single posting, and parse errors (repo: OfflineCPA/tests/TrueBalance.Tests/Imports/ImportFlowTests.cs#L58-L170). `ImportNoLeakTests` runs fake values through the real importers (repo: OfflineCPA/tests/TrueBalance.Tests/Imports/ImportNoLeakTests.cs#L14-L19). They use the EF in-memory provider, so Postgres behavior is not covered.

`CardBalanceDiscardTests` covers balance meaning and discard (repo: OfflineCPA/tests/TrueBalance.Tests/Imports/CardBalanceDiscardTests.cs#L66-L199). `SignLayoutTests` covers the layouts, preview, refusal and balance check (repo: OfflineCPA/tests/TrueBalance.Tests/Imports/SignLayoutTests.cs#L17-L60).

## Parser limits

A PDF line with two trailing numbers is read as amount, then balance. The preview still starts from a guess: a negative debit is a reversal only when the same file also has positive debits, unless the balance says otherwise. An OFX file with more than one account statement is refused.

## Change it safely

Mask any new text field before it is stored or logged ([[pii-masking]]). A new format is one more `IStatementImporter` ([[uploads]]).
