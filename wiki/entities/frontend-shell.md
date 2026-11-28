---
checked:
  OfflineCPA/frontend/src/main.tsx: 3d7150da80e43e3650342aa4758fa8b74e95d6d6
  OfflineCPA/frontend/src/App.tsx: 0586494bd4d8b86970a1f26bcdaa1420af6bdafa
  AGENTS.md: d4872b7b8ff1b9704748aee4d5756e10b01624ae
  OfflineCPA/frontend/src/shared/route.ts: 6ea11953515bb220b71f990017860e4496ab4c4e
  OfflineCPA/frontend/src/features/home/route.tsx: cb99d97eb74140f42391fea28fb5c6d69c274529
  OfflineCPA/frontend/src/shell/Shell.tsx: 1670dc645d0062a0bb3453e285e362fc9cfa2e42
  OfflineCPA/frontend/src/shell/useHashPath.ts: 10cd8757d2da966c2282cc5f5398ee38a244e586
  OfflineCPA/frontend/src/shared/api.ts: 8b9911d2b890a3811ff67d722890c963f44e7f5c
  OfflineCPA/frontend/vite.config.ts: c080a07efb89148943871f19f5e05bf14ea32cdb
  OfflineCPA/frontend/.eslintrc.cjs: ce2ca463a55b5470104563a55db8ef1b3e1fa13e
  OfflineCPA/frontend/package.json: 1837c712445ce0b8458f4f0575c47f1716126014
  docs/adr/0003-clean-architecture-ports-adapters.md: c9c7256eea1b992b1d2443ea30a1356ec0de5388
---
# Frontend shell

The app is a small shell with a place for every screen. `main.tsx` mounts `App` (repo: OfflineCPA/frontend/src/main.tsx#L1-L10). `App` collects the screens and hands them to `Shell` (repo: OfflineCPA/frontend/src/App.tsx#L1-L10). Today there is one screen, Home.

## Add a screen

Make a folder. That is the whole trick, and no shared file changes.

1. Draw the mockup in `docs/mockups/` first (repo: AGENTS.md#L72).
2. Add `src/features/<name>/` with your component.
3. Add `src/features/<name>/route.tsx` that exports `route`: `path`, `label`, `order` and `component` (repo: OfflineCPA/frontend/src/shared/route.ts#L4-L13). Copy Home (repo: OfflineCPA/frontend/src/features/home/route.tsx#L1-L4).

`App` finds the file with `import.meta.glob('./features/*/route.tsx', { eager: true })` (repo: OfflineCPA/frontend/src/App.tsx#L4-L6). Vite replaces that call at build time, and the pattern has to be a literal string ([Vite 6 glob import docs](https://github.com/vitejs/vite/blob/v6.4.5/docs/guide/features.md#glob-import-caveats)) [High 1/1]. Pick an unused `path`, and leave gaps in `order` so the next screen fits. Home is `/` at order 0.

## Shell

`Shell` sorts the routes by `order`, builds the nav, shows the matching screen or the not-found page, and passes the sorted routes to the screen (repo: OfflineCPA/frontend/src/shell/Shell.tsx#L6-L32). Addresses look like `#/accounts`, read by `useHashPath`, so there is no router library and no server fallback to set up (repo: OfflineCPA/frontend/src/shell/useHashPath.ts#L1-L9).

## Talking to the backend

Use `api()` from `shared/api.ts`. It is the one place that calls `fetch`, adds `VITE_API_URL` (default `/api`), sends and reads JSON, and throws `ApiError` with the server's `error` text and the status (repo: OfflineCPA/frontend/src/shared/api.ts#L1-L40). It has no file upload support yet, so the screen that needs one adds it there. The Vite dev server proxies `/api` to `http://localhost:8080` (repo: OfflineCPA/frontend/vite.config.ts#L8-L15).

## Import boundaries

ESLint's `no-restricted-imports` keeps features apart: inside a feature, import your own files with `./` and shared code with `../../shared/...`. Keep shared imports in files at the top of your feature folder, because a deeper `../../../shared` path is rejected too. `shared/` may not import `shell/` or `features/` (repo: OfflineCPA/frontend/.eslintrc.cjs#L30-L55). The rule reads `import` and `export ... from` lines, so don't sneak around it with `import()`. ESLint stays on version 8, and one extra override keeps our rule set steady on top of the `typescript-eslint` 8 preset (repo: OfflineCPA/frontend/.eslintrc.cjs#L18-L29).

## Tests

`npm test` runs Vitest with React Testing Library in jsdom (repo: OfflineCPA/frontend/package.json#L6-L11) (repo: OfflineCPA/frontend/vite.config.ts#L16-L19). CI runs it after lint.

## Planned, not built

ADR 0003 (accepted) names the target screens: setup wizard, transactions, reconcile, reports, and settings / network log (repo: docs/adr/0003-clean-architecture-ports-adapters.md#L15). The old one-screen dashboard is gone; its backend routes are untouched.

## Change it safely

Do not add a CDN, a web font, or telemetry. See [[backend-host]] and [[uploads]].
