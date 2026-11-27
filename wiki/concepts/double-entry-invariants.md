---
checked:
  OfflineCPA/src/Backend/Endpoints/LedgerEndpoints.cs: 80a26042df5de3cd4363a4e97018b372a2b36ba6
  OfflineCPA/src/Domain/Entities/Posting.cs: 53956565729f8b7d009c0bcc9997481f9ea6e4f5
  OfflineCPA/src/Domain/Ledger/Money.cs: a2a10d99251e73d37eedc059e1b804a57e1e3f36
  OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs: 3722c8129033d5fa805f1277b55c83cde13a5c91
  OfflineCPA/src/Domain/Entities/JournalEntry.cs: 60a5b3d1e5b424ba8d60bd599e5a6072b3de7e87
  OfflineCPA/src/Infrastructure/Data/EfLedgerStore.cs: 4cd65b156acdb9137bc2e5656d1663c61fb300fa
  OfflineCPA/src/Domain/Entities/Period.cs: bcc4fe128753b85180c4567b93e875337cb7df6c
  OfflineCPA/src/Domain/Ledger/LedgerRuleException.cs: 17a075cde4cb15a500cb0ad2a16db76dda06ab7a
  OfflineCPA/src/Backend/Program.cs: 452674984015389ecb7473705b58db3f8a3cbf88
  OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerTests.cs: 32143217f410c0b14792b66a476804be626dbab6
  docs/ledger.md: 2eba6c35c8c780c81a1572e5362ff66584445921
---
# Double-entry invariants

A posting amount is a signed `decimal`. Positive is a debit and negative is a credit. There is no side column (repo: OfflineCPA/src/Domain/Entities/Posting.cs#L5-L11). Zero amounts are rejected, and amounts with more than two decimal places are rejected without rounding (repo: OfflineCPA/src/Domain/Entities/Posting.cs#L22-L31) (repo: OfflineCPA/src/Domain/Ledger/Money.cs#L3-L19). The column type is `numeric(18,2)` (repo: OfflineCPA/src/Infrastructure/Data/OfflineCPADbContext.cs#L72-L75).

## Draft, post, reverse

`CreateDraft` requires a non-empty memo of at most 500 characters, a date inside the period, an unlocked period, and at least one posting. A draft may be unbalanced (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L40-L68).

`Post` requires status Draft, at least two postings, a sum of exactly `0.00`, the entry's own period, and that period unlocked. It then sets status Posted (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L71-L93).

`Reverse` requires status Posted and no existing reversal. It does not edit the original lines. It returns a new Posted entry whose amounts are the negation, sets `ReversesEntryId` on the new entry and `ReversedByEntryId` on the original, and marks the original Reversed (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L96-L131). The reversal period must be unlocked and must contain the reversal date, so a locked period can be corrected from an open one (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L110-L113).

## Balances and locks

`IsInLedger` is true for Posted and Reversed. Both must be summed or the reversal would shift the account (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L32-L36) (repo: OfflineCPA/src/Infrastructure/Data/EfLedgerStore.cs#L26-L34).

`Period.Lock` can run once. There is no unlock method (repo: OfflineCPA/src/Domain/Entities/Period.cs#L41-L49). A locked period rejects new drafts and posts (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L53-L54) (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L88-L89).

Violations throw `LedgerRuleException`. The ledger routes map that failure to HTTP 400 (repo: OfflineCPA/src/Domain/Ledger/LedgerRuleException.cs#L4) (repo: OfflineCPA/src/Backend/Endpoints/LedgerEndpoints.cs#L9-L71).

## Change it safely

Change the check and the matching fact in `LedgerTests` together (repo: OfflineCPA/tests/TrueBalance.Tests/Ledger/LedgerTests.cs#L15). The database triggers are a second line behind these rules; they do not check that postings balance (repo: docs/ledger.md#L95-L108). See [[ledger]] and [[event-log]].
