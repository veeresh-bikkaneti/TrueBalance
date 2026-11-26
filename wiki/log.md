# Wiki Log

Append-only chronological record of ingests, queries, and lint passes.
Parse recent entries with: `grep "^## \[" wiki/log.md | tail -5`

## [2026-11-24] init | Scaffolded LLM Wiki structure
- Created immutable `raw/` directory and `wiki/` structure (`index.md`, `log.md`, `overview.md`, `sources/`, `entities/`, `concepts/`, `analyses/`).

## [2026-11-25] ingest | Code map
- Added entity pages for the ledger, uploads, charts, data, the backend host, and the frontend shell.
- Added concept pages for double-entry invariants, the event log, and the PII boundary.
- Each code fact on those pages uses a repo pointer and a checked blob hash.
