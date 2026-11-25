# TrueBalance prompt A1: feature board and architecture consistency review

Phase: Architecture (docs) | Branch: `docs/feature-board-arch-review` (new) | Token cap: 120k | Estimate: 80k | Seats: master orchestrator (main session) + 3 reviewer seats on cheaper models (Seat UI: frontend; Seat API: middle tier and domain; Seat DATA: database, events, PII) + 1 mid-tier reviewer who accepts each finding
Depends on: D2 merged. Can run beside 03b (different branch, docs only). Review main plus open PRs 26 and 28 as they stand.

You are working in the TrueBalance repo. This is a review and planning PR: docs only. Write no app code and change no ADR's status. If a change to an accepted ADR looks right, write it as a new ADR with status **Proposed** for Veeresh to decide.

## Step 0
Run `git fetch origin && git status -sb && git log --oneline -3 origin/main && gh pr list --state open` and paste it. Stop if the tree is dirty.

## Read first (in this order)
`AGENTS.md`, `docs/README.md`, `docs/specs/prompts/INDEX.md`, every ADR in `docs/adr/`, `docs/backlog/product-backlog.md`, `docs/roadmap/roadmap.md`, `docs/research/r1-competitor-research.md` (sections on module placement and statecharts), `.agent/ARCHITECTURE.md`, `.agent/skills/architecture/SKILL.md`, `.agent/skills/clean-code/SKILL.md`, `OfflineCPA/architecture_research.md`, `OfflineCPA/implementation_plan.md`.

## The owner's target (his words, 2026-11-25)
"so the code can scale, microfrontend for UI, make it vite or react page, middle tier .net asp core which i think is already set and backend postgres db. EDA, Clean Architecture, Domain driven, solid, etc., design patterns, clean code, i think all of it is already outlined in instruction files."

## Known conflicts with accepted ADRs (re-check, then handle as stated)
- **Microfrontends:** ADR 0011 (accepted 2026-11-24) says one React app, shell plus feature components, "No microfrontends". Do not build or plan real microfrontends. Arena (a) one Vite + React app split into feature modules with enforced boundaries, against (b) runtime microfrontends (for example Module Federation). Score both on: offline single install, one maintainer, bundle size, test cost, and a later split. Whichever wins, put the boundary rules for (a) in the plan, because they are what make (b) possible later. If (b) wins, also write `docs/adr/0012-microfrontends.md` with status Proposed, superseding 0011's frontend line only.
- **Postgres:** ADR 0006 says the default store is one encrypted SQLite file, with Postgres as an optional adapter. The code today runs on Postgres (Npgsql, docker-compose). Check that every data access goes through ports (`ILedgerStore` and the like) so both stay possible. Arena "SQLite default, Postgres optional" against "Postgres default"; if Postgres wins, write a Proposed ADR superseding 0006 and say what it does to one-command install (prompt 11).
- **EDA:** ADR 0011 allows in-process events and an append-only event log only. No broker. Check PR 28 and 03b against this.

## Part 1: architecture consistency review
Each seat checks its layer against the ADRs, `AGENTS.md` and the skills above, and returns findings rows only: rule (with ADR or file), where (`path:line`), evidence (quoted line or command output), severity (Blocker / Should / Nice), fix (one line), and which prompt should carry the fix (existing number or "new").
- **Seat UI:** `OfflineCPA/frontend`. Vite and React versions, folder layout (today `src/App.tsx` plus `components/Dashboard.tsx`), whether a shell plus `src/features/<feature>/` layout exists, cross-feature imports, where API calls live, state management, how the setup-wizard mockup in `docs/mockups/` maps to features. Propose the target layout: `src/shell/`, `src/features/<feature>/{api,components,routes,state,tests}`, a shared `src/ui/` kit, an import-boundary lint rule (arena two ESLint options, cite licenses).
- **Seat API:** `OfflineCPA/src`. ASP.NET Core version, minimal API endpoints in `Program.cs` versus thin endpoints over use cases, Clean Architecture dependency direction (ADR 0003), DDD building blocks per module (aggregates, value objects such as `Money`, domain events, repositories as ports), SOLID breaks, patterns in use or missing (use case / CQRS-lite, repository, unit of work, strategy for tax maps, adapter for importers, specification for rules), clean-code smells.
- **Seat DATA:** EF Core model and migrations, Postgres-only features that would block SQLite (ADR 0006), indexes for years of postings, event log shape, PII columns and masking (ADR 0005, 0010), secrets in config.
- **Orchestrator:** merges rows, removes duplicates, runs the two arenas above, and the reviewer accepts or rejects each row. Write `docs/architecture/consistency-review.md`: answer first, findings table sorted by severity, the two arena results, and a "scale" section that says what scale means for this app (records per year, file size, report time) with each figure sourced or marked `[Inferred]`.

## Part 2: feature board
Write `docs/roadmap/feature-board.md` and a static view `docs/roadmap/feature-board.html` in the same style as `docs/roadmap/build-map.html` (no external scripts, works offline). One row per feature from the backlog (TB-00 to TB-15, 04b, 03b and the research items), with columns: feature, TB id, module (ADR 0011), UI feature folder, API endpoints, use cases, aggregates, events, tables, prompt file, PR (number and state read from GitHub), status (Done / In PR / Next / Blocked / Later), tests, owner decision pending. Group by status, then phase. Fill each cell from the repo or GitHub; where nothing exists yet, write "planned" and the prompt that will add it. Link the board from `docs/README.md` and `AGENTS.md` section 2.

## Statecharts
Add one statechart for a feature's life on the board (Later -> Next -> In PR -> Done, with Blocked) and one for a frontend request through the layers (feature component -> feature api -> endpoint -> use case -> aggregate -> port -> adapter -> database, with the event path).

## Source and evidence rules
Apply the owner's rules exactly as written in the Facts section of `03-ledger-core.md`. Code findings are `[Tested]` with `path:line`. Library and version claims need a direct link and a tag. Analysis is `[Inferred]`.

## Gates (paste output)
- Every Blocker row has a `path:line` and a fix prompt.
- `grep -c 'Status: Accepted' docs/adr/*.md` is unchanged from main.
- The HTML board opens with no network (no `http` in `src=` or `href=` for scripts or styles).
- The reviewer's accept/reject table covers every row.

## Budget, git and report
- Cap 120k, estimate 80k; say both first with per-seat budgets (about 20k per seat, 15k reviewer); stop and ask before passing the cap; report measured tokens or "Data not available."
- Conventional Commits (`docs:`). No `Co-Authored-By` or AI trailer. No "Generated with" line. No rebase, force push or `git merge`. Open a draft PR; Veeresh merges.
- Report (pstack:teach + i-have-adhd): answer first, then 3 to 5 bullets: Blocker count and the top three, the two arena picks (and whether a Proposed ADR was written), features by status, full head SHA, tokens vs cap. End with the one decision Veeresh must make.
