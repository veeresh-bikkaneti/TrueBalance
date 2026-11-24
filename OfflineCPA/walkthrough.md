# OfflineCPA walkthrough: what runs today

This document describes only what the code in `OfflineCPA/` actually does. Aspirational items live in `docs/roadmap.md`. The project plan lives in `../docs/specs/prompts/INDEX.md`.

## What exists

- A .NET 8 solution with four projects under `src/`: `Domain` (entities), `Application` (one parser interface), `Infrastructure` (EF Core model, PdfPig PDF text extractor), `Backend` (minimal API with three endpoints: `POST /api/upload`, `GET /api/transactions`, `GET /api/export`).
- A Vite + React frontend under `frontend/` with a single dashboard screen.
- A `docker-compose.yml` that runs Postgres 15, the backend and the frontend in containers.
- An xUnit test project under `tests/TrueBalance.Tests` with one smoke test per layer.

## How to run it

Backend (requires .NET 8 SDK and a reachable PostgreSQL instance):

```bash
cd OfflineCPA
dotnet run --project src/Backend
```

The connection string comes from `appsettings.json`, or the `DefaultConnection` fallback compiled into `Program.cs`. There are no EF migrations, so on a fresh database `/api/transactions` and `/api/export` fail until the schema is created manually.

Frontend (requires Node 20+):

```bash
cd OfflineCPA/frontend
npm ci
npm run dev
```

Full stack:

```bash
cd OfflineCPA
docker compose up
```

## Known gaps (verified, not claimed as features)

- The frontend hardcodes `http://localhost:5030/api` for API calls; the backend listens on another port (8080 in Docker, per launchSettings locally). The UI cannot reach the API until prompt 01 fixes this.
- `/api/upload` saves files with the raw client filename (path traversal) and stores uploads unencrypted in a Docker volume. There is no authentication.
- PDF text is extracted and then discarded; no transactions or journal entries are created from file content. Upload records a row with the filename and date only.
- The Excel export contains only date, description and file path. There is no categorization, no OCR, no PII masking.
- PostgreSQL, the API and the UI ports are published on all interfaces, not just loopback.
- The tests added by this PR are wiring smoke tests only, not behavior tests.

See `../docs/research/review.md` for the full findings list and `../docs/adr/` for the accepted target architecture.
