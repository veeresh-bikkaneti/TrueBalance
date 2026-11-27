# OfflineCPA architecture: what is built

This document replaces the earlier research report. That report described libraries and steps that were never implemented (Tesseract, AG Grid, Zustand, internal Docker networks, masking middleware); those ideas are recorded in `docs/roadmap.md`. This file describes only the code that exists.

## Current structure

- `src/Domain` — no dependencies. Entities: `Account`, `Transaction`, `JournalEntry`. Enums: `AccountType`. Tax lines are versioned data in `tax-maps/` (`docs/adr/0007`).
- `src/Application` — one port: `ILocalFileParser` (extract text, check extension). Nothing else.
- `src/Infrastructure` — `OfflineCPADbContext` (EF Core model for the three entities, Npgsql provider), `PdfFileParser` (PdfPig page text).
- `src/Backend` — minimal API: `/api/upload` writes the uploaded file and inserts a `Transaction` row; `/api/transactions` lists all rows; `/api/export` writes an XLSX (date, description, file path) with ClosedXML. A `BackgroundService` scans an input directory every 30 seconds and moves PDFs to `processed/` after inserting a row.
- `frontend/` — React + TypeScript + Vite, one dashboard component, axios for HTTP, no routing.
- `tests/TrueBalance.Tests` — xUnit smoke tests asserting each layer is wired and referenced.

## What this is not

- Not a ledger: journal entries and accounts exist as tables, but nothing balances, posts or locks. Accepted target: `docs/adr/0002`.
- Not privacy-hardened: no masking, no encryption, no `INetworkGate`. Accepted targets: `docs/adr/0004`, `docs/adr/0005`, `docs/adr/0010`.
- Not the target store: ADR 0006 accepts an encrypted single-file SQLite store; today's code uses PostgreSQL.

The plan that replaces this prototype, item by item, is `docs/specs/prompts/INDEX.md`.
