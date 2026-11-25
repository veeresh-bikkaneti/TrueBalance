# TrueBalance fix prompts: index

Written 2026-11-19 from the TrueBalance review (`docs/research/review.md`). One prompt per draft PR. Paste each prompt inline into your local agent, in order, after the previous PR is merged. Every prompt carries the full rules block (budget, orchestration, pstack, engineering, facts, git, report format), so each stands alone.

Total: 26 prompts (17 build + 03b + R1, R1b, R1b-fix and R1b-fix2 research + A1 architecture + D1 and D2 chores + C1 combined run) | Remaining work (02 finish onward) caps add up to 2045k | Estimates add up to 1365k tokens.

## Status on 2026-11-25 (revised by the architect review)

| Item | State |
| --- | --- |
| 00 baseline tests and CI | Merged (PR 13) |
| 01 migrations and charts of accounts | Merged (PR 23). Left main's backend build red (`ToHashSetAsync`); fixed on PR 26 |
| ADR 0011 local modular monolith | Merged (PR 14). Prompts 03, 04 and 04b revised to fit it |
| Agent refresh and wiki | Merged (PR 15) |
| 02 security hardening | PR 26 open, draft. Fails to build (CS0120) and trips GitGuardian. Use the revised prompt 02 to finish it |
| Branches already in main | `docs/handoff-plan`, `chore/baseline-tests`, `fix/migrations-and-coa`, `chore/agent-refresh-wiki-skills`, `docs/adr-0011-local-modular-monolith`: 0 commits ahead of main, safe to delete |
| Dependabot | 17 open single-package PRs; replace with grouped updates (D1) |

## Merge order from here (release chain)

Status 2026-11-25 3 PM CT: main 23c9245 green. A1 done as PR 29 (c5e0caf, ready). 03b done on PR 28 (ff13ea5, CI green, ready). PR 26 (02) unchanged, red, conflicts with main (seeder file now, Program.cs after PR 28); revised 02 rebuilds it on a new branch.

1. Merge PR 29 and PR 28 (no shared files, either order).
2. C1 (one PR): D2's plan sync and 02's security fix together, replacing PR 26.
3. (Covered by C1. Run D2 or 02 alone only if C1 is not used.)
4. Close PR 26 once the replacement PR is green.
5. D1 Dependabot groups, then close the old Dependabot PRs.
6. Then 04 and 07 in parallel, then 04b, then 05, as before. A1's findings may add fix rows.

Before each merge the PR must contain the latest main (GitHub "Update branch"); agents never run `git merge`.

| # | Prompt | Phase | Branch | Cap | Estimate | Depends on |
|---|---|---|---|---|---|---|
| 00 | [Clean baseline and test harness](00-baseline.md) | P0 | `chore/baseline-tests` | 60k | 35k | Done (PR 13). |
| 01 | [Database migrations, starter chart of accounts, API address](01-migrations-coa.md) | P0 | `fix/migrations-and-coa` | 80k | 50k | Done (PR 23). |
| 02 | [Finish the security hardening branch (revised)](02-security-hardening.md) | P0 | `fix/security-hardening-v2` (replaces PR 26) | 50k | 32k | PR 28 merged. |
| R1 | [Competitor and open-source research](R1-competitor-research.md) | Research | `docs/r1-competitor-research` | 300k | 200k | Ran twice: PR 27 plus a second report. Merge with R1b. |
| R1b | [Merge both R1 results and add IRS recordkeeping and filing readiness](R1b-merge-research.md) | Research | `docs/r1-competitor-research` (PR 27) | 220k | 155k | Done 2026-11-25 (54fe5e5); Part 2 needs R1b-fix. |
| R1b-fix | [Correct Part 2 IRS facts and tags](R1b-fix-irs-part2.md) | Research | `docs/r1-competitor-research` (PR 27) | 60k | 35k | Done 2026-11-25 (988402f); Direct File still wrong. |
| R1b-fix2 | [Correct Direct File and Free File](R1b-fix2-direct-file.md) | Research | `docs/r1-competitor-research` (PR 27) | 15k | 8k | Done; PR 27 merged 2026-11-25 (23c9245). |
| C1 | [One PR for D2 and 02](C1-one-pr-sync-and-security.md) | P0 + Chore | `chore/sync-and-security-2026-11-25` | 90k | 62k | PRs 28 and 29 merged. Use this instead of running D2 and 02 separately. |
| D2 | [Sync repo plan and AGENTS.md, add council roles](D2-sync-plan-and-agents.md) | Chore | `docs/sync-plan-2026-11-25` | 40k | 30k | PR 29 merged. |
| 03b | [Close ledger core gaps on PR 28](03b-ledger-gaps.md) | P0 | `feat/ledger-core` (PR 28) | 90k | 55k | Done: PR 28 at ff13ea5. |
| A1 | [Feature board and architecture consistency review](A1-feature-board-arch-review.md) | Architecture | `docs/feature-board-arch-review` | 120k | 80k | Done: PR 29 at c5e0caf. |
| D1 | [Group Dependabot updates](D1-dependabot-groups.md) | P0 chore | `chore/dependabot-groups` | 15k | 10k | Prompts 02 and R1 merged. |
| 03 | [Ledger module with locked entries and in-process events (revised)](03-ledger-core.md) | P0 | `feat/ledger-core` | 160k | 110k | Prompts 02 and R1 merged (R1 ledger section required). |
| 04 | [Ports and adapters boundary plus network-off gate](04-ports-network-gate.md) | P0 | `feat/ports-and-network-gate` | 100k | 65k | Prompt 03 merged. |
| 04b | [PII masking core before import](04b-pii-masking-core.md) | P0 | `feat/pii-masking-core` | 90k | 60k | Prompt 04 merged. |
| 05 | [Staged statement import: parse, preview, review, post](05-statement-import.md) | P0 | `feat/statement-import` | 120k | 80k | Prompts 03, 04 and 04b merged. |
| 06 | [PII sanitization layer, encrypted vault and no-leak tests](06-pii-layer.md) | P1 | `feat/pii-layer` | 120k | 70k | Prompts 04b and 05 merged. |
| 07 | [Tax category maps as versioned data, beyond S-corp](07-tax-maps.md) | P1 | `feat/tax-maps` | 120k | 80k | Prompt 03 merged. |
| 08 | [Reports from the ledger and the CPA packet export](08-reports-cpa-packet.md) | P1 | `feat/reports-cpa-packet` | 150k | 100k | Prompts 06 and 07 merged. |
| 09 | [Bank reconciliation](09-reconciliation.md) | P1 | `feat/reconciliation` | 100k | 70k | Prompts 05 and 08 merged. |
| 10 | [Setup wizard and main app UI (shell to components)](10-setup-wizard-ui.md) | P1 | `feat/setup-wizard-ui` | 150k | 100k | Prompts 01, 02 and 07 merged (05, 08, 09 for the later screens). |
| 11 | [One-command bootstrap with encrypted local store](11-one-command-bootstrap.md) | P2 | `feat/one-command-bootstrap` | 150k | 110k | Prompts 04 and 06 merged. |
| 12 | [README trust statement, privacy and security docs](12-readme-trust.md) | P1 | `docs/trust-and-privacy` | 60k | 35k | Prompts 06 and 11 merged. |
| 13 | [Encrypted backups and restore](13-backups.md) | P2 | `feat/encrypted-backups` | 100k | 65k | Prompt 11 merged. |
| 14 | [Local receipt reading (OCR) behind the importer port](14-local-receipt-ocr.md) | P2 | `feat/local-receipt-ocr` | 150k | 100k | Prompts 05 and 06 merged. |
| 15 | [Opt-in integration and AI adapters behind the network gate](15-integration-ai-adapters.md) | P2 | `feat/opt-in-adapters` | 120k | 80k | Prompts 04 and 06 merged. |

## Order and parallel lanes

- Done: 00, 01. Running now: R1 research, and 02 (finish PR 26). Everything else, D1 included, waits for R1.
- After 03: 04 (ports and network gate) and 07 (tax maps) can run in parallel.
- After 04: 04b (PII masking core, ADR 0010). After 03 + 04b: 05 (import). After 04b + 05: 06 (PII layer).
- After 06 + 07: 08 (reports). After 05 + 08: 09 (reconciliation).
- 10 (UI) starts after 01, 02, 07; later screens wait for 05, 08, 09.
- After 04 + 06: 11 (one-command bootstrap), 15 (opt-in adapters). After 05 + 06: 14 (OCR).
- After 11: 13 (backups). After 06 + 11: 12 (README trust docs).

## Decisions only Veeresh makes

- Merge each draft PR (agents never merge).
- Approve any UI mockup in prompt 10 before its code.
- Approve library choices flagged by arena in prompts 06, 11 and 14 if the agent asks.

## Notes

- No AGENTS.md budget existed in the repo when these were written; each prompt tells the agent to use the stricter cap if one is added.
- P0 = 00, 01, 02, 03, 04, 05. Shipping those makes TrueBalance a working, safe double-entry ledger with real imports.
