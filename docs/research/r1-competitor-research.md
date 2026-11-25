# TrueBalance R1: Competitor & Open-Source Bookkeeping Research

> **Status**: Draft PR #27 (Merged A+B) | **Branch**: `docs/r1-competitor-research` | **Date**: 2026-11-25 (merged)
> **Phase**: Research | **Token cap**: 220k | **Estimate**: 155k
> **Not tax or legal advice.** All IRS form and retention references below are recordkeeping alignment for a CPA to prepare returns. Designs and workflows only—no code, schemas, text, or icons are copied from any proprietary, source-available, or copyleft project.

Compiled: 2026-11-25 | Evidence Pack: docs/research/r1-evidence.md | Baseline: docs/research/verified-facts.md

---

## Answer First: Top 5 Ideas to Adopt

1. **Immutable posted entries + formal reversals + closing lock dates (Prompt 03, ADR 0002/0011)** — Lock posted entries against silent edits/deletes like [ERPNext](https://docs.frappe.io/erpnext/correct-a-submitted-accounting-entry) and [Odoo](https://www.odoo.com/documentation/13.0/applications/finance/accounting/reporting/overview/data_inalterability.html), require formal reversing entries, and enforce closing lock dates with an exception audit log like [QuickBooks Online](https://quickbooks.intuit.com/learn-support/en-us/help-article/close-books/close-books-quickbooks-online/L59LelyPM_US_en_US) and [Manager.io](https://www.manager.io/guides/lock-date-form/) [High 4/4].
2. **Single-file encrypted SQLite (SQLCipher) + localhost-only monolith with lockfile + separate encrypted document vault (Prompts 06, 11, ADR 0006/0011)** — Combine [Manager.io's](https://github.com/Manager-io/Manager) single-file local ASP.NET Core + SQLite footprint with [KMyMoney's](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.formats.html) SQLCipher encryption and lockfile guard, keeping receipts in a separate encrypted vault to avoid [Manager.io's](https://www2.manager.io/guides/8394) DB bloat [High 3/3].
3. **Non-destructive PII masking before a staged import queue with layered deduplication (Prompts 04b, 05, ADR 0009/0010)** — Mask raw PII (preserving last-4 + keyed HMAC fingerprint) *before* staging bank lines in a `Pending -> Categorized/Excluded -> Posted` queue ([QBO](https://quickbooks.intuit.com/learn-support/en-us/help-article/duplicate-transactions/fix-duplicate-transactions-quickbooks-online-bank-feeds/L1fjxl88f_US_en_US), [GnuCash](https://gnucash.org/docs/v5/C/gnucash-manual/trans-import.html), [ERPNext](https://docs.frappe.io/erpnext/bank-transaction)), deduplicating via OFX `FITID` + keyed content hash + date/amount window ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.impexp.csv.html), [Firefly III](https://docs.firefly-iii.org/references/data-importer/duplicate-detection/)) [High 5/5].
4. **Three-state clearing (`Uncleared`/`Cleared`/`Reconciled`) + $0.00 statement gate + fail-loud balance assertions (Prompts 03, 09, ADR 0002)** — Require a $0.00 difference against statement ending balance ([QBO](https://quickbooks.intuit.com/learn-support/en-us/help-article/reconciliation-reports/learn-reconcile-workflow-quickbooks/L8ZibUuVE_US_en_US), [GnuCash](https://www.gnucash.org/docs/v5/C/gnucash-manual/acct-reconcile.html), [KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.reconcile.wizard.html)) paired with [hledger](https://hledger.org/checking-for-errors.html)/[Beancount](https://beancount.github.io/docs/getting_started_with_beancount/)-style fail-loud balance assertions—banning silent `pad` or `Orphan` plug splits [High 5/5].
5. **Versioned per-year tax-line maps + CPA year-end packet export with AJE round-trip (Prompts 07, 08, ADR 0007/0008)** — Store Schedule C (with multi-copy support like [GnuCash TXF](https://www.gnucash.org/docs/v5/C/gnucash-manual/set-tax-options.html)) and Form 1120-S maps as versioned data, and export a masked-by-default ZIP packet (Adjusted Trial Balance, GL, P&L, Balance Sheet, 1099 summary, receipts index) with Adjusting Journal Entry (AJE) support ([QBO](https://quickbooks.intuit.com/learn-support/en-us/help-article/account-management/managing-accountant-users-quickbooks-online/L2AcdYvHw_US_en_US), [QBD](https://quickbooks.intuit.com/learn-support/en-us/help-article/company-file/create-accountant-copy-company-file-quickbooks/L7vx8V8F3_US_en_US)) [High 3/3].

---

## 1. Goal (`pstack:sharpen`)

1. Identify proven accounting, import, reconciliation, privacy, and packaging designs across 13 commercial and open-source bookkeeping tools.
2. Filter every design through TrueBalance's constraints (offline-first, PII-masked before import, local modular monolith, CPA-ready recordkeeping, zero copyleft/proprietary code reuse).
3. Map 24 ranked, council-voted ideas to TrueBalance prompts (`01`–`15`, `04b`) and ADRs (`0002`–`0011`), unblocking Prompt `03` (ledger core & CoA) first.

---

## 2. Research & Council Structure (`pstack:swarm`)

- **4 parallel researcher seats** (public official docs & repos only, read 2026-11-25):
  - **Seat A**: QuickBooks Online (QBO), QuickBooks Desktop (QBD).
  - **Seat B**: GnuCash, Manager.io, plus **KMyMoney** *(added: mature cross-platform open-source desktop double-entry app solving GPG/SQLCipher encryption at rest, `.anon.xml` anonymization, saved CSV profiles, and OFX `FITID` matching better than GnuCash)*.
  - **Seat C**: Ledger, hledger, Beancount, Fava.
  - **Seat D**: Akaunting, Firefly III, ERPNext, Odoo Community.
- **3 parallel council seats** (evidence-pack only, zero web calls): **CPA/Bookkeeper**, **Architecture** (ADR 0003 & 0011), and **Privacy & Security** (ADR 0004, 0005, 0006, 0010). All 24+ ideas reached consensus (`Adopt` or `Adapt` with explicit guardrails; zero `Skip` votes, so no re-vote was required).
- **1 independent reviewer seat**: audits final report completeness, citations, license hygiene, and prose quality.
- **Total seats**: 8 (4 researchers + 3 council + 1 reviewer).

---

## 3. Core Workflow Statecharts (`pstack:statechart`) [Inferred]

### A. Statement Import -> Review -> Post
```text
QBO:         [CSV/OFX/Feed] -> For Review (Pending) --(Match/Add/Auto-add)--> Categorized (Posted)
                                     |---(Exclude)---> Excluded   [Undo -> For Review]
GnuCash:     [OFX/QIF/CSV]  -> Generic Import Matcher (Add / Update+Clear / Clear + Bayesian) -> Posted Splits
ERPNext:     [CSV/Feed]     -> Bank Transaction (Staged) --(Match/Create Voucher)--> Submitted GL Entry
TrueBalance: [CSV/OFX/CAMT] -> IPiiMasker (Mask + Last4 + HMAC) -> StagedQueue (Pending + Dedupe + Rules)
                                     |--(Human Approve)--> Balanced Immutable Ledger Entry (+ Audit Event)
                                     |--(Exclude/Undo)---> Excluded / Reverted (never mutates posted GL)
```

### B. Bank Reconciliation
```text
QBO/QBD/GnuCash/KMyMoney: [Blank/n (Uncleared)] -> [C/c (Cleared)] --(Statement Diff == $0.00)--> [R/y (Reconciled)]
TrueBalance Target:       [Uncleared] -> [Cleared] --(Diff == $0.00 + Balance Assertion Pass)--> [Reconciled]
                          * Post-reconcile edits trigger Discrepancy Drill-Down (never silent Orphan/pad)
                          * Single-period Undo reverts only the latest period without deleting prior reports
```

### C. Period Close & Entry Correction
```text
ERPNext/Odoo: [Draft] --(Post/Submit)--> [Posted/Immutable] --(Mistake?)--> [Reversing Entry + New Entry]
TrueBalance:  [Posted (Immutable)] + [Closing Lock Date (Soft Warn / Hard Lock + Passphrase)]
              * Post-lock adjustments require formal Reversing/AJE entry + Closing Date Exception audit event
```

### D. Year-End Close & CPA Packet
```text
[Posted Entries] -> [Close Books (Lock Date)] -> [Build CPA Packet (TB, ATB, GL, P&L, BS, Tax Summary, 1099, Receipts Index)]
                                      |--(AJE Import)--> [Adjusting Journal Entries (Masked)]
                                      |--(Verify)--> [Zero-Sum Ledger Check + Non-Zero Suspense Warning]
                                      -> [Export Encrypted ZIP] -> [Hand to CPA or Filing Tool]
```

---

## 4. Comparison Matrix (11 Topics × 13 Tools)

*All links point to primary official documentation or repositories verified 2026-11-25 (except Ledger's BSD license and `hledger.org` comparison noted as secondary). See docs/research/r1-evidence.md for full sentences and dates.*

### Part 1: QuickBooks & Desktop Tools (Seats A & B)

| Topic | QuickBooks Online | QuickBooks Desktop | GnuCash | KMyMoney | Manager.io |
| --- | --- | --- | --- | --- | --- |
| **1. Double-entry ledger** | Balanced JEs; editable/reversible ("R" suffix); 2-yr Audit Log; closing date + exception report ([QBO JE](https://quickbooks.intuit.com/learn-support/en-us/help-article/accounting-bookkeeping/create-journal-entry-quickbooks-online/L6Bzy9mT9_US_en_US), [Close](https://quickbooks.intuit.com/learn-support/en-us/help-article/close-books/close-books-quickbooks-online/L59LelyPM_US_en_US)) | Balanced JEs; dynamic RE rollover; closing password + exception report ([QBD JE](https://quickbooks.intuit.com/learn-support/en-us/help-article/journal-entries/create-journal-entry-quickbooks-desktop-windows/L3UuQaxmb_US_en_US), [Close](https://quickbooks.intuit.com/learn-support/en-us/help-article/close-books/close-books-quickbooks-desktop/L8T1Cgfhk_US_en_US)) | Multi-split JEs; editable; reversal/void (`v`); Close Book; read-only day threshold ([GnuCash](https://www.gnucash.org/docs/v5/C/gnucash-manual/trans-reverse.html)) | Multi-category splits; editable; no void or period lock date ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.ledger.transactions.html)) | Balanced JEs (imbalance to `Suspense`); reversible History; `Lock Date` ([Manager](https://www.manager.io/guides/lock-date-form/)) |
| **2. Chart of accounts** | Entity-tailored CoA; sub-accounts; lock from posting; numbers opt-in ([QBO CoA](https://quickbooks.intuit.com/learn-support/en-us/help-article/chart-accounts/add-account-chart-accounts-quickbooks-online/L0mCv3AKn_US_en_US), [Nums](https://quickbooks.intuit.com/learn-support/en-us/help-article/chart-accounts/use-account-numbers-chart-accounts/L7lRLN0Do_US_en_US)) | Entity CoA; sub-accounts; numbers off by default; no auto-numbering ([QBD CoA](https://quickbooks.intuit.com/learn-support/en-us/help-article/chart-accounts/add-edit-delete-accounts-quickbooks-desktop/L5g0CLUta_US_en_US)) | Template wizard (Common/Business); sub-accounts; optional codes + renumber ([GnuCash](https://www.gnucash.org/docs/v5/C/gnucash-manual/busnss-ar-setup.html)) | 5 fixed roots; sub-accounts/categories; importable templates; no GL code col ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.accounts.create.html)) | BS/P&L custom groups; P&L subtotals; optional codes; country templates ([Manager](https://www.manager.io/guides/chart-of-accounts/)) |
| **3. Statement import** | CSV/QBO/QFX/OFX; `For review` staging; match/exclude dedupe ([CSV](https://quickbooks.intuit.com/learn-support/en-us/help-article/bank-transactions/format-csv-files-excel-bank-transactions-quickbooks/L4BjLWckq_US_en_US), [Dupes](https://quickbooks.intuit.com/learn-support/en-us/help-article/duplicate-transactions/fix-duplicate-transactions-quickbooks-online-bank-feeds/L1fjxl88f_US_en_US)) | QBO/Excel/CSV/IIF + PDF receipt capture into `For Review` ([Import](https://quickbooks.intuit.com/learn-support/en-us/help-article/import-export-data-files/import-export-data-quickbooks-desktop/L9KS42UxP_US_en_US), [Receipts](https://quickbooks.intuit.com/learn-support/en-us/help-article/account-management/upload-receipts-quickbooks-desktop-pro-plus-plus/L35ri4KUE_US_en_US)) | QIF/OFX/CSV + AqBanking CAMT/MT940; Generic Import Matcher staging ([GnuCash](https://gnucash.org/docs/v5/C/gnucash-manual/trans-import.html)) | OFX/QFX/QIF/CSV (saved profiles); `FITID` + +/- day window; Accept/Unmatch ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.impexp.csv.html)) | QIF/OFX/QBO/CAMT053/MT940/CSV; summary preview; uncategorized to `Suspense` ([Manager](https://www.manager.io/guides/import-bank-statement/)) |
| **4. Bank reconciliation** | `blank`/`C`/`R`; $0.00 gate; discrepancy tray; cascading irreversible undo ([Workflow](https://quickbooks.intuit.com/learn-support/en-us/help-article/reconciliation-reports/learn-reconcile-workflow-quickbooks/L8ZibUuVE_US_en_US), [Undo](https://quickbooks.intuit.com/learn-support/en-us/help-article/accounting-bookkeeping/undo-remove-transactions-reconciliations-online/L6ERlEXxn_US_en_US)) | Ending balance $0.00 gate; Discrepancy report; sequential `Undo Last` ([QBD Rec](https://quickbooks.intuit.com/learn-support/en-us/help-article/reconciliation-reports/resolve-common-issues-previous-reconciliation/L6T5gJIKt_US_en_US)) | `n`/`c`/`y`/`v`; $0.00 gate; warns on reconciled edits; optional `Orphan` split ([GnuCash](https://www.gnucash.org/docs/v5/C/gnucash-manual/acct-reconcile.html)) | Wizard with live Diff; `C`->`R` at $0.00; does not block prior edits ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.reconcile.wizard.html)) | Cleared vs Pending; closing balance check; clickable Discrepancy drill-down ([Manager](https://www.manager.io/guides/bank-reconciliations/)) |
| **5. Categorization rules** | Priority-ordered rules (1 rule/txn); Auto-add or review; Excel export; AI suggestions ([QBO Rules](https://quickbooks.intuit.com/learn-support/en-us/help-article/banking/use-bank-rules-quickbooks-online/L1ZxZSVn5_US_en_US)) | Advanced Any/All renaming rules + Express auto-rule on Quick Add ([QBD Rules](https://quickbooks.intuit.com/learn-support/en-us/help-article/bank-feeds/use-renaming-rules-bank-feeds/L7m7kwjDP_US_en_US)) | Per-account token-frequency Bayesian matcher + `Import Map Editor` ([GnuCash](https://gnucash.org/docs/v5/C/gnucash-manual/trans-import.html)) | Per-payee match rules + past-txn autofill; arbitrary tie-break on overlap ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.payees.details.html)) | Receipt/Payment Bank Rules (specific before general); no ML; rest to `Suspense` ([Manager](https://www.manager.io/guides/bank-rules/)) |
| **6. Tax mapping** *(not tax advice)* | Detail Types -> Sched C; 1120-S mapping + Prep for taxes (lost on form switch) ([Sched C](https://quickbooks.intuit.com/learn-support/en-us/help-article/tax-forms/learn-quickbooks-online-detail-types-appear-tax/L8cmkg8i9_US_en_US), [1120-S](https://quickbooks.intuit.com/learn-support/en-us/help-article/corporation/manage-account-mapping-tax-categories-tax-forms/L54GSV2pl_US_en_US)) | Company tax form + per-account `Tax-Line Mapping` dropdown ([QBD Tax](https://quickbooks.intuit.com/learn-support/en-us/help-article/payroll-preferences/resolve-payroll-set-info-save/L7bAAT4j5_US_en_US)) | US TXF categories (1040/1065/1120/1120S); multi-copy `Schedule C(1)/(2)` ([GnuCash](https://www.gnucash.org/docs/v5/C/gnucash-manual/set-tax-options.html)) | Per-category boolean Tax flag + VAT; no IRS form/year maps ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.categories.edit.html)) | Tax Codes + Report Transformations; edits retroactive (not year-versioned) ([Manager](https://www2.manager.io/guides/8901)) |
| **7. Accountant/CPA export** | Live QBO Accountant invite; AJEs; Adjusted TB/GL; Excel/PDF/ZIP ([QBO CPA](https://quickbooks.intuit.com/learn-support/en-us/help-article/account-management/managing-accountant-users-quickbooks-online/L2AcdYvHw_US_en_US)) | Dividing-date Accountant's Copy (`.QBX`/`.QBA`/`.QBY`); GJE send; Excel/PDF ([QBD CPA](https://quickbooks.intuit.com/learn-support/en-us/help-article/company-file/create-accountant-copy-company-file-quickbooks/L7vx8V8F3_US_en_US)) | TB/GL/P&L/BS to PDF/HTML; CSV registers; TXF export; `.gnucash` file ([GnuCash](https://www.gnucash.org/docs/v5/C/gnucash-manual/report-print.html)) | Reports to HTML/CSV; register CSV / QIF; portable `.kmy` file ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.reports.working.html)) | TB/GL/P&L/BS print-to-PDF or TSV clipboard; single `.manager` backup ([Manager](https://www.manager.io/guides/reports/)) |
| **8. Offline & local storage** | Cloud-only SaaS; requires internet ([QBO Sec](https://quickbooks.intuit.com/learn-support/en-us/help-article/banking/privacy-security-quickbooks/L7q2gUqfM_US_en_US)) | Local `.QBW` (SQL Anywhere); LAN Server Manager; AES-256 sensitive data ([QBD DB](https://quickbooks.intuit.com/learn-support/en-us/help-article/install-products/use-quickbooks-database-server-manager/L1oLSsAZ1_US_en_US)) | Offline gzipped XML or SQLite/MySQL/PG; no native encryption; single-user ([GnuCash](https://www.gnucash.org/docs/v5/C/gnucash-guide/basics-files1.html)) | Offline `.kmy` XML w/ GPG encryption or SQLCipher/MySQL/PG + lockfile ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.formats.html)) | Offline local ASP.NET Core + Electron; unencrypted `.manager` SQLite ([Manager](https://github.com/Manager-io/Manager)) |
| **9. Backups & restore** | Nightly Intuit backups; point-in-time user restore only on Advanced ([QBO Bkp](https://quickbooks.intuit.com/learn-support/en-us/help-article/back-data/back-restore-quickbooks-online-advanced-company/L9sTCQn9P_US_en_US)) | Scheduled/manual `.QBB`, portable `.QBM`; `Verify Data` & `Rebuild Data` ([QBD Bkp](https://quickbooks.intuit.com/learn-support/en-us/help-article/back-data/back-quickbooks-desktop-company-file/L9qYBI54v_US_en_US)) | Auto timestamped `.gnucash` backups + `.log` replay on XML save ([GnuCash](https://www.gnucash.org/docs/v5/C/gnucash-guide/basics-backup1.html)) | Periodic autosave; numbered backups on close (up to 20); dated manual backup ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.settings.html)) | Continuous SQLite save + manual 1-click `.manager` backup/import ([Manager](https://www.manager.io/guides/backup/)) |
| **10. PII handling** | AES-256 cloud; warns against SSN/CC in receipts; Intuit Assist uses GenOS LLMs ([Sec](https://security.intuit.com/security-practices), [AI](https://quickbooks.intuit.com/learn-support/en-us/help-article/intuit-assist/hows-intuit-using-generative-ai/L1Z16bIPW_US_en_US)) | Encrypted SSN/CC/bank fields (90-day password); warns vs SSN in receipts ([Sec](https://security.intuit.com/security-practices)) | Unencrypted local file; external linked receipt paths; zero cloud/AI telemetry ([GnuCash](https://www.gnucash.org/docs/v5/C/gnucash-manual/trans-assoc.html)) | GPG/SQLCipher encryption; `.anon.xml` bug-report scrubber; zero cloud/AI ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.formats.html)) | Unencrypted `.manager` DB holding attachments (needs `Vacuum`); zero AI ([Manager](https://www2.manager.io/guides/8394)) |
| **11. Plugins & integrations** | REST/GraphQL API + coarse OAuth 2.0 scopes ([Intuit Dev](https://developer.intuit.com/app/developer/qbo/docs/develop/authentication-and-authorization/oauth-2.0)) | Local COM/qbXML + Web Connector (`.QWC`); Admin block-all toggle ([QBWC](https://quickbooks.intuit.com/learn-support/en-us/help-article/install-products/set-quickbooks-web-connector/L4Vp7VI44_US_en_US)) | Python/Guile bindings; AqBanking/LibOFX/Finance::Quote on user trigger ([GnuCash](https://wiki.gnucash.org/wiki/Python_Bindings)) | Modular plugins (KBanking, LibOFX, CSV, Woob); network only on user trigger ([KMyMoney](https://docs.kde.org/trunk_kf6/en/kmymoney/kmymoney/details.settings.plugins.html)) | URL Extensions + REST API Access Tokens; Desktop offline unless configured ([Manager](https://www.manager.io/guides/extensions/)) |

### Part 2: Plain-Text Tools (Seat C)

| Topic | Ledger | hledger | Beancount | Fava |
| --- | --- | --- | --- | --- |
| **1. Double-entry ledger** | Plain-text signed postings; elided auto-balance; parse-order `=` assertions ([hledger](https://hledger.org/ledger.html) *sec*) | Plain-text signed postings; date-ordered `=` assertions; `hledger close` ([hledger](https://hledger.org/MANUAL.html)) | Plain-text signed postings; `balance`/`pad`; `open`/`close` directives ([Beancount](https://beancount.github.io/docs/beancount_cheat_sheet/)) | Web UI over Beancount files; enforces Beancount rules ([Fava](https://beancount.github.io/fava/api/fava.html)) |
| **2. Chart of accounts** | 5 types, colon-delimited; optional `account` directive ([Ledger](https://github.com/ledger/ledger/blob/master/doc/GLOSSARY.md)) | 5 types (`A/L/E/R/X`), colon-delimited; optional `account` declarations ([hledger](https://hledger.org/sm-2023-setup.html)) | Strict 5 roots, colon-delimited; mandatory `open`/`close` directives ([Beancount](https://beancount.github.io/docs/getting_started_with_beancount/)) | Interactive 5-root trees + editor autocomplete ([Fava](https://beancount.github.io/fava/api/fava.core.html)) |
| **3. Statement import** | `ledger convert -f` for CSV; external scripts for OFX/PDF/dedupe ([Ledger](https://github.com/ledger/ledger/releases)) | CSV/TSV `.rules` + pipe converters; `--dry` preview; `.latest` dedupe ([hledger](https://hledger.org/import-csv.html)) | `beangulp` Python `Importer` (`identify`/`extract`/`deduplicate`) ([beangulp](https://github.com/beancount/beangulp/blob/master/beangulp/importer.py)) | Web Import UI running `beangulp` + "ignore duplicate" checkbox ([Fava](https://github.com/beancount/fava/blob/main/src/fava/help/import.md)) |
| **4. Bank reconciliation** | `*`/`!` flags + parse-order `=` fail-loud assertions ([hledger](https://hledger.org/ledger.html) *sec*) | `*`/`!` flags + date-ordered `=` fail-loud assertions + `check` ([hledger](https://hledger.org/checking-for-errors.html)) | `*`/`!` flags + `balance`/`pad` failing loudly in `bean-check` ([Beancount](https://beancount.github.io/docs/getting_started_with_beancount/)) | Surfaces all `bean-check` assertion errors in UI ([Fava](https://beancount.github.io/fava/api/fava.core.html)) |
| **5. Categorization rules** | Automated transactions (`= expr`); no built-in ML ([Ledger](https://github.com/ledger/ledger/blob/master/doc/GLOSSARY.md)) | Sequential `if` blocks/tables in CSV rules (last match wins); `auto` rules ([hledger](https://hledger.org/import-csv.html)) | Custom `beangulp` hooks or `smart_importer` local ML ([smart\_importer](https://github.com/beancount/smart_importer/blob/main/README.rst)) | Runs `beangulp` hooks (`smart_importer`) for manual UI review ([Fava](https://github.com/beancount/fava/blob/main/src/fava/help/import.md)) |
| **6. Tax mapping** *(not tax advice)* | Accounts + `:TAG:`/metadata; yearly `include` files ([Ledger](https://github.com/ledger/ledger/blob/master/doc/GLOSSARY.md)) | Accounts, tags, aliases + yearly journal queries (`-p 2023`) ([hledger](https://hledger.org/tax-reporting-setup.html)) | Yearly accounts/tags/links + SQL-like BQL (`bean-query`) ([Beancount](https://beancount.github.io/docs/beancount_query_language/)) | Filter by period/tag/payee + browser BQL queries ([Fava](https://beancount.github.io/fava/changelog.html)) |
| **7. Accountant/CPA export** | `bal`, `reg`, `csv` (RFC 4180), HTML, text ([Ledger](https://github.com/ledger/ledger/issues/732)) | `bs`/`is`/`bal`/`reg` via `-O` to CSV/TSV/HTML/JSON/FODS ([hledger](https://hledger.org/export.html)) | BQL (`bean-query`) + external v3 tools/Fava ([Beancount](https://github.com/beancount/beancount/blob/master/README.rst)) | Download filtered Beancount + BQL to CSV/XLSX/ODS ([Fava](https://beancount.github.io/fava/api/fava.util.html)) |
| **8. Offline & local storage** | Local plain text + `include`; optional GPG read; Git sync ([Ledger](https://github.com/ledger/ledger/releases)) | Local UTF-8 text + `include`; CLI/TUI/local web; OS encryption ([hledger](https://hledger.org/install.html)) | Local UTF-8 text + `include`; OS disk encryption + Git ([Beancount](https://github.com/beancount/beancount/issues/653)) | Local Flask server on `localhost:5000`; optional `--read-only` ([Fava](https://beancount.github.io/fava/usage.html)) |
| **9. Backups & restore** | File/Git backups + `--hashes` hash-chaining + `--strict` ([Ledger](https://github.com/ledger/ledger/releases)) | Git + pre-commit `hledger check -s ordereddates recentassertions` ([hledger](https://hledger.org/sm-2023-setup.html)) | File/Git backups + `bean-check` on every load ([Beancount](https://beancount.github.io/docs/getting_started_with_beancount/)) | `fava.ext.auto_commit` auto-commits every UI edit to Git ([Fava](https://beancount.github.io/fava/api/fava.ext.html)) |
| **10. PII handling** | Offline CLI; optional GPG; zero cloud/telemetry ([Ledger](https://github.com/ledger/ledger)) | Offline; private-by-default `stats`; zero cloud/telemetry ([hledger](https://hledger.org/news.html)) | Account-mirrored local `documents` folder; zero network/telemetry ([Beancount](https://github.com/beancount/beancount/discussions/972)) | Serves local docs + `--incognito` number hiding; zero telemetry ([Fava](https://beancount.github.io/fava/api/fava.html)) |
| **11. Plugins & integrations** | Boost.Python bindings + `python` directive; local only ([Ledger](https://github.com/ledger/ledger)) | `hledger-*` add-ons on `PATH` + Haskell API + local HTTP-JSON ([hledger](https://hledger.org/scripts.html)) | Pure Python `(entries, options_map)` plugin pipeline; zero auto-net ([Beancount](https://github.com/beancount/beancount/blob/master/beancount/plugins/close_tree.py)) | Python `FavaExtensionBase` lifecycle hooks + JS/Jinja ([Fava](https://github.com/beancount/fava/blob/main/src/fava/help/extensions.md)) |

### Part 3: Web & ERP Accounting Tools (Seat D)

| Topic | Akaunting | Firefly III | ERPNext | Odoo Community |
| --- | --- | --- | --- | --- |
| **1. Double-entry ledger** | Single-entry core; Double-Entry app adds debit/credit JEs + delete guard ([Akaunting](https://akaunting.com/hc/docs/double-entry-accounting/journal-entry/)) | `transaction_group`/`journals` + 2 signed `transactions` rows; editable; no lock dates ([Firefly III](https://docs.firefly-iii.org/explanation/more-information/architecture/)) | Submittable `docstatus=1` lock + Cancel/Amend; `Period Closing Voucher` + `Accounts Frozen Till Date` ([ERPNext](https://docs.frappe.io/erpnext/correct-a-submitted-accounting-entry)) | `account.move` posted lock + Reverse Entry; Hard/Tax Lock Dates; optional SHA-256 hash chain ([Odoo](https://www.odoo.com/documentation/13.0/applications/finance/accounting/reporting/overview/data_inalterability.html)) |
| **2. Chart of accounts** | 33 country templates (Double-Entry app); codes + sub-accounts ([Akaunting](https://akaunting.com/apps/double-entry)) | Flat accounts only; no CoA templates; explicitly no sub-accounts ([Firefly III](https://docs.firefly-iii.org/explanation/more-information/what-its-not/)) | Country CoA JSON templates; `root_type` + `is_group` leaf-only posting ([ERPNext](https://docs.frappe.io/erpnext/chart-of-accounts)) | Fiscal Localization CoA + codes + prefix-based Account Groups ([Odoo](https://www.odoo.com/documentation/16.0/applications/finance/accounting/get_started/chart_of_accounts.html)) |
| **3. Statement import** | Core XLS (500 rows); paid apps for OFX/MT940/Bank Feeds + Unmatched queue ([Akaunting](https://akaunting.com/hc/docs/import-export/importing-bank-transactions/)) | Separate Data Importer: CSV/CAMT.053 (no PDF); JSON configs; hash/ID dedupe ([Firefly III](https://docs.firefly-iii.org/references/data-importer/duplicate-detection/)) | Staged `Bank Transaction` + custom column mappings; ID/ref/date/amount dedupe ([ERPNext](https://docs.frappe.io/erpnext/bank-transaction)) | CSV/OFX/QIF/CAMT.053/CODA (full bank sync/Accounting is Enterprise or OCA) ([Odoo](https://www.odoo.com/documentation/17.0/applications/finance/accounting/bank/transactions.html)) |
| **4. Bank reconciliation** | Date-range matching; editable post-reconcile; no hard balance lock ([Akaunting](https://akaunting.com/hc/docs/banking-feeds-reconciliations/reconciling-transactions/)) | Date-range check-off; shows discrepancy; optional auto-balancing txn ([Firefly III](https://docs.firefly-iii.org/how-to/firefly-iii/finances/reconcile/)) | Matches `Bank Transaction` to vouchers; Clearance Date + partial unallocated tracking ([ERPNext](https://docs.frappe.io/erpnext/bank-reconciliation)) | Community has basic Invoicing payment match; full bank widget is Enterprise/OCA ([Odoo](https://www.odoo.com/forum/help-1/accounting-community-vs-enterprise-271286)) |
| **5. Categorization rules** | Bank Feeds rules + exact-amount/day-offset suggestions in Unmatched queue ([Akaunting](https://akaunting.com/apps/bank-feeds)) | Rule Groups, strict ALL/non-strict ANY, invertible triggers, stop-processing ([Firefly III](https://docs.firefly-iii.org/how-to/firefly-iii/features/rules/)) | Semi-Auto Payment Reconciliation suggestions; no standalone personal rule engine ([ERPNext](https://docs.frappe.io/erpnext/semi-auto-payment-reconciliation)) | Priority-ordered Reconciliation Models w/ tolerance & regex (full flow Enterprise) ([Odoo](https://www.odoo.com/documentation/17.0/applications/finance/accounting/bank/reconciliation_models.html)) |
| **6. Tax mapping** *(not tax advice)* | Flat/compound/withholding tax rates; no year-versioned US form maps ([Akaunting](https://akaunting.com/hc/docs/settings/taxes/)) | Household Budgets, Categories, Tags; no business tax form maps ([Firefly III](https://docs.firefly-iii.org/explanation/data-classification/what-to-use/)) | Tax Categories, Templates, validity-dated Tax Rules + country apps ([ERPNext](https://docs.frappe.io/erpnext/setting-up-taxes)) | Fiscal Positions + Tax Grids + Tax Lock Dates ([Odoo](https://www.odoo.com/documentation/17.0/applications/finance/accounting/reporting/tax_returns.html)) |
| **7. Accountant/CPA export** | Read-only Accountant role; GL/TB/BS/P&L + XLS exports (Double-Entry) ([Akaunting](https://akaunting.com/hc/docs/users-and-roles/roles-and-permission-levels/)) | CSV export via UI/CLI; web reports; no Accountant role or audit export ([Firefly III](https://docs.firefly-iii.org/tutorials/firefly-iii/exporting-data/)) | TB/GL/BS/P&L/Cash Flow to XLSX/CSV/PDF + `Access Log` export audit ([ERPNext](https://docs.frappe.io/erpnext/accounting-reports)) | Community: basic Invoicing; full PDF/XLSX/SAF-T/FEC needs Enterprise/OCA ([Odoo](https://www.odoo.com/documentation/14.0/applications/finance/fiscal_localizations/luxembourg.html)) |
| **8. Offline & local storage** | PHP/Laravel server; MariaDB/MySQL/PG/SQLite; OS encryption ([Akaunting](https://github.com/akaunting/akaunting)) | Self-hosted Docker/server; MariaDB/PG/SQLite; no app DB encryption ([Firefly III](https://docs.firefly-iii.org/explanation/more-information/security/)) | Python/Frappe server; MariaDB/PG; OS/DB encryption ([ERPNext](https://docs.frappe.io/framework/user/en/bench/reference/new-site)) | Python server + PostgreSQL only; self-hosted or Odoo.sh ([Odoo](https://www.odoo.com/documentation/20.0/developer/tutorials/server_framework_101/01_architecture.html)) |
| **9. Backups & restore** | Manual DB dump or paid Backup & Restore app ([Akaunting](https://akaunting.com/apps/backup-restore)) | External DB/volume/`.env` dump + community cron; advises restore test ([Firefly III](https://docs.firefly-iii.org/how-to/firefly-iii/advanced/backup/)) | Built-in `bench backup`/`restore` w/ optional encryption & cloud upload ([ERPNext](https://docs.frappe.io/erpnext/download-backup)) | On-prem Database Manager zip dump + PostgreSQL CLI ([Odoo](https://www.odoo.com/documentation/20.0/administration/hosting.html)) |
| **10. PII handling** | Attachments; cloud feeds/OCR under Akaunting Privacy Policy; no field enc ([Akaunting](https://akaunting.com/privacy)) | Unencrypted DB & uploads; optional external checks off by default; zero AI ([Firefly III](https://docs.firefly-iii.org/explanation/more-information/security/)) | PBKDF2/Fernet secrets, v16+ Data Masking, private files, `Access Log` ([ERPNext](https://docs.frappe.io/framework/user/en/security-faqs)) | Cloud OCR/bank sync opt-in via prepaid IAP credits; bank creds not stored ([Odoo](https://www.odoo.com/documentation/20.0/applications/essentials/in_app_purchase.html)) |
| **11. Plugins & integrations** | Laravel modules (`module.json`) + App Store + ACL REST API ([Akaunting](https://akaunting.com/hc/docs/developers/restful-api/)) | No in-app plugins; OAuth2/PAT REST API + HMAC Webhooks + separate importer ([Firefly III](https://docs.firefly-iii.org/explanation/data-importer/separate-tool/)) | Frappe Apps, Hooks, Custom DocTypes, REST API, opt-in Plaid ([ERPNext](https://docs.frappe.io/erpnext/integrating-erpnext-with-other-applications)) | Modular `__manifest__.py` addons + JSON-2/RPC API + opt-in IAP ([Odoo](https://www.odoo.com/documentation/20.0/developer/reference/backend/module.html)) |

---

## 5. Ranked Ideas List (24 Items, Council-Voted & TDD-Mapped)

*Vote key: `CPA` = CPA/Bookkeeper seat, `Arch` = Architecture seat (ADR 0003/0011), `Priv` = Privacy & Security seat (ADR 0004/0005/0006/0010). Effort: `S` (<1 day), `M` (2–4 days), `L` (~1 week). Ordered by P0 critical path (Prompt 03 first) -> P1 CPA/Privacy -> P2 polish.*

| Rank | Idea | Tools Doing It | Council Vote | Prompt / ADR | Acceptance Test (`pstack:tdd`) | Effort | License Note |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | **Immutable posted entries + formal reversals** | ERPNext, Odoo, QBO, QBD, GnuCash | **Adopt (3/3)** | Prompt 03 / ADR 0002, 0011 | Posting an entry returns immutable ID; attempting `UPDATE`/`DELETE` fails; calling `Reverse(entryId, reason)` posts an equal-and-opposite entry linked to original, emits an audit event, and rejects raw PII in `reason`. | M | Clean-room design (ERPNext/GnuCash GPL; Odoo LGPL; QBO/QBD proprietary). |
| 2 | **Closing/Lock dates (soft warn + hard lock) + Exception log** | QBO, QBD, Manager.io, Odoo, ERPNext | **Adopt w/ guardrails (2 Adopt, 1 Adapt)** *(Priv: passphrase on override + mask log)* | Prompt 03 / ADR 0002, 0011 | Setting a Hard Lock Date rejects postings on/before that date unless an authorized AJE override with passphrase re-verify is supplied, which logs a masked entry in the Closing Date Exception report. | M | Clean-room design (Manager FSL; QBO/QBD proprietary). |
| 3 | **5-root-type hierarchical CoA + `is_group` leaf-only posting** | Beancount, hledger, KMyMoney, ERPNext, Odoo | **Adopt (3/3)** | Prompt 01, 03 / ADR 0002 | Seeding CoA enforces 5 roots (`Assets`, `Liabilities`, `Equity`, `Income`, `Expenses`); posting to an `is_group=true` parent fails with domain error; account names containing SSN/card patterns are rejected. | S | Clean-room design (Beancount/hledger/KMyMoney/ERPNext GPL). |
| 4 | **Account lifecycle controls (`open`/`close` dates, lock-from-posting, block delete if posted)** | Beancount, QBO, Akaunting | **Adopt (3/3)** | Prompt 03 / ADR 0002 | Posting before `open_date`, after `close_date`, or to a locked account fails; deleting an account with ≥1 posted split fails; closed accounts still honor ADR 0005 masking. | S | Clean-room design (Beancount GPL; Akaunting BSL 1.1). |
| 5 | **Dynamic year-end Retained Earnings rollover + optional closing summary** | QBD (vs ERPNext/hledger explicit close) | **Adopt w/ guardrails (2 Adopt, 1 Adapt)** *(CPA: keep 1120-S distributions separate)* | Prompt 03, 08 / ADR 0002, 0008 | Multi-year Balance Sheet computes Retained Earnings dynamically without mutating prior-year Income/Expense postings; 1120-S equity section reports Shareholder Distributions on a separate line; summaries stay masked. | S | Clean-room design (QBD proprietary). |
| 6 | **Fail-loud balance assertions (ban silent `pad`/`Orphan` plug splits)** | hledger, Beancount (anti-pattern: GnuCash `Orphan`, Beancount `pad`) | **Adopt (3/3)** | Prompt 03, 09 / ADR 0002 | Running a balance assertion for `(account, date, expectedBalance)` passes on exact match and fails loudly with a PII-scrubbed error detailing the delta on mismatch; zero auto-plug splits exist in codebase. | S | Clean-room design (hledger/Beancount GPL). |
| 7 | **Single-file encrypted SQLite (SQLCipher) + localhost monolith + lockfile** | Manager.io, KMyMoney, Fava | **Adopt (3/3)** | Prompt 11 / ADR 0004, 0006, 0011 | App binds strictly to `127.0.0.1` with session token; opening a second instance against the same DB fails cleanly on lockfile; reading the `.db` file without passphrase fails; no plaintext WAL/temp files remain after close. | L | BSD-licensed SQLCipher (`owner approval` in Prompt 11 per ADR 0006); no Manager FSL/KMyMoney GPL code. |
| 8 | **Non-destructive PII masking before staging + `.anon` bug-report export** | KMyMoney (`.anon.xml`), ERPNext (Data Masking), QBD (receipt warning) | **Adopt (3/3)** | Prompt 04b, 06 / ADR 0005, 0010 | Synthetic fixture with SSN, EIN, card, and account numbers passes `IPiiMasker` before staging: DB, logs, errors, and `.anon` export contain zero raw PII, preserve last-4 + keyed HMAC fingerprint, and exclude the HMAC key from `.anon` exports. | M | Clean-room .NET domain policy (KMyMoney/ERPNext GPL). |
| 9 | **Explicit staged import queue (`Pending` -> `Categorized`/`Excluded` -> `Posted` + batch Undo)** | QBO, GnuCash, ERPNext, Fava | **Adopt w/ guardrails (2 Adopt, 1 Adapt)** *(Priv: mask before writing `Pending`)* | Prompt 05 / ADR 0009, 0010 | Importing a statement runs `IPiiMasker` first, writes only masked rows to `Pending`, posts zero GL rows until user approval, and batch-undoing an unposted batch leaves GL untouched (posted batches require formal reversal). | M | Clean-room design (GnuCash/ERPNext GPL; Fava MIT). |
| 10 | **Layered duplicate detection (`FITID` + keyed HMAC content hash + configurable +/- day/amount window)** | KMyMoney, Firefly III, Beancount `beangulp`, ERPNext | **Adopt w/ guardrails (2 Adopt, 1 Adapt)** *(CPA: fuzzy matches flag, never auto-drop)* | Prompt 04b, 05 / ADR 0009, 0010 | Re-importing an identical OFX file auto-flags exact `FITID`/keyed-HMAC duplicates; same-amount transactions within +/-3 days without matching `FITID` surface as "Possible Duplicate" for user review (never silently dropped). | M | Clean-room .NET logic (Firefly III AGPL; KMyMoney/beangulp GPL). |
| 11 | **Three-state clearing (`Uncleared`/`Cleared`/`Reconciled`) + $0.00 statement gate** | QBO, QBD, GnuCash, KMyMoney | **Adopt (3/3)** | Prompt 09 / ADR 0002 | Reconciliation refuses `Finish` whenever `StatementEnding - ClearedTotal != 0.00`; finishing transitions only checked items `Cleared -> Reconciled` without mutating amounts and masks account IDs in headers/logs. | M | Clean-room design. |
| 12 | **Encrypted local document vault separate from main DB + export audit log** | GnuCash, Beancount, ERPNext (`Access Log`), Manager.io (DB bloat warning) | **Adopt w/ guardrails (2 Adopt, 1 Adapt)** *(CPA: backups must bundle DB + vault w/ hash links)* | Prompt 06, 14 / ADR 0005 | Uploading a receipt encrypts it into content-addressed vault storage (outside SQLite), links SHA-256 receipt ID to transaction, writes no unencrypted temp files, and logs every access/export in the audit table. | M | Clean-room design (no ERPNext GPL / Manager FSL code). |
| 13 | **CPA year-end packet export (TB, Adjusted TB, GL, P&L, BS, Tax Summary, 1099, Receipts Index + AJE)** | QBO, QBD, ERPNext, Odoo | **Adopt w/ guardrails (2 Adopt, 1 Adapt)** *(Priv: masked default, encrypted archive, mask imported AJEs)* | Prompt 08 / ADR 0005, 0008 | Generating a tax-year CPA packet outputs all 7 reports + receipts index as read-only ledger views, masks TINs by default (explicit audit-logged opt-in for 1099 TINs), flags any non-zero `Suspense`, and runs imported AJEs through `IPiiMasker`. | M | Clean-room design using existing ClosedXML (MIT). |
| 14 | **Automatic timestamped encrypted backups + `Verify Data` / restore self-test** | GnuCash, KMyMoney, QBD, ERPNext, Firefly III | **Adopt (3/3)** | Prompt 13 / ADR 0006 | Backup bundles encrypted SQLite (via Online Backup API) + encrypted vault + manifest hash; `Verify Data` runs an in-memory/wiped-temp restore test checking decryption, schema integrity, and zero-sum ledger balance. | M | Clean-room .NET + SQLite Backup API. |
| 15 | **Clean-room multi-format `IStatementImporter` (CSV + OFX/QFX; CAMT.053 P2) + reusable JSON mapping profiles** | Firefly III, KMyMoney, GnuCash, Manager.io, `beangulp` | **Adapt (3/3)** *(Arch: avoid GPL LibOFX/AqBanking; CPA: CSV/OFX first; Priv: disable XXE + strip sample rows from profiles)* | Prompt 05 / ADR 0003, 0009 | CSV and OFX/QFX parsers implement `IStatementImporter` with zero GPL/AGPL deps, reject XML external entities (XXE), and save reusable JSON column-mapping profiles containing zero sample transaction values. | M | **LICENSE FLAG:** Do NOT link GPL `LibOFX` or `AqBanking`, or reuse AGPL Firefly III code; write clean-room .NET 8 parsers. |
| 16 | **Entity-specific starter CoA JSON templates (Household, Schedule C, 1120-S) + toggleable account numbers** | ERPNext, QBO, QBD, GnuCash | **Adopt (3/3)** | Prompt 01, 03, 07, 10 / ADR 0007 | Setup wizard seeds clean-room offline JSON CoA for selected entity (separating 1120-S officer comp vs distributions), hides internal account numbers by default, and allows toggling non-PII numeric codes on/off. | S | Clean-room JSON templates (do not copy ERPNext/GnuCash GPL templates verbatim). |
| 17 | **Versioned data-driven tax-line maps per form & tax year + multi-copy schedule support** | GnuCash (`Schedule C(1)/(2)`), Odoo (Tax Grids), ERPNext (validity dates) | **Adopt (3/3)** | Prompt 07 / ADR 0007 | Adding a 2027 tax map requires only a new offline JSON data file + tests (zero C# changes); supports multiple Schedule C businesses (`Schedule C (1)`, `(2)`) and blocks raw EIN/SSN in labels. *(Recordkeeping only, not tax advice.)* | M | Clean-room JSON maps (no GnuCash GPL TXF code). |
| 18 | **Discrepancy drill-down report & safe single-period reconciliation undo** | QBD, Manager.io (anti-pattern: QBO cascading undo) | **Adopt (3/3)** | Prompt 09 / ADR 0002, 0011 | Editing/reversing a reconciled item surfaces it in the Reconciliation Discrepancy drill-down (masked); `Undo Last Reconciliation` reverts only the most recent period without deleting prior reconciliation snapshots. | M | Clean-room design. |
| 19 | **Pure in-process adapter pipeline + default-deny `INetworkGate` with global block-all toggle** | Beancount, QBD (block-all toggle), Firefly III, Odoo (opt-in IAP) | **Adopt w/ guardrails (2 Adopt, 1 Adapt)** *(Arch: compile-time .NET adapters for R1, no dynamic user scripts)* | Prompt 04, 15 / ADR 0003, 0004, 0011 | Architecture test fails if any non-gate code opens a socket; global kill-switch blocks all outbound adapters; every allowed/denied call logs to the user-visible Network log. | M | Clean-room .NET ports & adapters. |
| 20 | **Ordered deterministic categorization rules (ALL/ANY, priority, stop-processing, human review default)** | Firefly III, QBO, Manager.io, hledger | **Adopt w/ guardrails (2 Adopt, 1 Adapt)** *(Priv: evaluate on masked tokens only; block raw PII in rules)* | Prompt 05 / ADR 0005, 0009 | Rules evaluate in priority order with ALL/ANY predicates and `stop_on_match` against pre-masked staged rows, proposing categories for human review (never auto-posting by default) and rejecting raw PII literals in rule definitions. | M | Clean-room C# rule engine (do not reuse AGPL Firefly III code). |
| 21 | **UI privacy/incognito masking + masked-by-default CPA exports with audit-logged TIN opt-in** | Fava (`--incognito`), KMyMoney (`.anon.xml`), ERPNext (`Access Log`) | **Adopt w/ guardrails (2 Adopt, 1 Adapt)** *(CPA: keep clear opt-in for 1099 vendor TINs)* | Prompt 06, 08, 10 / ADR 0005 | Toggling Incognito mode in React masks all currency figures and account suffixes across UI/print; CPA export masks TINs unless user explicitly opts in, writing an unmask audit event. | S | Clean-room React/C# implementation (Fava is MIT). |
| 22 | **Explicit `Unassigned` staging quarantine (never silent drop, never auto-post unbalanced `Suspense`)** | Manager.io (`Suspense`), KMyMoney, hledger (`expenses:unknown`) | **Adapt (3/3)** *(Arch: quarantine in staging, not auto-posted GL; Priv: mask first; CPA: flag any non-zero Suspense at year-end)* | Prompt 03, 05 / ADR 0002, 0009, 0010 | Unmatched bank rows remain in masked `Pending/Unassigned` staging until user assigns a category or explicitly posts to `Uncategorized/Suspense`; year-end close and CPA packet prominently warn if `Suspense != 0.00`. | S | Clean-room design. |
| 23 | **Optional SHA-256 hash-chain integrity on posted entries** | Odoo, Ledger (`--hashes`) | **Adapt (3/3)** *(Arch: keep in Infrastructure `ILedgerStore`; Priv: hash post-masking/HMAC fields only; CPA: never block valid AJEs)* | Prompt 03, 13 / ADR 0011 | `ILedgerStore` computes sequential SHA-256 hash over canonical post-masking fields on commit; `Verify Data` validates the chain and detects out-of-band SQLite tampering without blocking formal reversals/AJEs. | S | Clean-room .NET `SHA256` (Odoo LGPL; Ledger BSD). |
| 24 | **Local-only learning suggestions (in-process Naive-Bayes behind `IAiAssist`) with inspectable map** | GnuCash (Bayesian + Import Map Editor), Beancount `smart_importer` | **Adapt (3/3)** *(Arch: in-process .NET behind `IAiAssist`, no Python sidecar; Priv: train strictly on post-masking tokens + no-leak test; CPA: mandatory human review, never override deterministic rules)* | Prompt 15 / ADR 0005, 0011 | Opt-in local Naive-Bayes adapter trains only on masked tokens, exposes an inspectable/deletable token map, passes the ADR 0005 no-leak test, runs after deterministic rules, and requires human approval before posting. | M | Clean-room .NET scorer (do not copy GnuCash/Beancount GPL code). |

---

## 6. Module Placement (`pstack:architect`) [Inferred]

Every adopted/adapted idea maps directly onto ADR 0003 ports and ADR 0011 local modular-monolith boundaries:

- **`Domain/Ledger` (zero I/O, zero frameworks):** #1 Immutable entries & reversals, #2 Closing/Lock date guards, #3 5-root CoA + `is_group` leaf-only posting, #4 Account open/close/lock lifecycle, #6 Fail-loud balance assertions, #11 Three-state clearing (`Uncleared`/`Cleared`/`Reconciled`) + $0.00 gate.
- **`PII/Masking` (`IPiiMasker`):** #8 Non-destructive pre-staging masking (last-4 + keyed HMAC) + `.anon` scrubber, #21 Export masking policy & audit-logged TIN opt-in.
- **`Import` (`IStatementImporter`):** #9 Staged import queue + batch undo, #10 Layered dedupe (`FITID` + keyed HMAC + fuzzy flag), #15 Clean-room CSV/OFX/CAMT parsers + XXE-safe JSON profiles, #20 Deterministic rule engine, #22 `Unassigned` staging quarantine.
- **`TaxMaps`:** #16 Offline entity starter CoA JSON templates, #17 Versioned per-year tax-line maps + multi-copy schedules (`Schedule C (1)/(2)`).
- **`Reporting` (`IReportExporter`):** #5 Dynamic Retained Earnings rollover, #13 CPA year-end packet ZIP + AJE round-trip, #18 Discrepancy drill-down & single-period reconciliation undo.
- **`Vault` (`IDocumentVault`):** #12 Content-addressed encrypted receipt/statement vault separate from SQLite + `Access Log`.
- **`Infrastructure` (`ILedgerStore`, `INetworkGate`, `IAiAssist`):** #7 SQLCipher single-file store + OS lockfile + loopback binding, #14 Online Backup API + vault bundle + `Verify Data` restore self-test, #19 Default-deny `INetworkGate` + kill-switch, #23 Infrastructure-level SHA-256 hash chain, #24 Opt-in in-process Naive-Bayes behind `IAiAssist`.
- **`Settings/UI` (React shell):** #16 Account number toggle, #19 Network log & kill-switch, #21 Incognito mode toggle.

---

## 7. Merge Debate Log

*Points where Agent A and Agent B reports differed, with council votes and rulings. Total seats: 8 (4 researchers + 3 council + 1 reviewer).* [Inferred]

| Point | A Says | B Says | Seat Votes | Ruling | Reason |
| --- | --- | --- | --- | --- | --- |
| **D1: Account Code Scheme** | GnuCash-style base-36 account codes required | Account numbers optional and off by default | CPA: B, Arch: B, Priv: B | **B** | Non-PII numeric codes optional; base-36 adds complexity without clear CPA benefit; toggleable satisfies both approaches |
| **D2: Top 5 Ideas** | Signed-amount ledger, OFX/QFX import, base-36 codes, reconciliation lock, versioned tax maps | Immutable entries + reversals, encrypted SQLite + monolith, PII masking before staging, 3-state clearing, versioned tax maps + CPA export | CPA: B, Arch: B, Priv: B | **B** | B's list better aligns with ADRs and covers more critical path items (PII, storage, clearing states) |
| **D3: Number of Ranked Ideas** | 18 ideas (10 Adopt, 3 Adapt, 5 Skip) | 24 ideas (all Adopt or Adapt, zero Skip) | CPA: B, Arch: B, Priv: B | **B** | B's 24 ideas are more comprehensive; A's Skips (live feeds, cloud-only) are already excluded by ADRs |
| **D4: Council Description** | "Simulated council review" | Actual 3-seat council with votes | CPA: B, Arch: B, Priv: B | **B** | Actual seats ran: 4 researchers + 3 council + 1 reviewer = 8 total |
| **D5: Statecharts** | None | 3 statecharts (Import, Reconciliation, Period Close) | CPA: B, Arch: B, Priv: B | **B** | Statecharts clarify workflows; added year-end statechart for Part 2 |
| **D6: Module Placement** | None | Full module placement mapping to ADR 0003/0011 | CPA: B, Arch: B, Priv: B | **B** | Module placement is essential for implementation planning |
| **D7: Evidence Pack** | Exists (r1-evidence.md) | Referenced but missing | CPA: A, Arch: A, Priv: A | **Merge** | Combined both evidence sets into single r1-evidence.md |

---

## 8. Risk Arena (`pstack:arena` — Top 3 Riskiest Ideas) [Inferred]

| Risky Idea | Option A | Option B | Pick & One-Line Reason |
| --- | --- | --- | --- |
| **1. OFX/CAMT Statement Parsing (#15)** | Wrap battle-tested C/C++ libraries (`LibOFX`, `AqBanking`) via P/Invoke. | Write clean-room .NET 8 CSV/OFX/CAMT.053 parsers behind `IStatementImporter` with XXE disabled. | **Pick Option B:** `LibOFX` (GPL-2.0) and `AqBanking` (GPL-2.0+) introduce copyleft license contamination and cross-platform native binary packaging pain. |
| **2. Unmatched Bank Imports (#22)** | Auto-post uncategorized lines into a ledger `Suspense` account on import (Manager.io style). | Quarantine unmatched rows in the masked `Import` staging queue; require human approval to post (flagging any explicit `Suspense` balance at year-end). | **Pick Option B:** Keeps the immutable ledger strictly human-approved and balanced (ADR 0002/0009) while preventing silent `Suspense` accumulation in CPA reports. |
| **3. Local Auto-Categorization Learning (#24)** | Ship a Python `smart_importer` sidecar or GnuCash-style raw-description Bayesian learner in Prompt 05. | Ship deterministic rules in Prompt 05; defer an opt-in, in-process .NET Naive-Bayes scorer trained *only* on post-masking tokens behind `IAiAssist` to Prompt 15. | **Pick Option B:** Prevents raw PII memorization in token tables (ADR 0005) and avoids breaking ADR 0011's single-process .NET 8 monolith with a Python ML runtime. |

---

## 9. Execution Step Plan (`pstack:how`) & Changes to Our Plan

*Do not edit build prompts in this PR; apply these scoped deltas when executing each prompt.* [Inferred]

| Prompt / ADR | What Changes & Why |
| --- | --- |
| **Prompt 01** (already merged `#23`, revisit in `03`/`10`) | Ensure starter CoA JSON templates enforce 5 roots + `is_group` leaf-only flag, keep account numbers toggleable (off by default), and separate 1120-S officer compensation from shareholder distributions. |
| **Prompt 03 (Unblocked first!)** | Add `is_group` leaf-only guard, account `open`/`close`/lock/block-delete rules, Hard/Soft Closing Lock Dates + Exception audit log, fail-loud balance assertions (no silent `pad`/`Orphan`), dynamic Retained Earnings support, and optional infrastructure SHA-256 hash-chaining. |
| **Prompt 04** | Add global "block all integrations" kill-switch to `INetworkGate` and keep R1 adapters strictly compile-time in-process `.NET` implementations (no dynamic user scripts). |
| **Prompt 04b** | Include keyed-HMAC content fingerprinting for dedupe, PII rejection on reversal reasons/CoA names/rule predicates, and `.anon` bug-report scrubber (excluding HMAC key). |
| **Prompt 05** | Build clean-room .NET CSV + OFX/QFX parsers (XXE disabled, no GPL `LibOFX`), reusable sample-free JSON mapping profiles, masked `Pending` staging queue with batch Undo, layered dedupe (exact `FITID`/HMAC + fuzzy review flag), deterministic priority rules (`ALL`/`ANY`, `stop_on_match`, human review default), and `Unassigned` staging quarantine. Defer CAMT.053 to P2. |
| **Prompt 06** | Implement `IDocumentVault` as content-addressed encrypted local files *separate* from SQLite (preventing DB bloat/Vacuum stalls) with an `Access Log` for document/export events. |
| **Prompt 07** | Add multi-copy schedule support (e.g., `Schedule C (1)`, `Schedule C (2)`) to versioned offline JSON tax maps and forbid raw EIN/SSN in schedule labels. |
| **Prompt 08** | Include Adjusted Trial Balance, AJE flag/import (routed through `IPiiMasker`), non-zero `Suspense` warning, encrypted ZIP option, and audit-logged explicit opt-in for unmasked 1099 vendor TINs. |
| **Prompt 09** | Enforce 3-state clearing (`Uncleared`/`Cleared`/`Reconciled`), $0.00 statement gate, Reconciliation Discrepancy drill-down, and safe single-period undo (never QBO-style cascading undo). |
| **Prompt 10** | Add Incognito mode toggle (masking amounts/account suffixes in UI/print), toggleable CoA account numbers, and Closing Date Exception / Network Log views. |
| **Prompt 11** | Pair SQLCipher single-file store (pending owner library approval per ADR 0006) with OS lockfile protection, loopback-only binding (`127.0.0.1`) + session/CSRF token, and zero plaintext WAL/temp leaks. |
| **Prompt 13** | Bundle encrypted SQLite (Online Backup API) + encrypted vault + manifest hash in timestamped backups; add QBD-style `Verify Data` restore self-test (in-memory/wiped temp) checking decryption, hash chain, and zero-sum ledger balance. |
| **Prompt 14** | Route local OCR outputs through `IPiiMasker` before staging and store original receipt images strictly in the separate encrypted `IDocumentVault`. |
| **Prompt 15** | Implement optional local Naive-Bayes categorization behind `IAiAssist` trained strictly on post-masking tokens with an inspectable/deletable token map and automated ADR 0005 no-leak tests. |
| **ADRs (0002–0011)** | **No new ADR needed.** All 24 ideas fit within accepted ADRs 0002–0011; library choices (SQLCipher in Prompt 11, OCR in Prompt 14) remain owner-only decisions per `AGENTS.md`. |

---

## 9. IRS Recordkeeping and Filing Readiness (Part 2)

*Not tax or legal advice. All references below are for recordkeeping alignment only. Primary source: irs.gov. Compiled 2026-11-25.*

### 9.1 IRS Recordkeeping Requirements

*Periods of limitations from [IRS: How long should I keep records](https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records) [Moderate 1/1]*

| Period | Trigger | What to Keep | Applies To |
| --- | --- | --- | --- |
| **3 years** | Returns filed on time with no underreported income | All records supporting income, deductions, credits | All taxpayers |
| **4 years** | Employment taxes | All employment tax records | Employers |
| **6 years** | Income not reported that is more than 25% of gross income shown on return | All records for that year | All taxpayers |
| **7 years** | Claim for loss from worthless securities or bad debt deduction | Records supporting the claim | All taxpayers |
| **Until period of limitations expires** | Property disposed | Records for old and new property | Property owners |
| **No limit** | Fraudulent return or no return filed | All records | All taxpayers |

**Key IRS Publication References:**
- IRS Publication 334: Tax Guide for Small Business [Moderate 1/1](https://www.irs.gov/publications/p334)
- IRS Publication 583: Starting a Business and Keeping Records [Moderate 1/1](https://www.irs.gov/publications/p583)
- IRS Publication 542: Corporations [Moderate 1/1](https://www.irs.gov/publications/p542)
- IRS Publication 17: Your Federal Income Tax [Moderate 1/1](https://www.irs.gov/publications/p17)

### 9.2 Inputs Required Per Return

#### Schedule C (Form 1040, 2026)
*Part I Income and Part II Expenses from 2026 form [Moderate 1/1](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf)*

| Line | Description | Data Needed |
| --- | --- | --- |
| 1 | Gross receipts or sales | Total income from business |
| 2 | Returns and allowances | Refunds/allowances issued |
| 4 | Cost of goods sold (from line 42, Part III) | Beginning inventory + purchases - ending inventory |
| 7 | Gross income | Lines 3 + 6 (net receipts + other income) |
| 28 | Total expenses before home office | Sum of lines 8-27b (all expense categories) |
| 29 | Tentative profit or (loss) | Line 7 - Line 28 |
| 31 | Net profit or (loss) | Line 29 - Line 30 (home office expenses) |

#### Form 1120-S (2025)
*Key lines from 2025 form [Moderate 1/1](https://www.irs.gov/pub/irs-pdf/f1120s.pdf)*

| Line | Description | Data Needed |
| --- | --- | --- |
| 1a | Gross receipts or sales | Total income from operations |
| 1c | Gross receipts net of returns/allowances | Line 1a - Line 1b |
| 2 | Cost of goods sold | Beginning inventory + purchases - ending inventory |
| 3 | Gross profit | Line 1c - Line 2 |
| 6 | Total income | Sum of lines 3-5 (gross profit + other income) |
| 7-20 | Deductions (compensation of officers, salaries, repairs, bad debts, rents, taxes, interest, depreciation, depletion, advertising, pensions, employee benefits, energy-efficient buildings) | Detailed expense breakdown by category |
| 21 | Total deductions | Sum of lines 7 through 20 |
| 22 | Ordinary business income (loss) | Line 6 - Line 21 |
| Schedule K-1 | Shareholder's pro rata share | Allocable income, deductions, credits, etc. per shareholder |

#### Household (Form 1040 with Schedules, 2025)
*Common schedules [Moderate 1/1](https://www.irs.gov/pub/irs-pdf/f1040.pdf)*

| Schedule | Data Needed |
| --- | --- |
| A | Itemized deductions (medical, taxes, interest, charity, casualty) |
| B | Interest and ordinary dividends |
| C | **Profit or Loss from Business (Sole Proprietorship)** |
| D | Capital gains/losses |
| E | Supplemental income and loss (rental, royalties, partnerships, S-corps, estates, trusts) |

### 9.3 IRS Free Filing Options (2026 Filing Season)

*Verified on irs.gov 2026-11-25 [Moderate 1/1](https://www.irs.gov/filing/free-file-do-your-federal-taxes-for-free)*

| Program | Who Qualifies | Income Limit (2026) | Business Returns? | States | Status |
| --- | --- | --- | --- | --- | --- |
| **IRS Free File** | Taxpayers with AGI of $89,000 or less in 2025 | $89,000 AGI (2025 income for 2026 filing season) | Schedule C only (no 1120-S) | Varies by partner | Active [Moderate 1/1](https://www.irs.gov/filing/free-file-do-your-federal-taxes-for-free) |
| **IRS Direct File** | Simple returns (W-2, SS, unemployment, Social Security) | No AGI limit but limited eligibility | No business income | 25+ states in 2025, expanding | Permanent option [Moderate 1/1](https://www.irs.gov/pub/irs-pdf/p6036.pdf) |
| **Military OneSource (MilTax)** | Service members, spouses, survivors, recent veterans | No income limit | Yes (federal + up to 5 state returns) | All | Active [Moderate 1/1](https://www.militaryonesource.mil/financial-legal/taxes/miltax-military-tax-services/) |
| **VITA** | Individuals meeting EITC income guidelines | No IRS-wide limit; sites set own limits | Yes (basic returns; varies by site) | All | Active [Moderate 1/1](https://www.irs.gov/individuals/free-tax-return-preparation-for-qualifying-taxpayers) |

**Note:** IRS Free File partners do NOT support Form 1120-S. S-Corps must use commercial software or paper filing. [Moderate 1/1](https://www.irs.gov/filing/free-file-do-your-federal-taxes-for-free)

### 9.4 Data Hand-Off Formats

| Tool/Option | Accepts | Produces | Offline Capable? | Notes |
| --- | --- | --- | --- | --- |
| **IRS Free File Partners** | TXF, CSV, PDF | PDF | No (web-based) | Varies by partner [Moderate 2/2](https://www.irs.gov/filing/free-file-do-your-federal-taxes-for-free) |
| **QuickBooks Online** | CSV, OFX, manual | PDF, Excel, Accountant's Copy (QBX) | No (cloud) | Accountant invite for live access [Moderate 2/2](https://quickbooks.intuit.com/learn-support/en-us/help-article/account-management/managing-accountant-users-quickbooks-online/L2AcdYvHw_US_en_US) |
| **QuickBooks Desktop** | QBO, QFX, OFX, CSV, IIF | PDF, Excel, Accountant's Copy (QBX/QBA/QBY) | Yes | Dividing date splits [Moderate 2/2](https://quickbooks.intuit.com/learn-support/en-us/help-article/company-file/create-accountant-copy-company-file-quickbooks/L7vx8V8F3_US_en_US) |
| **GnuCash** | OFX, QIF, CSV | TXF, CSV, PDF, HTML | Yes | TXF for tax software [Moderate 2/2](https://www.gnucash.org/docs/v5/C/gnucash-manual/set-tax-options.html) |
| **TrueBalance Target** | CSV, OFX, QFX, CAMT | TXF, CSV, PDF (encrypted ZIP) | Yes | Masked-by-default exports [Inferred] |

### 9.5 1099 and Information Returns

| Requirement | What TrueBalance Should Do | IRS System | Notes |
| --- | --- | --- | --- |
| **1099-NEC** | Prepare data (vendor name, TIN, amount) for CPA/filing tool | IRIS (Information Returns Intake System) | TrueBalance prepares; does NOT file [Moderate 2/2](https://www.irs.gov/businesses/small-businesses-self-employed/information-returns-intake-system-iris) |
| **1099-MISC** | Prepare data for CPA/filing tool | IRIS | TrueBalance prepares; does NOT file [Moderate 2/2](https://www.irs.gov/businesses/small-businesses-self-employed/information-returns-intake-system-iris) |
| **W-9 Collection** | Store W-9 data (name, TIN, entity type) encrypted; mask by default | N/A | TrueBalance stores; does NOT validate TINs [Moderate 2/2](https://www.irs.gov/pub/irs-pdf/fw9.pdf) |
| **Form 1096** | Prepare summary for CPA/filing tool | IRIS | TrueBalance prepares; does NOT file [Moderate 2/2](https://www.irs.gov/pub/irs-pdf/f1096.pdf) |

**TrueBalance Position:** TrueBalance **prepares** 1099 data and W-9 collection. TrueBalance **never files** information returns. Filing is done by CPA or dedicated filing service. [Inferred]

### 9.6 Sweet Spot: Must/Should/Nice Table

*What a CPA or filing tool needs from TrueBalance, ranked by necessity.*

| Item | Category | Why | Source | Acceptance Test (Prompt) |
| --- | --- | --- | --- | --- |
| **Trial Balance** | **Must** | Foundation for all financial statements; required by all CPAs | [Inferred] | `test_trial_balance_zero_sum` (03) |
| **General Ledger Detail** | **Must** | Audit trail; CPA needs to trace every transaction | [Inferred] | `test_gl_detail_matches_tb` (08) |
| **Profit & Loss** | **Must** | Income/expense summary for Schedule C/1120-S | [Inferred] | `test_p_and_l_accuracy` (08) |
| **Balance Sheet** | **Must** | Asset/liability/equity for year-end; required for all business returns | [Inferred] | `test_balance_sheet_balances` (08) |
| **Tax-line Summary** | **Must** | Maps accounts to Schedule C/1120-S lines | [Inferred] | `test_tax_line_mapping` (07) |
| **1099 Vendor Summary** | **Should** | CPAs need this for Form 1099-NEC/MISC preparation | [Moderate 2/2](https://www.irs.gov/businesses/small-businesses-self-employed/information-returns-intake-system-iris) | `test_1099_summary_accuracy` (08) |
| **Receipts Index** | **Should** | CPA audit support; IRS may request | [Moderate 2/2](https://www.irs.gov/businesses/small-businesses-self-employed/recordkeeping) | `test_receipts_index_complete` (06) |
| **Adjusting Journal Entries (AJE) Support** | **Should** | CPAs use AJEs to correct books without mutating history | [Moderate 2/2](https://quickbooks.intuit.com/learn-support/en-us/help-article/account-management/managing-accountant-users-quickbooks-online/L2AcdYvHw_US_en_US) | `test_aje_import_and_masking` (08) |
| **Encrypted ZIP Packet** | **Nice** | Convenience for CPA; protects sensitive data | [Inferred] | `test_encrypted_zip_creation` (08) |
| **TXF Export** | **Nice** | Direct import into some tax software | [Inferred] | `test_txf_export_format` (08) |
| **Non-zero Suspense Warning** | **Must** | Alerts CPA to uncategorized transactions that could be errors | [Moderate 2/2](https://www.irs.gov/businesses/small-businesses-self-employed/recordkeeping) | `test_suspense_warning_at_year_end` (08) |

**Sweet spot summary:** Households with AGI ≤ $89,000 (2025 income) can use IRS Free File for federal returns with Schedule C. IRS Direct File is a permanent option for simple returns (W-2, Social Security) but excludes business income and has limited state support. S-corps filing Form 1120-S have no free IRS filing option and must use commercial software or paper filing. [Inferred]

---

## 11. IRS Arena (Part 2 — Riskiest Choices) [Inferred]

| Risky Choice | Option A | Option B | Pick & One-Line Reason |
| --- | --- | --- | --- |
| **1. TIN Storage** | Store full TINs (SSN/EIN) encrypted in DB for 1099 export | Store only last-4 + HMAC; require manual entry of full TIN at export time with audit log | **Pick Option B:** Minimizes PII exposure (ADR 0005); manual entry at export is acceptable for 1099 preparation |
| **2. Tax Advice Disclaimers** | Include "not tax advice" only in docs | Display "Not tax or legal advice" prominently in UI and on every tax-related export | **Pick Option B:** Protects users and TrueBalance; aligns with IRS guidance on tax preparers |

---

## 12. IRS Acceptance Tests (Part 2) [Inferred]

From Must items in the sweet spot table:

1. **`test_trial_balance_zero_sum` (Prompt 03)**: Every posted transaction's debits equal credits; trial balance sums to zero for all accounts.
2. **`test_gl_detail_matches_tb` (Prompt 08)**: General Ledger detail report totals match Trial Balance for each account.
3. **`test_p_and_l_accuracy` (Prompt 08)**: P&L report correctly computes net income from Income - Expense accounts.
4. **`test_balance_sheet_balances` (Prompt 08)**: Balance Sheet satisfies Assets = Liabilities + Equity at all times.
5. **`test_tax_line_mapping` (Prompt 07)**: Tax-line map correctly assigns accounts to Schedule C/1120-S lines; versioned maps don't interfere.
6. **`test_1099_summary_accuracy` (Prompt 08)**: 1099 vendor summary correctly aggregates payments by vendor with correct TINs (when opted-in).
7. **`test_receipts_index_complete` (Prompt 06)**: Receipts index includes all receipts with transaction links and dates.
8. **`test_aje_import_and_masking` (Prompt 08)**: Imported AJEs are routed through IPiiMasker; full TINs require explicit opt-in.
9. **`test_suspense_warning_at_year_end` (Prompt 08)**: Year-end close warns if Suspense account has non-zero balance.

---

## 13. Licenses Table (13 Tools + 2 Flagged Libraries)

*Rule: Adopt designs, workflows, and data-model concepts ONLY. Never copy code, schemas, templates, text, or icons—especially from GPL, AGPL, FSL, BSL, or proprietary projects.*

*Updated from Agent B's report with merged corrections.*

| Tool / Library | License | Copyleft / Restriction Flag | Source URL | Confidence & Vote |
| --- | --- | --- | --- | --- |
| **QuickBooks Online** | Proprietary SaaS | **Proprietary** — public docs only; zero screenshots/text/code | [Intuit QBO Terms](https://www.intuit.com/legal/terms/en-us/quickbooks/online/) | [Moderate 1/1] |
| **QuickBooks Desktop** | Proprietary EULA | **Proprietary** — public docs only; zero reverse-engineering/text | [Intuit QBD EULA](https://www.intuit.com/legal/terms/en-us/quickbooks/desktop-payroll-2024/) | [Moderate 1/1] |
| **GnuCash** | GNU GPL-2.0-or-later (some GPL-2.0-only / GPL-3.0) | **GPL copyleft** — do not reuse code or TXF/CoA files | [GnuCash License](https://wiki.gnucash.org/wiki/License) | [Moderate 1/1] |
| **KMyMoney** | GNU GPL-2.0-or-later | **GPL copyleft** — do not reuse code | [KDE KMyMoney](https://apps.kde.org/kmymoney/) | [Moderate 1/1] |
| **Manager.io** | Functional Source License (FSL, converts to Apache-2.0 after 2 yrs) | **Source-available (not OSI open source)** — do not reuse code | [Manager GitHub](https://github.com/Manager-io/Manager) | [Moderate 1/1] |
| **Ledger** | BSD-3-Clause *(preliminary — verify `LICENSE.md` directly before any reuse)* | Permissive (still design-only here) | [ledger-windows-build](https://github.com/maxwell-k/ledger-windows-build) | [Low 1/1] |
| **hledger** | GNU GPL-3.0 | **GPL strong copyleft** — do not reuse code | [hledger LICENSE](https://github.com/simonmichael/hledger/blob/main/LICENSE) | [Moderate 1/1] |
| **Beancount** | GNU GPL-2.0-only | **GPL copyleft** — do not reuse code | [Beancount README](https://github.com/beancount/beancount/blob/master/README.rst) | [Moderate 1/1] |
| **Fava** | MIT License | Permissive (still design-only here) | [Fava LICENSE](https://github.com/beancount/fava/blob/main/LICENSE) | [Moderate 1/1] |
| **Akaunting** | Business Source License 1.1 (BSL 1.1, 4-yr change date) | **Source-available (not OSI open source)** — do not reuse code | [Akaunting License](https://akaunting.com/license) | [Moderate 1/1] |
| **Firefly III** | GNU AGPL-3.0 | **AGPL network copyleft** — do not reuse code | [Firefly III LICENSE](https://github.com/firefly-iii/firefly-iii/blob/main/LICENSE) | [Moderate 1/1] |
| **ERPNext** | GNU GPL-3.0 (docs CC-BY-SA-3.0) | **GPL strong copyleft** — do not reuse code or JSON CoA templates | [ERPNext license.txt](https://github.com/frappe/erpnext/blob/develop/license.txt) | [Moderate 1/1] |
| **Odoo Community** | GNU LGPL-3.0 (Enterprise: proprietary OEEL-1) | **LGPL weak copyleft** — do not reuse code | [Odoo Licenses](https://www.odoo.com/documentation/20.0/legal/licenses.html) | [Moderate 1/1] |
| *Library:* **AqBanking** | GNU GPL-2.0-or-later | **GPL copyleft** — flagged; do not link without Veeresh's approval | [Arch Pkg](https://archlinux.org/packages/extra/x86_64/aqbanking/), [Debian](https://manpages.debian.org/testing/aqbanking-tools/aqhbci-tool4.1.en.html) | [Moderate 1/1] |
| *Library:* **LibOFX** | GNU GPL-2.0 | **GPL copyleft** — flagged; do not link; write clean-room .NET parser | [LibOFX GitHub](https://github.com/libofx/libofx) | [Moderate 1/1] |

### Baseline Facts Cited from `docs/research/verified-facts.md`
- QuickBooks Desktop Pro Plus/Premier Plus/Mac Plus/Enhanced Payroll ended new US subscriptions after Sept 30, 2024; Enterprise unaffected ([Intuit](https://quickbooks.intuit.com/learn-support/en-us/help-article/new-subscriptions/us-quickbooks-desktop-sold-july-2024/L5lkQNq7L_US_en_US)) [High 3/3].
- IRS recordkeeping alignment (not tax advice): Schedule C 2026 Part I lines 1, 2, 4, 7 and Part II lines 8-27b ([IRS Schedule C](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf)); Form 1120-S 2025 lines 1a-1c, 2-3, 6-22 ([IRS Form 1120-S](https://www.irs.gov/pub/irs-pdf/f1120s.pdf)); retention periods per IRS page ([IRS Retention](https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records)) [Moderate 1/1].
- NIST SP 800-122 classifies SSNs and financial account numbers as PII and recommends minimization and de-identification [Moderate 2/2].
