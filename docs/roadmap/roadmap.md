# TrueBalance roadmap

Nov 21, 2026 · Built from the 16 fix prompts in `docs/specs/prompts/` and the TrueBalance review. Token figures are planning caps and estimates, not measurements.

## At a glance

| Phase | Prompts | What you have when it's done | Caps | Estimates |
| --- | --- | --- | --- | --- |
| P0: works and is safe | 00–05 (with 04b) | A fresh install runs, uploads are safe, and imported statements become balanced, locked ledger entries | 680k | 440k |
| P1: CPA-ready and usable | 06–10, 12 | PII is masked and encrypted, tax categories fit every user type, a CPA packet exports, books reconcile, and a non-expert can set it up | 700k | 455k |
| P2: easy install and growth | 11, 13–15 | One command installs it with no Docker, backups are encrypted, receipts are read locally, and integrations plug in (off by default) | 520k | 355k |
| **Total** | **17** |  | **1,900k** | **1,250k** |

Every PR is a draft. You merge each one before the next prompt starts on it.

## Decided: PII masking runs before import

ADR 0010 is accepted (2026-11-21, owner decision after a 3-seat advisor review, 3 of 3 for masking first). A small masking core (04b) lands before statement import (05), so raw account numbers and tax IDs never reach the database or logs. Masking keeps the last 4 digits and a keyed fingerprint for matching; original statements are kept and encrypted in 06. Net cost about +30k (planning estimate).

## Lanes (what can run in parallel)

```
P0   00 baseline ─▶ 01 migrations ─┬─▶ 02 security
                                   └─▶ 03 ledger ─┬─▶ 04 ports + network gate ─▶ 04b masking core ─▶ 05 import
                                                  └─▶ 07 tax maps (P1, can start early)

P1   04 + 05 ─▶ 06 PII layer ─┐
     07 tax maps ─────────────┴─▶ 08 reports + CPA packet ─▶ 09 reconciliation (also needs 05)
     01 + 02 + 07 ─▶ 10 setup wizard UI (later screens wait for 05, 08, 09)

P2   04 + 06 ─┬─▶ 11 one-command bootstrap ─┬─▶ 13 backups
             │                              └─▶ 12 README trust (P1 item, waits for 06 + 11)
             └─▶ 15 opt-in integrations + AI adapter
     05 + 06 ─▶ 14 local receipt reading
```

The two widest points: after 01 (02 and 03 together) and after 03 (04 and 07 together).

## P0: works and is safe

| # | What it delivers in plain words | Depends on | Cap / est |
| --- | --- | --- | --- |
| 00 | Tests and CI exist, lint passes, vulnerable packages updated, docs stop claiming features that don't exist | none | 60k / 35k |
| 01 | A fresh install runs; starter account lists for household, sole proprietor and S-corp; the UI can reach the API | 00 | 80k / 50k |
| 02 | Uploads can't write files outside their folder; ports open only to this computer; no password in the code; no stack traces | 01 | 80k / 50k |
| 03 | Real double-entry books: entries must balance, posted entries can't be edited (fix with a reversal), periods lock, everything is logged | 01 | 150k / 100k |
| 04 | Plug-in boundary so integrations never touch the ledger; the internet is off unless a user turns an integration on; tests fail if code opens a connection | 03 | 100k / 65k |
| 04b | Account, routing and tax ID numbers are masked before anything is stored; logs stay clean; a no-leak test proves it | 04 | 90k / 60k |
| 05 | Import a bank CSV or OFX file, review each row, then post it as a balanced entry; duplicates get flagged | 03, 04b | 120k / 80k |

**Done when:** you can install it, import a statement, post entries that balance, and nothing reaches the network.

## P1: CPA-ready and usable

| # | What it delivers in plain words | Depends on | Cap / est |
| --- | --- | --- | --- |
| 06 | Masking extended to every PII type, sensitive fields encrypted, original documents in an encrypted vault, tests prove nothing leaks | 04b, 05 | 120k / 70k |
| 07 | Tax categories as data for Schedule C, 1120-S and households, fixed 1120-S lines, meals and mileage fields, 1099 vendor flag | 03 | 120k / 80k |
| 08 | Trial balance, P&L, balance sheet, general ledger, tax-line and 1099 summaries, and one CPA packet per year (CSV + Excel) | 06, 07 | 150k / 100k |
| 09 | Match the books to a bank statement balance and mark it reconciled | 05, 08 | 100k / 70k |
| 10 | The setup wizard and main screens from the mockup, built as an app shell with feature components | 01, 02, 07 | 150k / 100k |
| 12 | README trust statement, privacy and security docs, each claim linked to the test that proves it | 06, 11 | 60k / 35k |

**Done when:** a non-expert sets up their books in minutes, and a CPA gets a yearly packet built from the ledger with PII masked by default.

Two IRS facts these prompts rely on, verified Nov 19:

- Current Form 1120-S instructions list line 19 as Energy Efficient Commercial Buildings Deduction and line 20 as Other Deductions ([IRS i1120s](https://irs.gov/instructions/i1120s)) \[Confidence: Moderate | Vote: 1/1 primary\].
- Two sources report the 1099-NEC/MISC threshold rising from $600 to $2,000 for payments in 2026, inflation-adjusted after 2026 ([CPA Practice Advisor](https://www.cpapracticeadvisor.com/2026/09/01/what-the-new-1099-reporting-rules-mean/189451/), [Barnes Dennig](https://www.barnesdennig.com/obbba-brings-relief-form-1099-reporting/)) \[Confidence: Moderate | Vote: 2/2 secondary\]. Prompt 07 keeps the threshold as data so it can change without code.

## P2: easy install and growth

| # | What it delivers in plain words | Depends on | Cap / est |
| --- | --- | --- | --- |
| 11 | One command installs and starts it; books live in one encrypted local file; no Docker or Postgres needed | 04, 06 | 150k / 110k |
| 13 | Encrypted backups to a folder or USB drive, with a safe restore | 11 | 100k / 65k |
| 14 | Receipts read on your machine only, always reviewed before posting | 05, 06 | 150k / 100k |
| 15 | One example integration and an AI helper that only ever sees masked text, both off by default, with a kill switch | 04, 06 | 120k / 80k |

**Done when:** someone with no technical background installs it with one command, and integrations or AI can be added without touching the ledger or seeing raw PII.

## Your decision points

| When | Decision |
| --- | --- |
| After each PR | Review and merge the draft PR |
| During 06, 11, 14 | Approve the library pick if the agent asks (encryption, encrypted store, OCR) |
| During 10 | Approve any screen mockup that differs from the approved wizard mockup before its code |
| When any prompt nears its cap | Allow more tokens or stop |

## Order notes

- 12 is a P1 item, but it waits for 11 (P2) because the trust statement has to describe the one-command install and the encrypted file.
- 07 can start right after 03, while P0 is still finishing.
- 10's later screens (import, reports, reconcile) wait for 05, 08 and 09; the setup steps can go first.
