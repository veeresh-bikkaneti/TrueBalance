# TrueBalance product backlog (refined)

Last refinement: 2026-11-21. Source of truth for order and scope. Each item maps to one draft PR and one spec in `docs/specs/prompts/`. Token figures are planning caps and estimates, not measurements.

## Product goal
A free, open-source, offline bookkeeping app that a small business or household can install with one command, that keeps balanced double-entry books, protects PII by design, and produces a yearly packet a CPA can use to prepare tax returns. Recordkeeping support only; not legal or tax advice.

## Definition of Ready
- The story, acceptance criteria and dependencies are written here and in the matching spec.
- The dependency PRs are merged.
- The token cap and estimate are stated.
- Any UI work has an approved mockup (`docs/mockups/`).

## Definition of Done
- Acceptance criteria pass as automated tests (narrow regression), and the full suite is green.
- No raw PII in logs, exports or errors (no-leak suite, once it exists).
- No new outbound network path outside `INetworkGate`.
- Docs and ADRs updated if a decision changed.
- Opened as a draft PR; the owner reviews and merges.

## Ordered backlog

| Order | ID | Story | Key acceptance criteria | Depends on | Cap / est | Phase |
|---|---|---|---|---|---|---|
| 1 | TB-00 | As a maintainer, I want a clean baseline with tests and CI, so later PRs can prove what they fix | Backend test project and CI gate exist; lint passes; vulnerable packages updated; docs stop claiming features that don't exist | none | 60k / 35k | P0 |
| 2 | TB-01 | As a new user, I want a fresh install to start without errors, so I can begin my books | Migrations create the schema; starter chart of accounts for household, Schedule C and 1120-S; UI reaches the API from config | TB-00 | 80k / 50k | P0 |
| 3 | TB-02 | As a user, I want uploads and the app locked to my computer, so nobody else can reach my data | Generated file names (traversal test fails before, passes after); type and size checks; ports bound to 127.0.0.1; no secrets in code; production mode, no stack traces | TB-01 | 80k / 50k | P0 |
| 3 | TB-03 | As a bookkeeper, I want real double-entry books, so my numbers always balance and can't be silently changed | Unbalanced entry rejected; posted entry can't be edited or deleted; reversal nets to zero; locked period rejects posting; all balances sum to 0 | TB-01 | 150k / 100k | P0 |
| 4 | TB-04 | As a user, I want the app to stay offline unless I switch an integration on, so I can trust it | All outbound calls go through `INetworkGate`, default deny; attempts logged; build fails if other code opens a socket; ports and adapters in place | TB-03 | 100k / 65k | P0 |
| 4 | TB-07 | As a sole proprietor, S-corp owner or household, I want categories that match my tax form, so my CPA gets the right lines | Versioned maps per form and year; 1120-S lines 19/20 fixed; meals and mileage fields; 1099 vendor flag with threshold as data | TB-03 | 120k / 80k | P1 |
| 5 | TB-04b | (Candidate, see ADR 0010) As a user, I want account and tax ID numbers masked before any import, so raw PII is never stored | Detect and mask account, routing and TIN; logs clean; one no-leak test | TB-04 | ~60k est | P0 |
| 6 | TB-05 | As a user, I want to import a bank CSV or OFX, review each row and post it, so I don't type transactions by hand | Staged rows; preview and categorize; duplicates flagged; only reviewed rows post as balanced entries | TB-03, TB-04 | 120k / 80k | P0 |
| 6 | TB-10 | As a non-expert, I want a guided setup wizard, so I can set up my books in minutes | Matches approved mockup; shell to feature components; later screens wait for TB-05, TB-08, TB-09 | TB-01, TB-02, TB-07 | 150k / 100k | P1 |
| 7 | TB-06 | As a user uploading W-2s and statements, I want PII detected, masked and encrypted everywhere | Masked UI, logs and exports; encrypted fields and vault; no-leak suite passes | TB-04, TB-05 | 150k / 100k | P1 |
| 8 | TB-08 | As a CPA, I want a yearly packet built from the ledger, so I can prepare the return | Trial balance, P&L, balance sheet, GL, tax-line and 1099 summaries; CSV + XLSX; one zipped packet per year; masked by default | TB-06, TB-07 | 150k / 100k | P1 |
| 8 | TB-11 | As a non-technical user, I want one command to install and start the app, with no Docker | Single command per OS; encrypted single-file store; passphrase setup | TB-04, TB-06 | 150k / 110k | P2 |
| 8 | TB-14 | As a user, I want receipts read on my own computer, so I don't type them in | Local OCR only; always reviewed before posting; behind `IStatementImporter` | TB-05, TB-06 | 150k / 100k | P2 |
| 8 | TB-15 | As a maintainer, I want safe slots for integrations and AI, so the app can grow without risking PII | One example integration and an AI helper that sees only tokenized text; both off by default; kill switch | TB-04, TB-06 | 120k / 80k | P2 |
| 9 | TB-09 | As a bookkeeper, I want to reconcile to my bank statement, so I know the books match the bank | Match ledger to statement ending balance; mark reconciled; lock month | TB-05, TB-08 | 100k / 70k | P1 |
| 9 | TB-12 | As a prospective user, I want a plain trust statement, so I know what the app never does with my data | README trust statement, privacy and security docs; each claim linked to the test that proves it | TB-06, TB-11 | 60k / 35k | P1 |
| 9 | TB-13 | As a user, I want encrypted backups and a safe restore | Backup to folder or USB; restore tested | TB-11 | 100k / 65k | P2 |

Same order number means the items can run in parallel.

## Refinement notes
- TB-12 is P1 in value but waits on TB-11 (P2), because the trust statement must describe the one-command install and the encrypted file.
- TB-07 can start right after TB-03 while P0 finishes.
- TB-04b exists only if the owner accepts ADR 0010 Option A.
- Library choices in TB-06, TB-11 and TB-14 need the owner's approval.

## Open questions for the owner
1. ADR 0010: mask before import (Option A, recommended) or keep the order (Option B)?
2. Household mode: same app with a household chart of accounts, or a separate simplified view? (Assumed: same app, starter chart picked in the wizard.)
