# Wiki Index

Catalog of all wiki pages. Updated on every ingest and analysis.

## Overview
- [[overview]] — Module map for the code that is on main.

## Sources
<!-- Add ingested raw source summaries here -->

## Entities
- [[ledger]] — Ledger types, store, routes, tests, and what to change with them.
- [[statement-import]] — Queued batches, staged rows, duplicate flags, and posting.
- [[uploads]] — Upload store, PDF parser, folder watcher, and their tests.
- [[pii-masking]] — The 04b masking core: policy, port, detectors, redaction, and the no-leak gate.
- [[charts-of-accounts]] — Seed JSON, seeder, and `/api/books/setup`.
- [[data-and-migrations]] — DbContext, migrations, triggers, and the snapshot test.
- [[backend-host]] — Program wiring, `.env` loading, errors, and compose.
- [[network-gate]] — Ports, the default-deny network gate, and adapter registration.
- [[frontend-shell]] — The screen that exists, and the ADR 0003 layout that is planned.

## Concepts
- [[double-entry-invariants]] — Signed amounts, balance, post, reverse, and period lock.
- [[event-log]] — In-memory events, the dispatcher, and the append-only audit table.
- [[pii-boundary]] — What is masked today, and what 06 still has to do.

## Analyses
<!-- Add saved query syntheses and comparisons here -->
