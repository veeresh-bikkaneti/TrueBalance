# Wiki Log

Append-only chronological record of ingests, queries, and lint passes.
Parse recent entries with: `grep "^## \[" wiki/log.md | tail -5`

## [2026-11-24] init | Scaffolded LLM Wiki structure
- Created immutable `raw/` directory and `wiki/` structure (`index.md`, `log.md`, `overview.md`, `sources/`, `entities/`, `concepts/`, `analyses/`).

## [2026-11-25] ingest | Code map
- Added entity pages for the ledger, uploads, charts, data, the backend host, and the frontend shell.
- Added concept pages for double-entry invariants, the event log, and the PII boundary.
- Each code fact on those pages uses a repo pointer and a checked blob hash.

## [2026-11-26] update | Ports and network gate (prompt 04)
- Added [[network-gate]]: ports in Application, default-deny `LocalNetworkGate`, module-scan adapter registration, `IAiAssist` interface with no implementation.
- Renamed `ILocalFileParser` to `IStatementImporter` and `ISecureUploadStore` to `IDocumentVault`; moved the Excel export behind `IReportExporter`.
- Updated the checked hashes and line anchors on every page that cites `Program.cs`, the upload store, the parser, the architecture tests, or `docs/architecture/modules.md`.

## [2026-11-26] update | PII masking core (prompt 04b)
- Added [[pii-masking]]: domain `PiiPolicy` behind `IPiiMasker`, four local detectors, last-4 plus HMAC token, masked console logs and upload error bodies, id-only transaction references.
- Moved the upload route to `UploadEndpoints`; the folder watcher stores the generated id instead of the name and path.
- Added `docs/privacy/masking-coverage.md` and the no-leak test that gates prompt 05. Refreshed the checked hashes on every page citing changed files.
