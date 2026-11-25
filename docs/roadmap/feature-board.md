# Feature board

Status of every backlog item on `main` `23c9245`, read on 2026-11-25. PR state is from `gh pr list`. "planned" means the prompt will add it. Module names are ADR 0011.

Life of a row: Later, Blocked, Next, In PR, Done. See the statechart in `docs/architecture/consistency-review.md`.

## Done

| Feature | TB | Module | UI folder | API | Use cases | Aggregates | Events | Tables | Prompt | PR | Status | Tests | Owner decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Baseline, CI, honest docs | TB-00 | Settings | planned `src/shell` | none | none | none | none | none | `00-baseline.md` | [#13](https://github.com/veeresh-bikkaneti/TrueBalance/pull/13) merged | Done | backend test project exists | no |
| Migrations, starter chart, API base URL | TB-01 | Ledger | planned `src/features/setup` | `POST /api/books/setup` exists | seeder | `Account` | none | `Accounts` | `01-migrations-coa.md` | [#23](https://github.com/veeresh-bikkaneti/TrueBalance/pull/23) merged | Done | seeder tests on main | no |

## In PR

| Feature | TB | Module | UI folder | API | Use cases | Aggregates | Events | Tables | Prompt | PR | Status | Tests | Owner decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Upload safety, loopback, no secrets in code | TB-02 | Settings | unchanged dashboard | `POST /api/upload` | `ISecureUploadStore.SaveAsync` on the PR | none | none | none new | `02-security-hardening.md` | [#26](https://github.com/veeresh-bikkaneti/TrueBalance/pull/26) draft | In PR | `SecureUploadStoreTests`, `SecureErrorHandlingTests` | no |
| Double-entry ledger, locked posted entries | TB-03 | Ledger | planned `src/features/transactions` | planned on the branch: `POST /api/ledger/entries`, `.../post`, `.../reverse`, `POST /api/ledger/periods/{id}/lock`, `GET /api/ledger/balances` | `CreateDraftEntry`, `PostEntry`, `ReverseEntry`, `LockPeriod`, `GetAccountBalances` | journal header, postings, period | audit row only; domain events are 03b | `JournalEntries`, `Postings`, `Periods`, `LedgerAuditRecords` | `03-ledger-core.md` | [#28](https://github.com/veeresh-bikkaneti/TrueBalance/pull/28) draft `ca2b111` | In PR | `LedgerTests` on that branch | no |

## Next

| Feature | TB | Module | UI folder | API | Use cases | Aggregates | Events | Tables | Prompt | PR | Status | Tests | Owner decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Close ledger gaps: architecture tests, real domain events, drop-table comment | TB-03b | Ledger | none | same ledger routes as TB-03 | same use cases raise events inside the unit of work | same | `EntryPosted`, `EntryReversed`, `PeriodLocked` | event log (today `LedgerAuditRecords`) | not in `INDEX.md`; work stays on PR 28 | [#28](https://github.com/veeresh-bikkaneti/TrueBalance/pull/28) not in `ca2b111` yet | Next | planned: one event row per success, zero on failure, arch tests | no |

## Blocked

| Feature | TB | Module | UI folder | API | Use cases | Aggregates | Events | Tables | Prompt | PR | Status | Tests | Owner decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Ports and network-off gate | TB-04 | Settings | planned `src/features/settings` | planned network log | planned | none | none | planned network log | `04-ports-network-gate.md` | none | Blocked on TB-03 | planned | no |
| Mask account and tax id before import | TB-04b | PII/Masking | none | none | planned `IPiiMasker` | masking policy | none | none new | `04b-pii-masking-core.md` | none | Blocked on TB-04 | planned no-leak | no |
| Staged statement import | TB-05 | Import | planned `src/features/import` | planned | `ImportStatement`, `CategorizeTransaction` | staged row | none until post | planned staging tables | `05-statement-import.md` | none | Blocked on TB-03, TB-04, TB-04b | planned | no |
| PII layer, vault, no-leak suite | TB-06 | PII/Masking, Vault | planned settings | none | planned | vault record | access log, masked | vault is files, not the ledger | `06-pii-layer.md` | none | Blocked on TB-04b, TB-05 | planned | yes: encryption library |
| Tax maps as data | TB-07 | TaxMaps | planned setup step | planned | planned | none; maps are data | none | planned map files | `07-tax-maps.md` | none | Blocked on TB-03 | planned | no |
| Reports and CPA packet | TB-08 | Reporting | planned `src/features/reports` | `GET /api/export` exists as a dump; packet is planned | `BuildReport` | none; read-only | none | none; reads the ledger | `08-reports-cpa-packet.md` | none | Blocked on TB-06, TB-07 | planned | no |
| Reconciliation | TB-09 | Ledger | planned `src/features/reconcile` | planned | `Reconcile` | reconciliation | none | planned | `09-reconciliation.md` | none | Blocked on TB-05, TB-08 | planned | no |
| Setup wizard and shell | TB-10 | Settings | `src/shell` plus feature folders | uses the APIs above | none new | none | none | none | `10-setup-wizard-ui.md` | none | Blocked on TB-02, TB-07 | Cypress smoke exists; feature tests planned | yes: any screen that differs from `docs/mockups/setup-wizard.html` |
| Trust statement | TB-12 | Settings | none | none | none | none | none | none | `12-readme-trust.md` | none | Blocked on TB-06, TB-11 | planned: each claim links to a test | no |

## Later

| Feature | TB | Module | UI folder | API | Use cases | Aggregates | Events | Tables | Prompt | PR | Status | Tests | Owner decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| One-command install, encrypted file, no Docker | TB-11 | Ledger store | planned setup | none | open store | none | none | one SQLite file | `11-one-command-bootstrap.md` | none | Later | planned | yes: encryption library |
| Encrypted backup and restore | TB-13 | Vault | planned `src/features/settings` | planned | planned | backup manifest | none | file copy | `13-backups.md` | none | Later | planned restore test | no |
| Local receipt OCR | TB-14 | Import | planned `src/features/import` | planned | importer adapter | staged row | none | vault plus staging | `14-local-receipt-ocr.md` | none | Later | planned | yes: OCR library |
| Opt-in integration and AI adapters | TB-15 | Settings | planned `src/features/settings` | planned | `INetworkGate`, `IAiAssist` | none | network log | planned log | `15-integration-ai-adapters.md` | none | Later | planned no-leak | no |

## Research items

These are R1 ideas already voted into a prompt. They are not extra PRs. Status is Later until that prompt is Next. Source: `docs/research/r1-competitor-research.md` section 5.

| Id | Idea | Module | Prompt | Status |
| --- | --- | --- | --- | --- |
| R1-1 | Immutable posted entries and reversals | Ledger | 03 | In PR on #28; event shape is 03b |
| R1-2 | Lock date | Ledger | 03 | In PR on #28 |
| R1-3 | Five-root chart, post to leaves only | Ledger | 01, 03 | 01 Done; leaf rule planned in 03 |
| R1-4 | Account open, close, lock | Ledger | 03 | planned |
| R1-5 | Retained earnings from the ledger, not a stored copy | Reporting | 03, 08 | planned |
| R1-6 | Balance check fails loud | Ledger | 03, 09 | planned |
| R1-7 | Encrypted SQLite, loopback, lockfile | Ledger store | 11 | Later |
| R1-8 | Mask before staging | PII/Masking | 04b, 06 | Blocked |
| R1-9 | Staged import queue | Import | 05 | Blocked |
| R1-10 | Duplicate detection | Import | 04b, 05 | Blocked |
| R1-11 | Uncleared, cleared, reconciled | Ledger | 09 | Blocked |
| R1-12 | Document vault separate from the database | Vault | 06, 14 | Later |
| R1-13 | CPA packet | Reporting | 08 | Blocked |
| R1-14 | Encrypted backup and verify | Vault | 13 | Later |
| R1-15 | CSV and OFX importers, no GPL parser library | Import | 05 | Blocked |
| R1-16 | Starter charts per entity | TaxMaps | 01, 07, 10 | 01 Done; rest planned |
| R1-17 | Tax maps as data files | TaxMaps | 07 | Blocked |
| R1-18 | Reconciliation drill-down and undo last period | Ledger | 09 | Blocked |
| R1-19 | Default-deny network gate | Settings | 04, 15 | Blocked |
| R1-20 | Categorization rules on masked text | Import | 05 | Blocked |
| R1-21 | Incognito UI and masked exports | Reporting | 06, 08, 10 | Blocked |
| R1-22 | Unassigned staging, no silent suspense | Import | 03, 05 | planned |
| R1-23 | Hash chain inside `ILedgerStore` | Ledger | 03, 13 | planned |
| R1-24 | Local suggestions behind `IAiAssist` | Settings | 15 | Later |

Counts: Done 2, In PR 2, Next 1, Blocked 9, Later 4. Research rows are not a sixth status; they follow the prompt they belong to.
