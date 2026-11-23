# TrueBalance fix prompts: index

Written 2026-11-19 from the TrueBalance review (`docs/research/review.md`). One prompt per draft PR. Paste each prompt inline into your local agent, in order, after the previous PR is merged. Every prompt carries the full rules block (budget, orchestration, pstack, engineering, facts, git, report format), so each stands alone.

Total: 17 prompts | Caps add up to 1900k | Estimates add up to 1250k tokens.

| # | Prompt | Phase | Branch | Cap | Estimate | Depends on |
|---|---|---|---|---|---|---|
| 00 | [Clean baseline and test harness](00-baseline.md) | P0 | `chore/baseline-tests` | 60k | 35k | None (first PR). |
| 01 | [Database migrations, starter chart of accounts, API address](01-migrations-coa.md) | P0 | `fix/migrations-and-coa` | 80k | 50k | Prompt 00 merged. |
| 02 | [Upload path traversal, local-only ports, secrets, production mode](02-security-hardening.md) | P0 | `fix/security-hardening` | 80k | 50k | Prompt 01 merged. |
| 03 | [Double-entry ledger core with locked posted entries](03-ledger-core.md) | P0 | `feat/ledger-core` | 150k | 100k | Prompt 01 merged (02 can run in parallel). |
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

- Start: 00 -> 01.
- After 01: 02 (security) and 03 (ledger) can run in parallel.
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
