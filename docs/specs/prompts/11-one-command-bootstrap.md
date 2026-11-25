# TrueBalance prompt 11: One-command bootstrap with encrypted local store

Phase: P2 | Branch: `feat/one-command-bootstrap` | Token cap: 150k | Estimate: 110k | Max helper seats: 3
Depends on: Prompts 04 and 06 merged.

You are working in the TrueBalance repo. This prompt is self-contained. Do this one PR only.

## Task

Make install as simple as one command, with no Docker or Postgres required by default.

Scope:
1. `EncryptedSqliteStore` implementing `ILedgerStore` (SQLCipher-compatible provider; arena the library choice and cite sources). One file per set of books, opened with the user passphrase. Keep `PostgresStore` as an optional adapter selected in settings.
2. Serve the built frontend from the backend so the app is one local process on `127.0.0.1`; add a per-launch local session token so other local apps can't call the API.
3. Bootstrap scripts: `bootstrap.ps1` (Windows) and `bootstrap.sh` (Linux, macOS later). Each checks prerequisites, builds, creates `.env` with generated secrets, starts the app and opens the browser. Clear, plain-language console output with numbered steps.
4. Keep docker-compose as an advanced option, updated to the new defaults.
5. Data folder layout documented; file permissions restricted to the current user.

Acceptance criteria:
- Fresh machine walk-through: one command to running app, recorded in the PR.
- Test: SQLite store passes the same ledger test suite as Postgres (shared contract tests).
- Test: API rejects requests without the session token.

Out of scope: auto-update (offline app has none).

## Context (read first)

Repo: github.com/veeresh-bikkaneti/TrueBalance (public). App code lives in `OfflineCPA/`: .NET 8 solution (`src/Domain`, `src/Application`, `src/Infrastructure`, `src/Backend` minimal API), Vite + React frontend (`frontend/`), docker-compose with Postgres 15. TrueBalance is a free, open-source, offline bookkeeping app for small businesses and households. It must balance books and produce reports a CPA can use to file taxes. It must never connect to the internet unless the user turns on an integration, and no AI model (local or remote) may ever receive raw PII.

Review findings this work fixes (verified by running the code on 2026-11-19, commit 406eec1):
- No EF migrations, so `/api/transactions` and `/api/export` return 500 on a fresh database.
- `/api/upload` saves using the raw client filename, so a crafted filename writes files outside the upload folder (path traversal, confirmed).
- Uploads are stored unencrypted; no auth; Postgres, API and UI ports are published on all interfaces; DB password is hardcoded in docker-compose.yml and Program.cs; compose runs in Development mode and leaks stack traces.
- PDF text is extracted and thrown away; CSV is ignored. No journal entries are ever created; the Excel export has only date, description and file path.
- `TaxLineItem` enum targets Form 1120-S only, and labels Travel/Meals as lines 20/21 (current 1120-S instructions: line 19 is Energy Efficient Commercial Buildings Deduction, line 20 is Other Deductions).
- Frontend hardcodes `http://localhost:5030/api`; backend listens on 5159 (launchSettings) or 8080 (Docker).
- Zero backend tests; 3 eslint errors; npm audit flags axios (high), form-data (high), follow-redirects (moderate).
- Docs (`walkthrough.md`, `architecture_research.md`) claim features that do not exist.

Target architecture (all prompts build toward it):
- Domain (no I/O): Account, JournalEntry with Posting lines (sum must be 0), Period, Reversal; tax maps as versioned data; PII policy (classify, mask, tokenize).
- Application: use cases (ImportStatement, CategorizeTransaction, PostEntry, Reconcile, BuildReport) depending only on interfaces.
- Ports in Application, adapters in Infrastructure: ILedgerStore, IStatementImporter, IDocumentVault, IReportExporter, INetworkGate (default deny, every outbound call must pass it and is logged), IAiAssist (no implementation by default; tokenized input only).
- UI: React shell -> feature components (Setup wizard, Transactions, Reconcile, Reports, Settings/Network log). Approved mockup: `docs/mockups/setup-wizard.html` (if you can't open it, the flow is: who the books are for -> lock books with passphrase -> import statement with PII masked -> review and post balanced entries -> CPA reports).
- Reports and exports are read-only views over posted ledger entries. Never generate a report from raw imported files.

## Budget (token economy)

- Check the repo for an AGENTS.md (or similar) token budget. At the time of writing none exists. If one exists now, use whichever cap is stricter.
- Cap and estimate for this prompt are in the header. Say both before you start.
- Stop and ask Veeresh before you pass the cap. Do not push past it on your own.
- Report tokens used (main plus every helper) and estimated cost with the result.

## How to run this (orchestration)

- The main session only plans, integrates and verifies. Building, writing and review go to helpers on cheaper models, or to a Council Panel: one shared evidence pack, parallel clean-context seats, scoped re-votes on disagreements only. A mid-tier reviewer model accepts or rejects the work. Never name a specific model in code, docs or commits.
- Every helper seat has real overhead, so use the fewest seats that cover the work (the header gives a maximum). Small edits stay in the main session.
- Use the Edit tool for file changes (not shell rewrites).
- pstack steps, in order (if pstack isn't installed, do each step inline): sharpen (restate the goal and acceptance criteria in 3 lines), statechart (states and transitions of any flow you touch), swarm (split into parallel sub-tasks with owners), architect (where each piece sits in the layers), arena (two options for the riskiest choice; pick one with one reason), how (step plan), tdd (write the failing test first; run only the narrow regression needed to clear the gates, not the full suite unless the gate needs it), unslop (all prose: PR body, docs, comments).

## Engineering rules

- SOLID and clean code; loose coupling; reuse before adding; shell -> component layering in the UI.
- Domain has no I/O and no framework references. Application depends on interfaces only. Infrastructure implements them.
- A working mockup before any UI code. If the change has UI, put the mockup in `docs/mockups/` first and link it in the PR.
- Money is `decimal`, never float. Round only at posting and display.
- Offline rule: add no network calls, CDNs, web fonts, telemetry or update checks. No AI SDKs.
- PII rule: never put real or realistic personal data in code, fixtures, logs or screenshots. Use obviously fake values generated by a test helper.
- Keep docs honest: describe only what the code does.

## Facts (owner's source and evidence rules)

Any external fact (IRS rule, standard, library behavior, version number) in code comments, docs or the PR body follows these rules. Not legal or tax advice: frame IRS items as recordkeeping for a CPA.

#### 1. Grounding & Verification
- **Search First:** Actively use web search to ground responses in current information. Prioritize official documentation, primary datasets, source code, or user-provided files before secondary sources.
- **Deduplicate Sources:** Multiple articles or copies republishing the same underlying origin count as a single source.
- **Fact-Check Everything:** Independently verify every claim, number, version number, setting name, and reference.

#### 2. Zero Fabrication
- Base all analysis strictly on verified, provided, or cited real-world data.
- Never invent, estimate, or extrapolate missing figures.
- If required data cannot be verified or found, explicitly state: **"Data not available."** Drop unverified claims rather than guessing.

#### 3. Confidence Scoring & Phrasing
Evaluate each verified claim using the following scale and match your phrasing to the confidence level:
- **High (3+ agreeing primary sources):** Use definitive phrasing (e.g., *"confirms," "establishes"*).
- **Moderate (2 agreeing sources, or 1 primary source):** Use measured phrasing (e.g., *"indicates," "suggests"*).
- **Low (1 secondary source such as a blog/forum, or conflicting sources):** Explicitly flag the uncertainty (e.g., *"sources conflict," "preliminary reports suggest"*).
- **None (0 verifiable sources):** Omit the claim entirely.

#### 4. Source Attribution & Formatting
- **Inline Citations:** End every sourced claim or data point with a direct source link and a confidence tag showing agreeing sources over total sources checked—for example: `[High 3/3]` or `[Low 1/2]`.
- **Label Original Work:** Clearly separate verified facts from your own work by explicitly labeling analytical interpretations as **[Inferred]** and direct executions/checks as **[Tested]**.

#### 5. Statistical Rigor
When presenting quantitative data or analysis:
- Show all calculations step-by-step.
- Name the exact statistical method used (e.g., mean, median, standard deviation, linear regression).
- State the sample size ($n$) and exact timeframe for every metric.
- Cite the specific dataset or source for every input number.

Applied to this PR: tag format is `[High 3/3]` (agreeing sources / sources checked). Test results you ran are [Tested]; design choices and arena picks are [Inferred].

## Git and PR rules

- Start from the latest `main` (after the previous prompt's PR is merged). Branch name is in the header.
- Conventional Commits. No `Co-Authored-By` or any AI trailer. No "Generated with" line in the PR body.
- No rebase, no force push, no `git merge`. Use cherry-pick or `--ff-only` if you need to sync.
- Open a draft PR only. Veeresh reviews and merges; never merge.
- PR body: a "Before:" paragraph, an "After:" paragraph, one sentence on what the change does, then a short "How" paragraph, then the test commands run with pass counts.

## Report back (pstack:teach + i-have-adhd)

Answer first in one line (done, blocked, or needs a decision), then 3 to 5 bullets: what changed, PR link, tests run with exact counts, tokens used vs cap and estimated cost, and the one decision you need from Veeresh (if any). Explain any new concept in one plain sentence.
