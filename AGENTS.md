# AGENTS.md: TrueBalance

Read this file, `docs/README.md`, and `docs/specs/prompts/INDEX.md` first. Everything needed to build TrueBalance is in this repo.

## 1. Mission & Non-Negotiable Constraints
- **What TrueBalance is**: Free, open-source, offline bookkeeping app for small businesses and households (`OfflineCPA/`). Keeps balanced books, protects PII by design, installs with one command, and exports CPA-ready tax reports (recordkeeping support only, not tax or legal advice).
- **Offline & PII-by-Design**: Zero new outbound network paths outside `INetworkGate`. Zero raw PII in logs, errors, exports, committed fixtures, or prompts sent to any AI model.
- **Placeholders**: Never pre-fill real vendor or business names in templates or sample data; use generic placeholders.

## 2. Repo State & Active Queue (as of 2026-11-24)
- **Code**: `OfflineCPA/` (Python 90.3%, C# 4.7%, TypeScript 2.8% / React shell, CSS 1.7%). Commit `406eec1` builds, but fresh install lacks migrations and ledger core (`docs/research/review.md`).
- **Plan & Backlog**: 17 PRs across 3 phases in `docs/specs/prompts/` (`INDEX.md`), `docs/roadmap/roadmap.md`, `docs/roadmap/build-map.html`, and `docs/backlog/product-backlog.md`.
- **Active Gate**: ADR 0010 accepted — prompt `04b` (PII masking core) runs before `05`; never run real statements before `06` merges. Next PR: `docs/specs/prompts/00-baseline.md`.

## 3. Token Economy & Orchestration (First Priority)
- **Pick Tier Up Front**: Classify each PR before starting:
  - **S (Small)**: Baseline (`00`), docs, config, single-module fix — **250k** uncached+out, **2M** cached, **25 turns**, **2 cheaper seats (0 re-votes)**, **$0.25 USD**.
  - **M (Medium)**: Standard feature, DB migration, UI screen — **600k** uncached+out, **6M** cached, **50 turns**, **3 cheaper seats (1 re-vote)**, **$0.75 USD**.
  - **L (Large/Critical)**: PII masking (`04b`), encryption (`06`), ledger core, OCR (`14`) — **1.2M** uncached+out, **12M** cached, **80 turns**, **4 cheaper seats (2 re-votes)**, **$2.00 USD**.
- **Separate Cached vs. Fresh**: Track uncached input, cached input, output tokens, turn count, and USD separately. Cap turns to stop context-reread loops.
- **80% Warning & Hard Stop**: Warn Veeresh at 80% of any cap (tokens, turns, or USD). Stop and ask Veeresh before passing 100%.
- **Main Session Scope**: Keep the main session strictly to **planning, integrating, and verifying** (<20% of PR budget).
- **Council Panel Seating Loop**: Send building, writing, and review seats to **cheaper models** (1 shared evidence pack, parallel clean-context seats, scoped re-votes within the tier limit). Never name a specific model; always say "cheaper models".

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
- **Strict Citation Rule (Mandatory)**: Every factual claim, number, quote, or code reference in `wiki/` MUST include an inline pointer to the exact raw file and location (e.g., `(source: raw/filename.md#section-or-line)` or `(source: raw/doc.pdf, p. 4)`). Never summarize away exact figures without linking back.
- **Ingest**: Read new `raw/` file -> write summary in `wiki/sources/` with inline citations -> update `wiki/entities/` and `wiki/concepts/` using `[[wikilinks]]` and citations -> update `wiki/index.md` -> append `## [YYYY-MM-DD] ingest | Title` to `wiki/log.md`.
- **Query**: Read `wiki/index.md` first -> read relevant `wiki/` pages -> follow `(source: raw/...)` back into `raw/` for exact numbers/code -> cite both wiki page and raw file. File reusable syntheses in `wiki/analyses/`.
- **Lint**: Run `python3 .agent/scripts/wiki_lint.py` to check broken `(source: raw/...)` paths, broken `[[wikilinks]]`, and orphan pages, plus review for uncited claims and contradictions.

## 6. Git, Architecture & Verification Rules
1. **One Spec, One Draft PR**: Build in `INDEX.md` order on the branch named in the spec. Open as draft. Never merge, approve, or enable auto-merge (Veeresh merges).
2. **Git Hygiene**: Conventional Commits. No `Co-Authored-By` or AI trailers in commits; no "Generated with" lines in PRs. No rebase, no force push, no `git merge`.
3. **Architecture**: Show a working mockup in `docs/mockups/` before UI code. Enforce Clean Architecture, SOLID, loose coupling, React shell to feature components, and ADRs (change only via superseding ADR).
4. **Facts & Evidence**: Check official docs, source code, or repo files first (`docs/research/verified-facts.md`). Tag sourced lines `[High 3/3]`, `[Moderate 2/2]`, or `[Low 1/1]` with a source link; mark own findings `inferred` or `tested`. Drop unsourced claims.

## 7. Owner-Only Decisions
- Merging any PR.
- Library picks in prompts `06` (encryption), `11` (encrypted store, packaging), and `14` (OCR).
- Any screen differing from the approved setup-wizard mockup.
- Exceeding any token, turn, or USD cap.
