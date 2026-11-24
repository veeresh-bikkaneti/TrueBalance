# OfflineCPA tasks: honest status

Checked items are in the code and covered by a passing check (test, lint or build). Unchecked items are not implemented. Repair work is ordered in `../docs/backlog/product-backlog.md` and `../docs/specs/prompts/INDEX.md`.

## 1. Domain (backend)
- [x] `Account`, `Transaction`, `JournalEntry` entities exist.
- [x] EF Core model configured for the three entities (PostgreSQL provider).
- [ ] EF migrations (none exist; the app cannot create a fresh database).
- [ ] Starter chart of accounts (no seed data).

## 2. Services and endpoints (backend)
- [x] Polling background service that scans an input directory every 30 s and moves PDFs to `processed/` after inserting one `Transaction` row per file.
- [x] PDF text extraction with PdfPig, offline, via `ILocalFileParser`.
- [x] XLSX export endpoint with ClosedXML (date, description, file path only).
- [ ] Mapping extracted text to transactions (extracted text is discarded).
- [ ] Double-entry posting, balancing, locking (no code).

## 3. Web client (frontend)
- [x] 1120-S dashboard screen renders.
- [x] Upload button and export button call the API.
- [x] Configurable API base URL (`VITE_API_URL`, default same-origin `/api` with a Vite dev proxy to `http://localhost:8080`; API port is 8080 locally and in Docker).
- [ ] Local directory configuration screen.
- [ ] Categorized transaction view (nothing to show).

## 4. Security QA
- [ ] No cloud leakage is a design goal, not a verified property; there are no tests proving it.
- [ ] Ports bound to loopback only; no auth; uploads unencrypted; DB password hardcoded.
