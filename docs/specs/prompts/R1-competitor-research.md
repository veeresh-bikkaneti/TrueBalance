# TrueBalance prompt R1: How QuickBooks and open-source bookkeeping tools solve our problems

Phase: Research (runs now, in parallel with 02) | Branch: `docs/r1-competitor-research` | Token cap: 300k | Estimate: 200k | Max helper seats: 4 researchers + 3 council seats + 1 reviewer
Depends on: nothing. Output is docs only. Prompt 03 is blocked until this merges with its ledger and chart-of-accounts sections, so finish those first; the rest feed 04b to 15 (see "Where this feeds in").

You are working in the TrueBalance repo. This prompt is self-contained. Do this one docs-only PR. Write no app code.

## Step 0: repo check (do this before anything else)

Run `git fetch origin && git status -sb && git log --oneline -3 origin/main` and paste it. Create the branch from `origin/main`. Read, in this order: `AGENTS.md`, `docs/research/review.md`, `docs/research/verified-facts.md`, `docs/specs/prompts/INDEX.md`, ADRs 0003 to 0011. Do not re-research what `verified-facts.md` already settles; cite it.

## Goal (sharpen)

Find the best proven designs for the problems TrueBalance faces, so good ideas can be adopted (as designs, not copied code) before each build step. Output: one comparison matrix, one ranked "ideas to adopt" list mapped to TrueBalance prompts and ADRs, every claim sourced and vote-tagged.

## Tools to study

Required: QuickBooks Online and QuickBooks Desktop (Intuit's own docs and help pages), GnuCash, Akaunting, Manager.io, Ledger, hledger, Beancount (with Fava), Firefly III, ERPNext, Odoo Community (accounting). You may add up to 3 others if they solve a topic better (for example KMyMoney, Skrooge, Wave, Xero); say why each was added.

## Topics (one matrix row each)

1. Double-entry ledger model: entry and line schema, signed vs debit/credit columns, immutability, reversals, periods and close/lock.
2. Chart of accounts: templates by entity type (household, sole proprietor, S-corp), numbering, sub-accounts.
3. Statement import: formats (CSV, OFX/QFX, CAMT, PDF), column mapping profiles, staging/review before posting, duplicate detection rules.
4. Bank reconciliation: workflow, matching keys, statement-balance lock, how mismatches are shown.
5. Categorization rules: payee rules, rule priority, learning from past choices, how users review suggestions.
6. Tax mapping: how accounts map to Schedule C, Form 1120-S lines and household budgets; whether maps are versioned per tax year.
7. Accountant/CPA export: what an accountant actually receives (trial balance, GL, adjusting entries, accountant copy, file formats).
8. Offline and local storage: file format, encryption at rest, single-file vs server database, multi-device.
9. Backups and restore: automatic vs manual, encryption, versioning, restore testing.
10. PII handling: how account numbers, tax IDs and attachments are stored, masked or encrypted; what goes to any AI or cloud feature.
11. Plugin and integration architecture: extension points, importers as plugins, how network access is gated or disclosed.

## How to run this (orchestration, pstack)

- The main session only plans, merges evidence and verifies. Research goes to 4 researcher seats on cheaper models, in parallel, each with a clean context: seat A QuickBooks; seat B GnuCash, KMyMoney-style desktop tools and Manager.io; seat C Ledger, hledger, Beancount and Fava; seat D Akaunting, Firefly III, ERPNext and Odoo. Each seat covers all 11 topics for its tools.
- Each seat returns rows for one shared evidence pack (`docs/research/r1-evidence.md`): tool, topic, finding in one sentence, source URL, source type (primary = the tool's own docs, code or maintainers; secondary = anything else), date read.
- Council: 3 seats on cheaper models read only the evidence pack, not the web: a CPA/bookkeeper seat (does this help a CPA file?), an architecture seat (does it fit ADR 0003 and 0011: one local app, ports and adapters, in-process events?), a privacy seat (does it fit ADR 0005 and 0010: no raw PII to AI, masking before import, offline by default?). Each seat ranks ideas and votes adopt, adapt or skip. Re-vote only where seats disagree. A mid-tier reviewer model accepts or rejects the final report.
- pstack steps, in order (inline if pstack isn't installed): sharpen (restate the goal above in 3 lines), statechart (only if a topic is a flow, for example import -> review -> post, draw how each leading tool does it), swarm (the seat split above), architect (where each adopted idea sits in TrueBalance's modules), arena (for the 3 riskiest ideas, two options each, pick one with one reason), how (step plan), tdd (not code: for each adopted idea write the acceptance test the build prompt should add), unslop (all prose).
- Use the fewest seats that cover the work. If a seat finishes its tools early, do not start new research; report.

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

## License and copying rules

- Record each tool's license from its own repository or site, with a link and vote tag.
- Adopt designs, workflows and data-model ideas only. Do not copy code, schemas, text or icons from any project. This matters most for GPL and AGPL projects (record which ones are): TrueBalance must not include their code.
- QuickBooks is proprietary: learn from public docs only; never use screenshots or text from it in the repo.
- If an idea can only be done by reusing a library, name the library and its license and flag it for Veeresh's approval.

## Safety

- No accounts, sign-ups, trials or logins on any product. Public docs and public source only.
- Never download and run another project's code. Read it on the web or in a shallow clone without running it.
- No real personal or financial data anywhere in the output.

## Deliverables (one draft PR, docs only)

1. `docs/research/r1-competitor-research.md`:
   - Answer first: the top 5 ideas to adopt, one line each.
   - Comparison matrix: rows = the 11 topics, columns = tools, each cell one short phrase plus a source link.
   - Ranked ideas list, 15 to 30 items: idea, which tools do it, adopt/adapt/skip vote with counts, which TrueBalance prompt or ADR it changes (00 to 15, 04b, ADR numbers), the acceptance test it implies, effort (S/M/L), license note.
   - "Changes to our plan": for each prompt or ADR that should change, one line on what and why. Propose ADR drafts as `docs/adr/00NN-*.md` with Status: Proposed only if a decision is needed; do not edit build prompts.
   - Licenses table.
2. `docs/research/r1-evidence.md` (the evidence pack, all rows kept).
3. Add the new files to `docs/README.md`.

## Where this feeds in

- Ledger model, periods and chart of accounts: before 03 starts.
- PII handling: 04b and 06.
- Import formats, mapping profiles, dedupe, categorization rules: 05.
- Tax mapping: 07. CPA export: 08. Reconciliation: 09. Wizard flows: 10.
- Storage and encryption: 11. Backups: 13. Plugins and integrations: 15.

## Budget (token economy)

- Cap 300k, estimate 200k. Say both before you start, and give a per-seat budget (about 35k per researcher, 15k per council seat).
- Stop and ask Veeresh before you pass the cap. Do not push past it on your own.
- Report tokens used (main plus every seat) and estimated cost with the result.

## Git and PR rules

- Conventional Commits (`docs: ...`). No `Co-Authored-By` or any AI trailer. No "Generated with" line in the PR body.
- No rebase, no force push, no `git merge`. Draft PR only; Veeresh merges.
- PR body: "Before:" paragraph, "After:" paragraph, one sentence on what it adds, short "How" paragraph.

## Report back (pstack:teach + i-have-adhd)

Answer first in one line (done, blocked, or needs a decision), then 3 to 5 bullets: the top 3 ideas, which build prompts change, PR link, tokens used vs cap and estimated cost, and the one decision you need from Veeresh (if any). Explain any new concept in one plain sentence.
