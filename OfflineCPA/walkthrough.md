# OfflineCPA walkthrough: what runs today

This document describes only what the code in `OfflineCPA/` actually does. Aspirational items live in `docs/roadmap.md`. The project plan lives in `../docs/specs/prompts/INDEX.md`.

## What exists

- A .NET 8 solution with four projects under `src/`: `Domain` (entities), `Application` (parser and chart-seeder interfaces), `Infrastructure` (EF Core model with an initial migration, seed charts of accounts, PdfPig PDF text extractor), `Backend` (minimal API with four endpoints: `POST /api/upload`, `POST /api/books/setup`, `GET /api/transactions`, `GET /api/export`).
- A Vite + React frontend under `frontend/` with a single dashboard screen.
- A `docker-compose.yml` that runs Postgres 15, the backend and the frontend in containers.
- An xUnit test project under `tests/TrueBalance.Tests` with one smoke test per layer.

## How to run it

Backend (requires .NET 8 SDK and a reachable PostgreSQL instance):

```bash
cd OfflineCPA
dotnet run --project src/Backend
```

The connection string comes from the `ConnectionStrings:DefaultConnection` configuration value (environment variable or `OfflineCPA/.env`, loaded at startup). If it is missing, the app fails fast with a clear message; there is no password compiled into the code. An EF Core initial migration ships in `src/Infrastructure/Migrations` and `Database.Migrate()` runs at startup, so a fresh database gets the schema automatically. The API listens on port 8080, both locally (launchSettings) and in Docker (compose maps `127.0.0.1:8080:8080`).

To seed a starter chart of accounts, call `POST /api/books/setup` with `{ "kind": "household" | "sole-proprietor" | "s-corporation" }`. Seeding is idempotent; account codes stay unique.

Frontend (requires Node 20+):

```bash
cd OfflineCPA/frontend
npm ci
npm run dev
```

The frontend reads the API base URL from `VITE_API_URL` and defaults to same-origin `/api`. In `npm run dev`, a Vite proxy forwards `/api` to `http://localhost:8080`, so no CORS change is needed. In Docker, compose sets `VITE_API_URL=http://localhost:8080` at build time.

Full stack: `scripts/init-env.sh` then `docker compose up`. Compose builds the backend connection string with `Host=database` and does not read `CONNECTION_STRING`.

Host run: `docker compose -f docker-compose.yml -f docker-compose.dev.yml up database`, then `dotnet run --project src/Backend`. `CONNECTION_STRING` in `.env` uses `Host=localhost` for that path. The dev override publishes Postgres on `127.0.0.1:5432` only. The default compose file still publishes no database port.

## Known gaps (verified, not claimed as features)

- `/api/upload` stores files under a generated ID with an allowlisted extension and a size cap (default 20 MB), never using the client filename for the path. Uploads are still unencrypted in a Docker volume, and there is no authentication.
- PDF text is extracted and then discarded; no transactions or journal entries are created from file content. Upload records a row with the filename and date only.
- The Excel export contains only date, description and file path. There is no categorization, no OCR, no PII masking.
- Unhandled exceptions return RFC 7807 ProblemDetails with no stack trace (Production is the default; Development is opt-in via `ASPNETCORE_ENVIRONMENT`).
- The tests added by this PR are wiring smoke tests only, not behavior tests.

See `../docs/research/review.md` for the full findings list and `../docs/adr/` for the accepted target architecture.
