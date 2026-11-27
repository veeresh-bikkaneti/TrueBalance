---
checked:
  OfflineCPA/frontend/src/main.tsx: 3d7150da80e43e3650342aa4758fa8b74e95d6d6
  OfflineCPA/frontend/src/App.tsx: 306f2244e219b1665f4b5690625fd8068725ca70
  OfflineCPA/frontend/src/components/Dashboard.tsx: e97260fda939957facc5a7c2db1486a35d7cfa4f
  OfflineCPA/frontend/vite.config.ts: 05af52d3878d1d225177e0ccbb3e5b9499f3680e
  docs/adr/0003-clean-architecture-ports-adapters.md: c9c7256eea1b992b1d2443ea30a1356ec0de5388
  AGENTS.md: d4872b7b8ff1b9704748aee4d5756e10b01624ae
---
# Frontend shell

What exists today is one React screen. `main.tsx` mounts `App` (repo: OfflineCPA/frontend/src/main.tsx#L1-L10). `App` renders a sidebar titled OfflineCPA with two links, "1120-S Dashboard" and "Security Rules", and it imports one component, `Dashboard` (repo: OfflineCPA/frontend/src/App.tsx#L2-L28).

## Dashboard

`Dashboard` reads `VITE_API_URL` and otherwise uses `/api` (repo: OfflineCPA/frontend/src/components/Dashboard.tsx#L13). It loads `GET /transactions`, posts a file to `POST /upload`, and downloads `GET /export` (repo: OfflineCPA/frontend/src/components/Dashboard.tsx#L20-L74). The Vite dev server proxies `/api` to `http://localhost:8080` (repo: OfflineCPA/frontend/vite.config.ts#L7-L14).

## Planned, not built

ADR 0003 (accepted) names the target UI: a React app shell and feature components for a setup wizard, transactions, reconcile, reports, and settings / network log (repo: docs/adr/0003-clean-architecture-ports-adapters.md#L15). Those screens are not what `App` renders today (repo: OfflineCPA/frontend/src/App.tsx#L6-L28).

## Change it safely

A new screen needs a mockup in `docs/mockups/` before UI code (repo: AGENTS.md#L72). Keep API calls on the `VITE_API_URL` base. Do not add a CDN, a web font, or telemetry. See [[backend-host]] and [[uploads]].
