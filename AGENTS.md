# AGENTS.md: TrueBalance

Read this file, `docs/README.md`, and `docs/specs/prompts/INDEX.md` first. Everything needed to build TrueBalance is in this repo.

## 1. Mission & Non-Negotiable Constraints
- **What TrueBalance is**: Free, open-source, offline bookkeeping app for small businesses and households (`OfflineCPA/`). Keeps balanced books, protects PII by design, installs with one command, and exports CPA-ready tax reports (recordkeeping support only, not tax or legal advice).
- **Offline & PII-by-Design**: Zero new outbound network paths outside `INetworkGate`. Zero raw PII in logs, errors, exports, committed fixtures, or prompts sent to any AI model.
- **Placeholders**: Never pre-fill real vendor or business names in templates or sample data; use generic placeholders.

## 2. Repo State & Active Queue (as of 2026-11-25)
- **Main**: `18db3df` (`feat: double-entry ledger core with locked posted entries (TB-03) (#28)`). Check runs on that commit: `backend` success, `frontend` success, `Dependabot` failure. [Tested] `gh api repos/veeresh-bikkaneti/TrueBalance/commits/18db3df/check-runs`
- **Merged**: 00 baseline tests and CI (PR 13), 01 migrations and charts of accounts (PR 23), R1 competitor research (PR 27), ADR 0010 (PR 12), ADR 0011 (PR 14), agent refresh and wiki (PR 15), A1 architecture consistency review (PR 29), ledger core TB-03 (PR 28).
- **Open**: PR 26 is a draft for 02 security (`backend` failure, `frontend` success, `GitGuardian Security Checks` failure). Seventeen Dependabot PRs are open: #1, #2, #3, #5, #6, #8, #9, #10, #16, #17, #18, #19, #20, #21, #22, #24, #25. [Tested] `gh pr list --json number,statusCheckRollup` on 2026-11-25: #25 fails `backend` and `frontend` (GitGuardian success); #24 fails `backend` (`frontend` and GitGuardian success); #9 and #16 through #22 pass `backend`, `frontend` and GitGuardian; #1, #2, #3, #5, #6, #8 and #10 report GitGuardian success only.
- **Plan**: `docs/specs/prompts/` (`INDEX.md`), `docs/roadmap/roadmap.md`, `docs/roadmap/build-map.html`, `docs/roadmap/feature-board.md` (static view: `docs/roadmap/feature-board.html`), and `docs/backlog/product-backlog.md`. Architecture consistency review: `docs/architecture/consistency-review.md`.
- **Active gate**: ADR 0010 accepted. Prompt `04b` (PII masking core) runs before `05`. Do not run real statements before `06` merges.
- **Next step** (`INDEX.md` merge order): C1 is this sync (D2 plus the 02 security rebuild, one draft PR that replaces #26). After that PR is green, the owner closes #26, then D1 (grouped Dependabot), then 04 and 07 in parallel, then 04b, then 05.

## 3. Token Economy & Orchestration (First Priority)
- **Pick Tier Up Front**: Classify each PR before starting:
  - **S (Small)**: Baseline (`00`), docs, config, single-module fix — **250k** uncached+out, **2M** cached, **25 turns**, **2 cheaper seats (0 re-votes)**, **$0.25 USD**.
  - **M (Medium)**: Standard feature, DB migration, UI screen — **600k** uncached+out, **6M** cached, **50 turns**, **3 cheaper seats (1 re-vote)**, **$0.75 USD**.
  - **L (Large/Critical)**: PII masking (`04b`), encryption (`06`), ledger core, OCR (`14`) — **1.2M** uncached+out, **12M** cached, **80 turns**, **4 cheaper seats (2 re-votes)**, **$2.00 USD**.
- **Separate Cached vs. Fresh**: Track uncached input, cached input, output tokens, turn count, and USD separately. Cap turns to stop context-reread loops.
- **80% Warning & Hard Stop**: Warn Veeresh at 80% of any cap (tokens, turns, or USD). Stop and ask Veeresh before passing 100%.
- **Main Session Scope**: Keep the main session strictly to **planning, integrating, and verifying** (<20% of PR budget).
- **Council Panel Seating Loop**: Send building, writing, and review seats to **cheaper models** (1 shared evidence pack, parallel clean-context seats, scoped re-votes within the tier limit). Name tiers, never a vendor or model, so any AI tool can run this repo: cheaper models for the orchestrator and seats, a mid-tier model for the architect reviewer, a frontier model for the advisor.
- The cap and estimate in each prompt win over these tiers. The tiers apply only to work with no prompt.
- Before the first edit or subagent call, state the cap and your estimated token and USD range. Report uncached input, cached input, output tokens, turns and USD with every milestone result.
- End every milestone report with this footer. A number the tool cannot measure is written "Data not available." and never estimated. When a known number has crossed 80%, the footer starts with WARNING: `Budget: uncached <n> | cached <n> | output <n> | turns <n> | $<n> of $<cap> (<p>%)`

## Council of experts and roles

**cheaper model** = the fastest low-cost model your tool offers. **mid-tier model** = a stronger general model. **frontier model** = the strongest model your tool offers.

Reuse the personas already written. Do not copy their text into this file. Master orchestrator: `.agent/agents/orchestrator.md`. Product Owner: `.agent/agents/product-owner.md`.

| Role | Tier | What they do |
| --- | --- | --- |
| **Master orchestrator** | cheaper model | Splits the work, briefs each seat, merges results, never does the heavy lift itself. |
| **Council of experts** | cheaper models, one persona each, with a written brief | A **Product Owner** who verifies every tax or filing fact against irs.gov (the IRS portal) and owns acceptance criteria. **Domain experts** such as a CPA or bookkeeper, a small-business owner, a household filer, and privacy and security. Each seat states its persona, its sources and its vote. |
| **Reviewer** | architect role, mid-tier model | Checks the work against the ADRs and this file, using `/pstack`, `/potato-mode` and `/gstack`, and accepts or rejects each item. |
| **Advisor** | frontier model | Gives the final verdict on points the council or reviewer leave split. |

Key commands, and when to use them:
- `/pstack`: on every PR, before opening it. Run sharpen, statechart, swarm, architect, arena, how, tdd, unslop, then the report.
- `/gstack`: when scoping a feature, auditing security, or running browser or QA checks (`/office-hours`, `/autoplan`, `/cso`, `/qa`, `/review`).
- `/potato-mode`: Data not available.

## 4. Mandatory Skills & Frameworks
Load and obey these local skills in `.agent/skills/` alongside `pstack` (Cursor plugin) and `gstack` (`garrytan/gstack`):
1. **`i-have-adhd`** (`.agent/skills/i-have-adhd/SKILL.md`): Always on until Veeresh says "stop adhd mode" or "normal mode". Lead with next action on Line 1, restate state every turn, number multi-step tasks (cap 5 visible items), give concrete time estimates, make wins visible, matter-of-fact errors, zero preamble/recap/closers.
2. **`ponytail`** (`.agent/skills/ponytail/SKILL.md`): Default `full` level. Smallest complete change that solves the task. No unasked abstractions, wrappers, or boilerplate. Never cut trust-boundary validation, data-loss error handling, security, or tests for non-trivial logic.
3. **`karpathy-guidelines`** (`.agent/skills/karpathy-guidelines/SKILL.md`): Think Before Coding (surface assumptions), Simplicity First, Surgical Changes (touch only what the task requires), Goal-Driven Execution (verifiable checks).
4. **`llm-wiki`** (`.agent/skills/llm-wiki/SKILL.md`): Local Markdown knowledge base (`raw/` + `wiki/`) replacing Graphify.
5. **`pstack` & `gstack`**:
   - Every PR runs `pstack`: `sharpen` -> `statechart` -> `swarm` -> `architect` -> `arena` -> `how` -> `tdd` (narrow regression only, just enough to clear gates) -> `unslop` on all prose -> report back with `pstack:teach` + `i-have-adhd`.
   - Use `gstack` (`/office-hours`, `/autoplan`, `/cso`, `/qa`, `/review`) when scoping features, auditing security, or running browser/QA checks.

## 5. Local LLM Wiki (`raw/` + `wiki/`)
Based on [Karpathy's LLM Wiki](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f) with strict source citations:
- **Immutable `raw/`**: Never modify, rename, or delete files inside `raw/`.
- `raw/` holds public project sources only (docs, specs, papers, public web pages). Never put user statements, tax forms (W-2, W-9, 1099), receipts or any PII in `raw/` or `wiki/`. Private sources go in `raw/private/`, which git ignores.
- **Strict Citation Rule (Mandatory)**: Every factual claim, number, quote, or code reference in `wiki/` MUST include an inline pointer to the exact raw file and location (e.g., `(source: raw/filename.md#section-or-line)` or `(source: raw/doc.pdf, p. 4)`). Never summarize away exact figures without linking back.
- **Ingest**: Read new `raw/` file -> write summary in `wiki/sources/` with inline citations -> update `wiki/entities/` and `wiki/concepts/` using `[[wikilinks]]` and citations -> update `wiki/index.md` -> append `## [YYYY-MM-DD] ingest | Title` to `wiki/log.md`.
- **Query**: Read `wiki/index.md` first -> read relevant `wiki/` pages -> follow `(source: raw/...)` back into `raw/` for exact numbers/code -> cite both wiki page and raw file. File reusable syntheses in `wiki/analyses/`.
- **Lint**: Run `python3 .agent/scripts/wiki_lint.py` to check broken `(source: raw/...)` paths, broken `[[wikilinks]]`, and orphan pages, plus review for uncited claims and contradictions.

## 6. Git, Architecture & Verification Rules
1. **One Spec, One Draft PR**: Build in `INDEX.md` order on the branch named in the spec. Open as draft. Never merge, approve, or enable auto-merge (Veeresh merges).
2. **Git Hygiene**: Conventional Commits. No `Co-Authored-By` or AI trailers in commits; no "Generated with" lines in PRs. No rebase, no force push, no `git merge`.
3. **Architecture**: Show a working mockup in `docs/mockups/` before UI code. Enforce Clean Architecture, SOLID, loose coupling, React shell to feature components, and ADRs (change only via superseding ADR).
4. **Facts & Evidence**: Check official docs, source code, or repo files first (`docs/research/verified-facts.md`). Follow the owner's source and evidence rules exactly as written in `docs/specs/prompts/02-security-hardening.md` (section Facts). Tags look like `[High 3/3]`, `[Moderate 1/1]`, `[Low 1/2]`; analysis is `[Inferred]`, direct checks are `[Tested]`.

## 7. Owner-Only Decisions
- Merging any PR.
- Library picks in prompts `06` (encryption), `11` (encrypted store, packaging), and `14` (OCR).
- Any screen differing from the approved setup-wizard mockup.
- Exceeding any token, turn, or USD cap.
