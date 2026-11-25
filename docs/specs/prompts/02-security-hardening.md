# TrueBalance prompt 02 (revised 2026-11-25): Finish the security hardening branch

Phase: P0 | Branch: `fix/security-hardening-v2` (new, replaces PR 26) | Token cap: 50k | Estimate: 32k | Max helper seats: 1 builder + 1 reviewer
Depends on: PR 28 (ledger core) merged. Main already builds; PR 27 fixed the seeder.

You are working in the TrueBalance repo. This prompt is self-contained. Do this one PR only. PR 26 (`fix/security-hardening`, one commit `e4fb349`) conflicts with main and cannot be updated without a merge, which the git rules forbid. So rebuild it on a new branch and open one new draft PR that replaces it. PR 26 showing as conflicting is expected: do not stop for it, do not ask for "Update branch" on PR 26, and do not edit PR 26's branch.

## Step 0: repo check (do this before anything else)

Run and paste the output in your first message:
```
git fetch origin && git status -sb && git log --oneline -5 origin/main
git rev-list --left-right --count origin/main...HEAD
dotnet build OfflineCPA/OfflineCPA.sln -c Release && dotnet test OfflineCPA/tests/TrueBalance.Tests
```
Stop and report if the working tree is dirty, if `main` does not build, or if `git log origin/main --oneline | grep -i ledger` shows PR 28 is not on main yet.

Then rebuild the branch (no merge, no rebase, no force push):
```
git switch -c fix/security-hardening-v2 origin/main
git cherry-pick e4fb349
```
Resolve the conflicts by hand, then `git cherry-pick --continue`:
- `OfflineCPA/src/Infrastructure/Services/JsonChartOfAccountsSeeder.cs`: keep main's version. Main already compiles; drop the branch's older seeder change.
- `OfflineCPA/src/Backend/Program.cs`: keep everything main has (ledger services in DI and the `/api/ledger/` routes from PR 28) and add PR 26's changes: no hardcoded connection-string fallback (throw if it is missing), the upload options in DI, and the rewritten `/api/upload` handler.
Paste `git diff --name-only --diff-filter=U` (must be empty) and the build result after the resolve. Read `AGENTS.md`, ADR 0010 and ADR 0011 before you plan.

## Task

PR 26 already does most of the original prompt 02 (generated upload names, extension, type and size allowlist, loopback-only ports, no published Postgres, secrets from `.env`, Production mode with ProblemDetails, filename-only logging). Two checks fail on its head (e4fb349). Fix them and close the remaining gaps.

Scope:
1. Build fix (tested on CI): `Program.cs` line 77 uses `HttpContext.RequestAborted` inside the `/api/upload` minimal API lambda, which fails with CS0120. Take a `CancellationToken` (or `HttpContext`) parameter in the lambda and pass it on. 
2. GitGuardian (tested: it flags a generic password in `OfflineCPA/docker-compose.yml` for commit e4fb349). The current file reads the password from `.env`, so the hit is most likely the old hardcoded value removed by this commit, which is still in git history (inferred). Confirm which line it points at. If it is the removed line: do not rewrite history (no force push); tell Veeresh it is a burned local dev default, that nobody should reuse it, and that he can mark the finding as resolved in GitGuardian. If anything secret-like is still in the tree, remove it.
3. Gaps against the original prompt (check each against the tests; add a test where one is missing): rejected names cover `../../x.csv`, `..\x.csv`, an absolute path and a null byte; the folder-watch service logs the generated ID, not the original filename (a filename can hold a person's name); an unhandled exception in Production returns ProblemDetails with no stack trace.
4. Note for 04b, do not fix here: the upload endpoint still writes the original filename into `Transaction.Description` and the full stored path into `ReferenceDocumentPath`. List this in the PR body under "Left for 04b".

Acceptance criteria:
- The new draft PR's body says "Replaces #26" in its first line. Do not close PR 26; Veeresh closes it.
- `backend`, `frontend` and GitGuardian checks are green on the new head (or GitGuardian is explained as history-only, per item 2).
- `dotnet test` passes with the new tests; paste the counts.
- `docker compose config` (with a sample `.env`) shows no 0.0.0.0 bindings and no published 5432.

Out of scope: encryption (06), auth (11), dependency bumps (D1).

## Context (read first)

Repo: github.com/veeresh-bikkaneti/TrueBalance (public). App code lives in `OfflineCPA/`: .NET 8 solution (`src/Domain`, `src/Application`, `src/Infrastructure`, `src/Backend` minimal API), tests in `tests/TrueBalance.Tests` (xUnit), Vite + React frontend (`frontend/`), docker-compose with Postgres 15 for development. TrueBalance is a free, open-source, offline bookkeeping app for small businesses and households. It must balance books and produce reports a CPA can use to file taxes. It must never connect to the internet unless the user turns on an integration, and no AI model (local or remote) may ever receive raw PII.

State of main on 2026-11-25 (commit 81f584f):
- Merged: 00 baseline tests and CI (PR 13), ADR 0011 local modular monolith (PR 14), agent refresh and wiki (PR 15), 01 initial EF migration and seedable charts of accounts for household, sole proprietor and S-corp (PR 23).
- Main is green at 23c9245 or later; PR 27 already fixed the seeder build error. PR 28 adds the ledger core (JournalEntry header, Posting lines, events, `/api/ledger/` routes in `Program.cs`).
- The current model calls the entry header `Transaction` and its lines `JournalEntry`, with a signed `Amount` (positive debit, negative credit). Tables: Accounts, Transactions, JournalEntries.
- ADR 0011: one local app. The backend is a modular monolith (modules: Ledger, Import, PII/Masking, Vault, TaxMaps, Reporting, Settings) enforced by project or namespace rules and architecture tests. Events are in-process only, with an append-only event log table that holds masked data only. No broker, no microservices, no microfrontends. Docker is for developers only.
- ADR 0010: PII masking core (04b) lands before statement import (05).

Target architecture (all prompts build toward it):
- Domain (no I/O): Account, JournalEntry with Posting lines (sum must be 0), Period, Reversal; tax maps as versioned data; PII policy (classify, mask, tokenize).
- Application: use cases (ImportStatement, CategorizeTransaction, PostEntry, Reconcile, BuildReport) depending only on interfaces.
- Ports in Application, adapters in Infrastructure: ILedgerStore, IStatementImporter, IDocumentVault, IReportExporter, INetworkGate (default deny, every outbound call must pass it and is logged), IAiAssist (no implementation by default; tokenized input only).
- UI: React shell -> feature components (Setup wizard, Transactions, Reconcile, Reports, Settings/Network log). Approved mockup: `docs/mockups/setup-wizard.html` (if you can't open it, the flow is: who the books are for -> lock books with passphrase -> import statement with PII masked -> review and post balanced entries -> CPA reports).
- Reports and exports are read-only views over posted ledger entries. Never generate a report from raw imported files.

## Budget (token economy)

- AGENTS.md on main now exists. If it sets a token budget, use whichever cap is stricter.
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
- Release chain (applies to the new PR, not to PR 26): before Veeresh merges, the branch must contain the latest `main`. If GitHub shows the PR as behind, stop and tell him; he uses GitHub's "Update branch". If it shows a conflict, stop and tell him which files conflict; "Update branch" cannot fix a conflict. You never run `git merge` yourself.
- Conventional Commits. No `Co-Authored-By` or any AI trailer. No "Generated with" line in the PR body.
- No rebase, no force push, no `git merge`. Use cherry-pick or `--ff-only` if you need to sync.
- Open a draft PR only. Veeresh reviews and merges; never merge.
- PR body: a "Before:" paragraph, an "After:" paragraph, one sentence on what the change does, then a short "How" paragraph, then the test commands run with pass counts.

## Report back (pstack:teach + i-have-adhd)

Answer first in one line (done, blocked, or needs a decision), then 3 to 5 bullets: what changed, PR link, tests run with exact counts, tokens used vs cap and estimated cost, and the one decision you need from Veeresh (if any). Explain any new concept in one plain sentence.
