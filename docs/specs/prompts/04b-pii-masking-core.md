# TrueBalance prompt 04b: PII masking core (runs before statement import)

Phase: P0 | Branch: `feat/pii-masking-core` | Token cap: 90k | Estimate: 60k | Max helper seats: 2
Depends on: Prompt 04 merged. Prompt 05 must not merge until this PR's no-leak test passes.

You are working in the TrueBalance repo. This prompt is self-contained. Do this one PR only.
Decision record: `docs/adr/0010-pii-masking-before-import.md` (Accepted). Read it first.

## Task

Statement import (05) is the first step that takes in real statements. Before it lands, raw account numbers and tax IDs must never reach the database, logs or error messages.

Scope:
1. Domain policy `PiiPolicy` behind a narrow port `IPiiMasker` (Application layer). The importer in 05 only calls the port; it holds no detection logic.
2. Pluggable detectors (one class per type, registered through DI): bank account number, ABA routing number (check digit), SSN, EIN/ITIN. Local patterns only. No AI, no network.
3. Non-destructive masking. For each detected value keep: the last 4 digits (display and matching key), and a keyed fingerprint (HMAC-SHA256 with a local key) so two rows with the same number still match. Never store the full value. Do not alter amounts, dates or payee text that is not a detected value.
4. Log and error filter: a redaction enricher so raw detected values never reach logs; error responses never echo input values.
5. Document coverage and gaps in `docs/privacy/masking-coverage.md`: which types are detected, known false positives and negatives, and what waits for 06 (cards, DOB, addresses, encryption, vault).
6. Contributor warning in that doc and in `AGENTS.md`: do not run real statements before prompt 06 merges.

Acceptance criteria (tests first, synthetic fixtures only, no real data):
- No-leak test: fake SSN, EIN, account and routing numbers go through the masker and a stub importer path; assert none of the raw values appear in any database text column, in captured logs, or in error bodies.
- Detection tests per type, including false-positive checks (invoice numbers, phone numbers, amounts).
- Matching test: two rows with the same fake account number produce the same last-4 and fingerprint; different numbers with the same last 4 produce different fingerprints.
- Fixtures live in one shared folder so 06 extends them instead of copying.

Out of scope: encryption at rest, the document vault, card/DOB/address detectors and the full no-leak suite (all 06). Re-measure the 05 and 06 estimates after this PR and report them.

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

- The main session only plans, integrates and verifies. Building, writing and review go to helpers on cheaper models, or to a Council Panel: one shared evidence pack, parallel clean-context seats, scoped re-votes on disagreements only. A Sonnet-class reviewer accepts or rejects the work. Never name a specific model in code, docs or commits.
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

## Facts

Any external fact (IRS rule, standard, library behavior) in code comments, docs or the PR body gets an inline source link and a tag: [Confidence: High | Vote: 3/3]. 3+ agreeing primary sources = High; 2 agreeing or 1 primary = Moderate; 1 secondary or conflicting = Low; 0 sources = leave the claim out. Not legal or tax advice: frame IRS items as recordkeeping for a CPA.

## Git and PR rules

- Start from the latest `main` (after the previous prompt's PR is merged). Branch name is in the header.
- Conventional Commits. No `Co-Authored-By` or any AI trailer. No "Generated with" line in the PR body.
- No rebase, no force push, no `git merge`. Use cherry-pick or `--ff-only` if you need to sync.
- Open a draft PR only. Veeresh reviews and merges; never merge.
- PR body: a "Before:" paragraph, an "After:" paragraph, one sentence on what the change does, then a short "How" paragraph, then the test commands run with pass counts.

## Report back (pstack:teach + i-have-adhd)

Answer first in one line (done, blocked, or needs a decision), then 3 to 5 bullets: what changed, PR link, tests run with exact counts, tokens used vs cap and estimated cost, and the one decision you need from Veeresh (if any). Explain any new concept in one plain sentence.
