# TrueBalance prompt 03b: close the ledger core gaps on PR 28

Phase: P0 | Branch: `feat/ledger-core` (exists, PR 28, draft) | Token cap: 90k | Estimate: 55k | Seats: 2 builders on cheaper models (Seat T: architecture tests + modules doc; Seat E: events + event log) + 1 mid-tier reviewer
Depends on: D2 merged (so `docs/specs/prompts/03-ledger-core.md` on main is the revised one). Do not open a new PR.

You are working in the TrueBalance repo, on PR 28's branch.

## Step 0
Run `git fetch origin && git status -sb && git log --oneline -5 origin/feat/ledger-core` and paste it. Stop if the tree is dirty or the head is not `ca2b111346f7b44a1d206fe54c93eed9278f4ba5` or a later commit on top of it. If main moved since PR 28 branched and GitHub shows the branch behind, stop and ask Veeresh to press "Update branch"; never run `git merge` or rebase.

## What the architect review found on ca2b111 (2026-11-25)
PR 28 follows the old 03 spec. CI is green and posted entries are guarded by database triggers. Against ADR 0011 and the revised `03-ledger-core.md` it is missing:
1. Architecture tests that fail when Domain references Infrastructure or a framework, or when anything outside the Ledger module writes ledger tables. No `docs/architecture/modules.md`.
2. In-process domain events `EntryPosted`, `EntryReversed`, `PeriodLocked`, dispatched in memory inside the same database transaction, each appending one row to an append-only event log with masked data only. PR 28 writes a `LedgerAuditRecord` row instead, with no event types or dispatcher.
3. Migration `20261125000000_LedgerCore` drops the old `JournalEntries` table instead of renaming and keeping rows. The PR body says nothing ever wrote to that table.
Re-check each claim in the code before you change anything; say if the code disagrees.

## Changes (pstack: sharpen, statechart, swarm, architect, arena, how, tdd, unslop)
1. **Arena, then build:** dependency-rule library for architecture tests (two options, for example NetArchTest.Rules and ArchUnitNET). Pick one with one reason. Its license must be MIT, Apache-2.0 or BSD; cite the license file. Tests: Domain has no reference to Infrastructure, EF Core or ASP.NET Core; Application has no reference to Infrastructure; only types in the Ledger namespaces touch `JournalEntries`, `Postings`, `Periods` and the event log. Show each test failing once on a deliberate violation (paste the run), then remove the violation.
2. **Arena, then build:** event log shape. Option A: rename `LedgerAuditRecords` to `EventLog` and add an `EventType` column. Option B: keep the table and document it as the event log. Pick one with one reason. Either way: an `IDomainEvent` marker in Domain, the three events raised by the aggregate, and a dispatcher in Application that runs inside the same unit of work. Event rows hold who (local user id), when, event type, entity id and a masked summary only; no descriptions, names or account numbers. Keep the existing insert-only trigger.
3. **Migration:** check with a query whether any row could exist in the old `JournalEntries` table on an install made from main before PR 28 (01 seed data included). Paste the query and result. If none can exist, add a short comment in the migration and a line in `docs/ledger.md` saying so. If rows can exist, migrate them into `JournalEntries` (header) and `Postings` instead of dropping them.
4. `docs/architecture/modules.md`: the module list from ADR 0011, which namespaces belong to Ledger, the dependency rules the tests enforce, and the event contracts (name, fields, masked fields).
5. Keep `Program.cs` edits to the endpoint wiring only. PR 26 also edits `Program.cs`; whichever merges second needs GitHub's "Update branch" and a conflict fix by Veeresh. Say in the PR body which lines overlap.

## Acceptance (tdd, narrow)
- Posting an entry writes exactly one event row in the same transaction; a failed post writes none.
- Reversing writes one `EntryReversed` row; locking writes one `PeriodLocked` row.
- An update or delete of an event row fails at the database.
- Architecture tests pass, and failed once on the deliberate violation.
- The test suite and the backend CI job stay green.

## Source and evidence rules
Apply the owner's rules exactly as written in the Facts section of `03-ledger-core.md`. Library claims (license, version, API) need a direct link and a tag.

## Budget, git and report
- Cap 90k, estimate 55k; say both first with per-seat budgets (about 25k per builder, 10k reviewer); stop and ask before passing the cap; report measured tokens or "Data not available."
- Conventional Commits (`feat(ledger):`, `test(ledger):`, `docs(ledger):`). No `Co-Authored-By` or AI trailer. No "Generated with" line. No rebase, force push or `git merge`. Push to PR 28 only; update its body (Before/After/How) to cover the new work. Veeresh merges.
- Report (pstack:teach + i-have-adhd): answer first, then 3 to 5 bullets: the two arena picks, the migration finding, test and CI results, `Program.cs` overlap with PR 26, full head SHA, tokens vs cap.
