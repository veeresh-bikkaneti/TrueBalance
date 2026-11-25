# TrueBalance prompt D2: sync the repo plan and AGENTS.md with the current prompts

Phase: Chore (docs) | Branch: `docs/sync-plan-2026-11-25` (new) | Token cap: 40k | Estimate: 30k | Seats: main session on a cheaper model + 1 mid-tier reviewer
Depends on: PR 29 (A1) merged, because both edit `AGENTS.md` and `docs/README.md`. Branch from `origin/main` after that merge.

You are working in the TrueBalance repo. Docs and agent-instruction files only. Write no app code.

## Inputs
Veeresh gives you a folder path where he unzipped `truebalance-prompts-2026-11-25.zip` (outside the repo). Check every file against `MANIFEST.sha256` in that folder with `sha256sum -c MANIFEST.sha256` and paste the result; stop on any mismatch. Copy each verbatim into `docs/specs/prompts/` (overwrite the older copy if one exists): `INDEX.md`, `02-security-hardening.md`, `03-ledger-core.md`, `03b-ledger-gaps.md`, `04-ports-network-gate.md`, `04b-pii-masking-core.md`, every `05`-`15` file he attaches, `D1-dependabot-groups.md`, `D2-sync-plan-and-agents.md`, `C1-one-pr-sync-and-security.md`, `A1-feature-board-arch-review.md`, `R1-competitor-research.md`, `R1b-merge-research.md`, `R1b-fix-irs-part2.md`, `R1b-fix2-direct-file.md`. Stop and list what is missing if `INDEX.md` or any of 02, 03, 03b, A1 is not in the folder. Do not copy `MANIFEST.sha256` into the repo.

## Step 0
Run `git fetch origin && git status -sb && git log --oneline -3 origin/main` and paste it. Stop if the tree is dirty. Branch from the latest `origin/main`.

## What the architect review found (2026-11-25)
- `docs/specs/prompts/` on main is the 2026-11-19 set. The revised 02, 03 and 04b, the research prompts, D1 and the owner's source rules exist only outside the repo. PR 28 was built from the old 03 for that reason.
- `AGENTS.md` section 2 is stale: "as of 2026-11-24", commit `406eec1`, "Next PR: 00-baseline", and a language breakdown that is not checked.
- `AGENTS.md` section 3 tiers (250k / 600k / 1.2M) conflict with the cap each prompt states.
- `raw/` is committed. `.gitignore` drops pdf, csv, tsv, xls, xlsx, ofx, qfx and qbo files, but any other file put in `raw/` (a pasted statement, a W-2 transcript) would be committed.

## Changes
1. Copy the attached prompts (above). Do not edit them.
2. `AGENTS.md` section 2: replace with current state read from GitHub (not from this prompt): main head and CI result, merged PRs (00, 01, R1), open PRs and their check results, and the next step per `INDEX.md`. Drop the language breakdown.
3. `AGENTS.md` section 3: add two lines: "The cap and estimate in each prompt win over these tiers. The tiers apply only to work with no prompt." and "Before the first edit or subagent call, state the cap and your estimated token and USD range. Report uncached input, cached input, output tokens, turns and USD with every milestone result." Then add the footer every agent ends a milestone report with: `Budget: uncached <n> | cached <n> | output <n> | turns <n> | $<n> of $<cap> (<p>%)`. A number the tool cannot measure is written "Data not available." and never estimated. When a known number has crossed 80%, the footer starts with WARNING.
4. `AGENTS.md` section 6.4: replace the tag sentence with: "Follow the owner's source and evidence rules exactly as written in `docs/specs/prompts/02-security-hardening.md` (section Facts). Tags look like `[High 3/3]`, `[Moderate 1/1]`, `[Low 1/2]`; analysis is `[Inferred]`, direct checks are `[Tested]`."
5. `AGENTS.md` section 5 and `.agent/skills/llm-wiki/SKILL.md` section 1: add "`raw/` holds public project sources only (docs, specs, papers, public web pages). Never put user statements, tax forms (W-2, W-9, 1099), receipts or any PII in `raw/` or `wiki/`. Private sources go in `raw/private/`, which git ignores." Add `raw/private/` to `.gitignore`.
6. `.agent/scripts/wiki_lint.py`: add one check that fails when a file in `raw/` (outside `raw/private/`) or `wiki/` matches an SSN pattern (`\b\d{3}-\d{2}-\d{4}\b`), an EIN pattern (`\b\d{2}-\d{7}\b`) or a 13 to 19 digit card-like run. Add a test fixture only under the scripts folder, using obviously fake numbers that still match. Add a second fixture with one broken `(source: raw/...)` path and one broken `[[wikilink]]`, so the existing checks have a committed regression test too. tdd: show each check failing on its fixture, then passing on the real tree.
7. `AGENTS.md`: add a section "Council of experts and roles" from the owner's direction (2026-11-25): a council of experts on cheaper models, each with a clear persona, including a Product Owner who verifies facts on the IRS portal and domain experts; a verdict from an advisor on the strongest model available; a master orchestrator on a cheaper model that delegates the work; and a reviewer in the architect role using the `/pstack`, `/potato-mode` and `/gstack` key commands. The project must work with any AI provider, so name tiers, never a vendor or model. Open the section with three definitions: **cheaper model** = the fastest low-cost model your tool offers; **mid-tier model** = a stronger general model; **frontier model** = the strongest model your tool offers. AGENTS.md already says seats run on cheaper models and the main session plans, integrates and verifies, and `.agent/agents/orchestrator.md` and `.agent/agents/product-owner.md` already define generic personas. Reuse them: link those files from the table, and in `product-owner.md` add only the duty to verify tax and filing facts on irs.gov. Do not duplicate their text. Write a short role table:
   - **Master orchestrator** (cheaper model): splits the work, briefs each seat, merges results, never does the heavy lift itself.
   - **Council of experts** (cheaper models, one persona each, with a written brief): a **Product Owner** who verifies every tax or filing fact against irs.gov (the IRS portal) and owns acceptance criteria; **domain experts** such as a CPA or bookkeeper, a small-business owner, a household filer, and privacy and security. Each seat states its persona, its sources and its vote.
   - **Reviewer** (architect role): checks the work against the ADRs and this file, using the `/pstack`, `/potato-mode` and `/gstack` key commands, and accepts or rejects each item.
   - **Advisor** (frontier model): gives the final verdict on points the council or reviewer leave split.
   Replace the section 3 line "Never name a specific model; always say 'cheaper models'" with "Name tiers, never a vendor or model, so any AI tool can run this repo: cheaper models for the orchestrator and seats, a mid-tier model for the architect reviewer, a frontier model for the advisor." Also make `.agent/agents/orchestrator.md` tool-neutral: replace vendor-specific tool and model names (its built-in agents table names two vendor models) with the tier words, and keep the rest. List the key commands with one line each on when to use them. If `/potato-mode` is not installed or documented in this repo, write "Data not available." next to it rather than guessing what it does, and say so in your report.
8. `.agent/rules/TRUEBALANCE.md` (P1) must not contradict P0 after these edits. Section 3: replace "cheaper models" seating with one line pointing to the new role section in `AGENTS.md` (cheaper seats, mid-tier architect reviewer, frontier advisor). Section 4: replace "Stack scope: `OfflineCPA/` (Python, C#, TypeScript/React)" with the stack read from the repo (C# .NET 8 backend, React + TypeScript frontend; Python only in `.agent/scripts/`), marked `[Tested]` with the command that showed it.
9. `docs/backlog/product-backlog.md`: update TB-03's acceptance criteria and dependency to match the revised 03 and 03b. Add rows for 03b, D2 and A1 with their caps.

## Source and evidence rules
Apply the owner's rules exactly as written in `02-security-hardening.md` (Facts section). Anything you state about repo state is `[Tested]` with the command that showed it.

## Gates (paste output)
- `diff` of each copied prompt against the unzipped folder is empty.
- `python3 .agent/scripts/wiki_lint.py` passes on the tree and fails on the fixture.
- `git check-ignore raw/private/x.md` prints the path.
- `grep -n '406eec1\|Next PR: .*00-baseline' AGENTS.md` prints nothing.
- `grep -n 'Master orchestrator\|Product Owner\|Advisor\|potato-mode' AGENTS.md` shows the new role section.
- `grep -n 'Never name a specific model\|Python, C#' AGENTS.md .agent/rules/TRUEBALANCE.md` prints nothing.
- `grep -n 'every milestone\|Budget: uncached' AGENTS.md` shows the reporting line and the footer.
- `grep -rn -i -E 'opus|sonnet|haiku|gpt-|gemini' AGENTS.md .agent/rules .agent/agents docs/specs/prompts --exclude=D2-sync-plan-and-agents.md` prints nothing.

## Budget, git and report
- Cap 40k, estimate 30k; say both first; stop and ask before passing the cap; report measured tokens or "Data not available."
- Conventional Commits (`docs:` and `chore(agent):`). No `Co-Authored-By` or AI trailer. No "Generated with" line. No rebase, force push or `git merge`. Open a draft PR; Veeresh merges.
- Report (pstack:teach + i-have-adhd): answer first, then 3 to 5 bullets: files copied, AGENTS.md changes (role table included), gate output, full head SHA, tokens vs cap.
