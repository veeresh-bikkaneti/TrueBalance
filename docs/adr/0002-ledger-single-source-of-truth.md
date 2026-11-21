# ADR 0002: The double-entry ledger is the single source of truth

- Status: Accepted
- Date: 2026-11-21

## Context
Today the app saves uploaded files, creates a row per file and dumps rows to Excel. No journal entries are ever created and the export has no amounts (see `docs/research/review.md`, sections 1 and 3). IRS Pub 583 allows single- or double-entry systems; double-entry uses journals and ledgers ([IRS Pub 583](https://irs.gov/publications/p583)) [Confidence: Moderate | Vote: 1/1 primary].

## Decision
- Every financial fact is a `JournalEntry` with two or more `Posting` lines whose debits equal credits.
- Entries move Draft -> Posted -> Reversed. Posted entries are immutable; corrections are reversing entries linked to the original.
- Fiscal `Period`s can be locked; nothing posts into a locked period.
- Every create, post, reverse and lock writes an append-only audit record.
- Money is `decimal` with 2 places at the boundary; never floating point.

## Consequences
- Imports, reports, reconciliation and exports all depend on the ledger; none may write around it.
- Needs property-style tests: sum of all balances is always 0.
- Immutability is enforced in the use case and by a database guard.
- Spec: `docs/specs/prompts/03-ledger-core.md`.
