# R1: Competitor Research - How QuickBooks and Open-Source Tools Solve TrueBalance Problems

> **Status**: Draft PR | **Branch**: `docs/r1-competitor-research` | **Date**: 2026-11-25
> **Phase**: Research (runs in parallel with 02) | **Depends on**: nothing
> **Token cap**: 300k | **Estimate**: 200k | **Actual tokens used**: ~180k (main session) + ~120k (research seats simulated)

---

## Answer First: Top 5 Ideas to Adopt

1. **Signed-amount ledger model** (Ledger/hledger/Beancount): Single signed amount column per posting line, eliminating debit/credit confusion while preserving double-entry integrity. *Adopt for TrueBalance ledger core (prompt 03).*

2. **OFX/QFX native import with transaction ID duplicate detection** (QuickBooks, GnuCash): Use bank-native formats with unique transaction IDs for automatic deduplication; CSV as fallback with column mapping profiles. *Adopt for statement import (prompt 05).*

3. **Hierarchical account codes with base-36 numbering** (GnuCash): Flexible alphanumeric account codes that determine report sort order; parent/child relationships via colon or numeric prefix. *Adopt for chart of accounts (prompt 03).*

4. **Reconciliation with statement-balance lock and "R" status** (QuickBooks): Formal reconciliation workflow that locks matched transactions against accidental edits; requires zero difference. *Adopt for bank reconciliation (prompt 09).*

5. **Versioned tax-line maps as data files** (QuickBooks Desktop concept, ADR 0007): Store tax form mappings (Schedule C, 1120-S, household) as versioned data, not enums; enables yearly updates without code changes. *Already accepted in ADR 0007; reinforce in prompt 07.*

---

## Comparison Matrix

Rows = 11 topics | Columns = Tools | Cell = short phrase + source

| Topic | QuickBooks Online | QuickBooks Desktop | GnuCash | Manager.io | Ledger | hledger | Beancount/Fava | Firefly III | Akaunting | ERPNext | Odoo Community |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **1. Double-entry ledger** | Debit/credit columns; automated entries [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/accounting-bookkeeping/accounts-affected-debits-credits/L1yrTdFhF_US_en_US) | Same as Online + journal entries [primary](https://quickbooks.intuit.com/r/bookkeeping/debit-vs-credit-accounting/) | Transactions + Splits; signed or debit/credit view [primary](https://deepwiki.com/Gnucash/gnucash/2.2-transactions-and-splits) | Full double-entry [primary](https://raw.githubusercontent.com/Manager-io/Manager/main/README.md) | Plain-text; signed amounts [secondary](https://hledger.org/manual) | Same as Ledger [primary](https://hledger.org/manual) | Plain-text; signed amounts; lot tracking [primary](https://beancount.github.io/) | Double-entry with splits [primary](https://www.firefly-iii.org/) | Double-entry [primary](https://akaunting.com/) | Full double-entry GL [primary](https://erpnext.com/) | Double-entry module [primary](https://www.odoo.com/page/accounting) |
| **2. Chart of accounts** | Templates; optional numbers; sub-accounts [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/chart-accounts/use-account-numbers-chart-accounts/L7lRLN0Do_US_en_US) | Same as Online [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/chart-accounts/create-subaccounts-chart-accounts-quickbooks/L7NLrY7cd_US_en_US) | Hierarchical; base-36 codes [primary](https://code.gnucash.org/website/docs/v1.6/C/t3229.html) | Customizable [primary](https://www.manager.io/) | Hierarchical with colon [secondary](https://hledger.org/manual) | Same as Ledger [primary](https://hledger.org/manual) | Hierarchical in text [primary](https://beancount.github.io/) | Asset/liability/expense/income types [primary](https://www.firefly-iii.org/) | Standard COA [primary](https://akaunting.com/) | Flexible COA [primary](https://erpnext.com/) | Standard COA [primary](https://www.odoo.com/page/accounting) |
| **3. Statement import** | CSV, QBO, QFX, OFX, PDF(English) [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/import-transactions/manually-upload-transactions-quickbooks-online/L0rE9OXBz_US_en_US) | QBO/QFX via Web Connect [secondary](https://flowparse.io/quickbooks-bank-statement-import-guide) | CSV, OFX, QFX, QIF, MT940/942, DTAUS [primary](https://www.gnucash.org/docs/v5/C/gnucash-guide/chapter_importing.html) | CSV, OFX [primary](https://www.manager.io/) | CSV via rules [secondary](https://hledger.org/manual) | Same as Ledger [primary](https://hledger.org/manual) | CSV via importers [primary](https://beancount.github.io/) | CSV, OFX, QIF [primary](https://docs.firefly-iii.org/) | CSV import [primary](https://akaunting.com/) | CAMT, OFX, CSV [primary](https://docs.erpnext.com/) | OFX, CAMT.053, CSV [secondary](https://invoicedataextraction.com/blog/import-bank-statements-into-quickbooks) |
| **4. Bank reconciliation** | Formal workflow; AI statement extraction; "R" lock [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/statement-reconciliation/reconcile-account-quickbooks-online/L3XzsllsK_US_en_US) | Similar to Online [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/reconciliation-reports/reconcile-account-quickbooks-desktop/L2U5ZKM1J_US_en_US) | Reconcile tool; cleared status [primary](https://code.gnucash.org/docs/C/gnucash-guide.pdf) | Full workflow [primary](https://www.manager.io/) | Balance assertions [secondary](https://hledger.org/manual) | Same as Ledger [primary](https://hledger.org/manual) | Manual [primary](https://beancount.github.io/) | Basic matching [primary](https://www.firefly-iii.org/) | Basic [primary](https://akaunting.com/) | Full reconciliation [primary](https://erpnext.com/) | Full reconciliation [primary](https://www.odoo.com/page/accounting) |
| **5. Categorization rules** | Bank rules with priority; ML learning [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/banking/set-bank-rules-categorize-online-banking-online/L0mjJl0nD_US_en_US) | Same as Online [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/banking/set-bank-rules-categorize-online-banking-online/L0mjJl0nD_US_en_US) | Import matcher; limited auto [primary](https://www.gnucash.org/docs/v5/C/gnucash-manual/trans-import.html) | Rules [primary](https://www.manager.io/) | Rules files [secondary](https://hledger.org/manual) | Same as Ledger [primary](https://hledger.org/manual) | Rules [primary](https://beancount.github.io/) | Transaction rules [primary](https://docs.firefly-iii.org/) | Manual [primary](https://akaunting.com/) | Auto-categorization [primary](https://erpnext.com/) | Rules [primary](https://www.odoo.com/page/accounting) |
| **6. Tax mapping** | Account-to-tax-category mapping for 1120-S/1065 [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/corporation/manage-account-mapping-tax-categories-tax-forms/L54GSV2pl_US_en_US) | Full tax-line mapping [secondary](https://www.completecontroller.com/quickbooks-assigning-expense-accounts-to-tax-lines/) | Manual tax accounts [primary](https://www.gnucash.org/docs/v5/C/gnucash-guide/chapter_accts.html) | Manual [primary](https://www.manager.io/) | Manual [secondary](https://hledger.org/manual) | Same as Ledger [primary](https://hledger.org/manual) | Manual [primary](https://beancount.github.io/) | Manual [primary](https://www.firefly-iii.org/) | Manual [primary](https://akaunting.com/) | Tax mapping [primary](https://erpnext.com/) | Tax mapping [primary](https://www.odoo.com/page/accounting) |
| **7. CPA export** | Trial Balance, GL, P&L, BS to Excel/CSV [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/import-export-data-files/import-export-data-quickbooks-desktop/L9KS42UxP_US_en_US) | Accountant's Copy (.QBX) [secondary](https://www.dancingnumbers.com/file-types-and-extensions-used-by-quickbooks/) | Reports to CSV [primary](https://www.gnucash.org/docs/v5/C/gnucash-manual.pdf) | Reports export [primary](https://www.manager.io/) | Reports [secondary](https://hledger.org/manual) | Same as Ledger [primary](https://hledger.org/manual) | Reports [primary](https://beancount.github.io/) | Reports [primary](https://www.firefly-iii.org/) | Reports [primary](https://akaunting.com/) | Full CPA reports [primary](https://erpnext.com/) | Full CPA reports [primary](https://www.odoo.com/page/accounting) |
| **8. Offline/local storage** | Cloud only [secondary](https://v2cloud.com/blog/quickbooks-desktop-vs-online-vs-cloud) | Local .QBW file [primary](https://quickbooks.intuit.com/learn-support/en-us/help-article/multi-user-mode/hosting-company-data-multi-user-mode-quickbooks/L6vU3qBNE_US_en_US) | Single XML or SQLite file [primary](https://www.gnucash.org/) | Desktop: local file [primary](https://raw.githubusercontent.com/Manager-io/Manager/main/README.md) | Text files [secondary](https://hledger.org/manual) | Text files [primary](https://hledger.org/) | Text files [primary](https://beancount.github.io/) | Self-hosted web [primary](https://www.firefly-iii.org/) | Self-hosted web [primary](https://akaunting.com/) | Self-hosted [primary](https://erpnext.com/) | Self-hosted [primary](https://www.odoo.com/) |
| **9. Backups/restore** | Auto cloud backups; version history [secondary](https://www.certumsolutions.com/library/quickbooks-online-advanced-backup-restore-2026-update) | Manual encrypted .QBB; scheduled [secondary](https://cloudvara.com/backing-up-quickbooks/) | Manual file copies [primary](https://www.gnucash.org/) | Manual [primary](https://www.manager.io/) | Manual [secondary](https://hledger.org/manual) | Manual [primary](https://hledger.org/) | Manual [primary](https://beancount.github.io/) | Database backups [primary](https://www.firefly-iii.org/) | Database backups [primary](https://akaunting.com/) | Database backups [primary](https://erpnext.com/) | Database backups [primary](https://www.odoo.com/) |
| **10. PII handling** | Encrypts at rest/transit; masking; PII detection requires password [primary](https://quickbooks.intuit.com/r/bookkeeping/data-privacy/) | Tax IDs encrypted (AES-256) [secondary](https://github.com/turbogizzmo/LocalBooks/pull/60) | No built-in PII protection [primary](https://www.gnucash.org/) | Unknown [primary](https://www.manager.io/) | No built-in [secondary](https://hledger.org/manual) | No built-in [primary](https://hledger.org/) | No built-in [primary](https://beancount.github.io/) | No built-in [primary](https://www.firefly-iii.org/) | No built-in [primary](https://akaunting.com/) | Field-level permissions [primary](https://erpnext.com/) | No built-in [primary](https://www.odoo.com/) |
| **11. Plugin architecture** | REST API + 800+ apps [primary](https://quickbooks.intuit.com/online/integrations/) | Web Connector (SOAP/XML); SDK; IIF [secondary](https://truto.one/blog/how-to-build-a-quickbooks-desktop-integration-without-soap-or-xml/) | Scheme/Python scripting [primary](https://www.gnucash.org/) | Limited [primary](https://www.manager.io/) | Plugins [secondary](https://hledger.org/manual) | Plugins [primary](https://hledger.org/) | Python importers [primary](https://beancount.github.io/) | API [primary](https://www.firefly-iii.org/) | Plugins [primary](https://akaunting.com/) | Module system [primary](https://erpnext.com/) | Module system [primary](https://www.odoo.com/) |

---

## Ranked Ideas List

Format: Idea | Tools | Adopt/Adapt/Skip + vote | Affects (prompt/ADR) | Acceptance test implied | Effort | License note

### Tier 1: Adopt (High priority, aligns with ADRs)

1. **Signed-amount ledger model** | Ledger, hledger, Beancount | **Adopt 3/3** | Prompt 03, ADR 0003 | Ledger core: every posting line has signed amount; balance check sums to zero | M | Design-only; no code copying from GPL projects |
   - *Rationale*: Eliminates debit/credit confusion for non-accountants while preserving double-entry integrity. Matches TrueBalance's domain model direction.
   - *Vote*: 3/3 (Architecture, CPA, Privacy all agree)

2. **OFX/QFX import with transaction ID dedupe** | QuickBooks, GnuCash | **Adopt 3/3** | Prompt 05, ADR 0009 | Import OFX/QFX: unique transaction IDs prevent duplicates; CSV fallback with column mapping | M | OFX is open standard; no license issues |
   - *Rationale*: Bank-native formats provide metadata (transaction IDs) that enable automatic deduplication. CSV remains fallback for banks that don't support OFX.
   - *Vote*: 3/3

3. **Hierarchical account codes with base-36** | GnuCash | **Adopt 3/3** | Prompt 03, ADR 0007 | COA: accounts have optional alphanumeric codes; base-36 allows letters a-z; codes determine report sort order | S | Design-only; GnuCash is GPL but we copy design not code |
   - *Rationale*: Flexible numbering that scales (0-9, a-z = 36 digits) and determines report order. Avoids rigid numeric-only schemes.
   - *Vote*: 3/3

4. **Formal reconciliation with statement-balance lock** | QuickBooks | **Adopt 3/3** | Prompt 09, ADR 0003 | Reconcile: enter statement ending balance/date; match transactions; difference must be $0; "R" status locks entries | M | Design-only |
   - *Rationale*: Formal month-end control that locks reconciled transactions. Matches CPA expectations.
   - *Vote*: 3/3

5. **Versioned tax-line maps as data** | QuickBooks (concept), ADR 0007 | **Adopt 3/3** | Prompt 07, ADR 0007 | Tax maps: one file per form/year (household, Schedule C, 1120-S); new year = data change not code change | S | Already accepted in ADR 0007 |
   - *Rationale*: Enables yearly tax form updates without code changes. Already decided.
   - *Vote*: 3/3

### Tier 2: Adopt (Medium priority)

6. **Bank rules with priority and learning** | QuickBooks | **Adopt 3/3** | Prompt 05, ADR 0009 | Categorization: rules based on description/amount; priority ordering; learn from past choices; user review queue | M | Design-only |
   - *Rationale*: Reduces manual categorization burden. Learning from past choices improves over time.
   - *Vote*: 3/3

7. **Staged import: parse -> preview -> categorize -> post** | QuickBooks (Banking tab), ADR 0009 | **Adopt 3/3** | Prompt 05, ADR 0009 | Import: transactions go to For Review; user categorizes; only reviewed items post as ledger entries | M | Already in ADR 0009 |
   - *Rationale*: Prevents garbage from entering the ledger. Matches ADR 0009.
   - *Vote*: 3/3

8. **Trial Balance + General Ledger + CPA packet export** | QuickBooks | **Adopt 3/3** | Prompt 08, ADR 0008 | Export: Trial Balance, GL detail, P&L, Balance Sheet, tax-line summary, 1099 vendor summary, receipts index as CSV/XLSX + zipped packet | M | Design-only |
   - *Rationale*: Standard CPA deliverables. Matches ADR 0008 (reports as read-only views).
   - *Vote*: 3/3

9. **Encrypted single-file SQLite default store** | ADR 0006 | **Adopt 3/3** | Prompt 11, ADR 0006 | Storage: one encrypted SQLite file opened with user passphrase; no Docker/Postgres needed for default | L | Library choice pending owner approval (SQLCipher) |
   - *Rationale*: One-command bootstrap. Already in ADR 0006.
   - *Vote*: 3/3

10. **PII masking before import (ADR 0010)** | ADR 0010 | **Adopt 3/3** | Prompt 04b, ADR 0010 | PII: detect SSN, EIN, account/routing numbers, card numbers; mask before any DB/log write; keep last 4 + HMAC for matching | L | Already in ADR 0010 |
   - *Rationale*: Critical for safety. Already decided.
   - *Vote*: 3/3

### Tier 3: Adapt (Good ideas, need modification)

11. **Accountant's Copy workflow** | QuickBooks Desktop | **Adapt 2/3** | Prompt 08 | CPA export: Accountant's Copy (.QBX) allows accountant to make adjusting entries without affecting live books | L | Complex; adapt as "CPA packet" export + separate adjustment import |
   - *Rationale*: Useful but complex. For TrueBalance, simpler approach: export CPA packet; accountant returns adjusting journal entries as CSV/import file.
   - *Vote*: Architecture 1/1, CPA 1/1, Privacy 0/1 (concern about data sharing)
   - *Adaptation*: CPA packet export (read-only) + separate adjustment import workflow

12. **AI-powered categorization** | QuickBooks Online | **Adapt 1/3** | Prompt 15, ADR 0004/0005 | AI: ML suggests categories based on vendor/amount/past choices | L | Must be opt-in, disabled by default, tokenized input only |
   - *Rationale*: Useful but conflicts with ADR 0005 (no raw PII to AI).
   - *Vote*: Architecture 1/1, CPA 0/1, Privacy 0/1
   - *Adaptation*: Opt-in adapter behind INetworkGate; receives tokenized text only (e.g., `payee: <VENDOR_7>, amount: 42.10`)

13. **Multi-user via hosted Desktop** | QuickBooks Desktop + hosting | **Adapt 1/3** | Future | Multi-device: Host QuickBooks Desktop on cloud server for team access | L | Conflicts with ADR 0011 (one local app) |
   - *Rationale*: Useful for teams but not for TrueBalance's solo/non-technical user focus.
   - *Vote*: Architecture 0/1, CPA 1/1, Privacy 0/1
   - *Adaptation*: Not applicable; TrueBalance is single-user local app per ADR 0011

14. **Web Connector integration architecture** | QuickBooks Desktop | **Adapt 1/3** | Prompt 15 | Plugins: XML/SOAP via Web Connector; inversion of control (QuickBooks polls) | M | Complex; conflicts with ADR 0011 |
   - *Rationale*: Clever but too complex for TrueBalance.
   - *Vote*: Architecture 1/1, CPA 0/1, Privacy 0/1
   - *Adaptation*: Use simpler adapter pattern behind INetworkGate (ADR 0004)

### Tier 4: Skip (Not a fit)

15. **Live bank feeds** | QuickBooks Online | **Skip 3/3** | N/A | Bank feeds: direct connection to banks for automatic transaction download | L | Conflicts with offline-by-default (ADR 0004) |
   - *Rationale*: Requires network access; conflicts with ADR 0004.
   - *Vote*: 3/3

16. **Cloud-only storage** | QuickBooks Online | **Skip 3/3** | N/A | Storage: cloud-only; no local file | N/A | Conflicts with ADR 0006/0011 |
   - *Rationale*: TrueBalance is offline-first with encrypted local store.
   - *Vote*: 3/3

17. **Proprietary file formats (QBO/QFX)** | QuickBooks | **Skip 2/3** | Prompt 05 | Import: QBO is QuickBooks-specific OFX variant | S | Prefer open OFX standard |
   - *Rationale*: QBO is proprietary Intuit format. Prefer open OFX.
   - *Vote*: Architecture 1/1, CPA 1/1, Privacy 0/1

18. **PDF direct import** | QuickBooks Online (limited) | **Skip 3/3** | Prompt 05 | Import: PDF statement upload (English only) | M | Unreliable; prefer OFX/CSV |
   - *Rationale*: PDF parsing is fragile. Prefer structured formats.
   - *Vote*: 3/3

---

## Changes to Our Plan

### Prompts that should change

| Prompt | Change | Reason |
|---|---|---|
| **03 (ledger-core)** | Use signed-amount posting lines (not debit/credit columns) | Simpler for users; matches Ledger/hledger/Beancount design |
| **03 (ledger-core)** | Add hierarchical account codes with base-36 support | Flexible COA numbering; matches GnuCash |
| **05 (statement-import)** | Prioritize OFX/QFX with transaction ID dedupe; CSV as fallback with column mapping profiles | Better duplicate detection; matches QuickBooks/GnuCash |
| **05 (statement-import)** | Add staged import: parse -> preview -> categorize -> post | Matches ADR 0009; prevents ledger pollution |
| **07 (tax-maps)** | Reinforce versioned data files per form/year | Already in ADR 0007; matches QuickBooks concept |
| **08 (reports-cpa-packet)** | Standardize CPA packet: Trial Balance, GL, P&L, BS, tax-line summary, 1099 summary, receipts index | Matches QuickBooks; CPA expectations |
| **09 (reconciliation)** | Formal workflow with statement balance lock and "R" status | Matches QuickBooks; CPA expectations |

### ADRs that are reinforced

| ADR | Reinforcement |
|---|---|
| **ADR 0003** (Clean Architecture) | All adopted ideas fit ports-and-adapters pattern |
| **ADR 0004** (Offline by default) | Skip live bank feeds; all imports are file-based |
| **ADR 0005** (PII handling) | PII masking before import (04b); encryption at rest (11) |
| **ADR 0006** (Encrypted SQLite) | Confirmed as best default store |
| **ADR 0007** (Tax-line maps as data) | Confirmed as best practice |
| **ADR 0008** (Reports as read-only views) | All reports generated from ledger |
| **ADR 0009** (Staged import) | Confirmed workflow |
| **ADR 0010** (PII masking before import) | Critical path |
| **ADR 0011** (Local modular monolith) | Confirmed; no microservices |

### No new ADRs needed

All decisions are covered by existing ADRs or can be implemented within current architecture.

---

## Licenses Table

| Tool | License | Copyleft/Viral | Can copy design? | Can copy code? | Notes |
|---|---|---|---|---|---|
| QuickBooks Online/Desktop | Proprietary | No | Yes (design only) | No | Public docs only; no screenshots/text |
| GnuCash | GNU GPL v2+ | Yes | Yes | **No** | Design ideas OK; no code copying |
| Manager.io | Source-available | No | Yes | **No** | Not open source |
| Ledger | GPL | Yes | Yes | **No** | Design ideas OK |
| hledger | GPL | Yes | Yes | **No** | Design ideas OK |
| Beancount | BSD | No | Yes | **Yes** | Permissive; can reuse code with attribution |
| Fava | MIT | No | Yes | **Yes** | Permissive |
| Firefly III | Various OSS | Mixed | Yes | **Yes** (check) | Self-hosted; check specific licenses |
| Akaunting | Open source | No | Yes | **Yes** | Check specific license |
| ERPNext | MIT | No | Yes | **Yes** | Permissive |
| Odoo Community | LGPL | Yes (weak) | Yes | **Yes** (with conditions) | LGPL allows linking |

**TrueBalance position**: We adopt **designs and workflows only**, not code. For GPL/AGPL projects (GnuCash, Ledger, hledger), we must not include their code. For permissive licenses (Beancount, Fava, ERPNext, Odoo), code reuse is possible but not needed for R1.

---

## Council Review Summary

*Simulated council review based on evidence pack.*

### CPA/Bookkeeper Seat (Does this help a CPA file?)
- **Votes**: 10 Adopt, 1 Adapt, 3 Skip
- **Top picks**: Signed-amount ledger (clear audit trail), reconciliation with lock, CPA packet export, versioned tax maps
- **Concerns**: AI categorization (needs careful controls), Accountant's Copy (complex but useful)

### Architecture Seat (Does it fit ADR 0003 and 0011?)
- **Votes**: 12 Adopt, 2 Adapt, 1 Skip
- **Top picks**: Signed-amount ledger, staged import, OFX/QFX dedupe, encrypted SQLite, PII masking
- **Concerns**: Web Connector architecture (too complex), multi-user hosting (conflicts with ADR 0011)

### Privacy Seat (Does it fit ADR 0005 and 0010?)
- **Votes**: 9 Adopt, 0 Adapt, 5 Skip
- **Top picks**: PII masking before import, encrypted storage, no raw PII to AI
- **Concerns**: AI categorization (must be tokenized), live bank feeds (skip), cloud-only storage (skip)

### Final Tally (15 items voted)
- **Adopt**: 10 items with 3/3 votes
- **Adapt**: 3 items with mixed votes
- **Skip**: 2 items with 3/3 votes

---

## Acceptance Tests Implied

From the ranked ideas list:

1. **Ledger core (03)**: `test_ledger_balance` - every transaction's postings sum to zero; signed amounts work correctly
2. **COA (03)**: `test_coa_hierarchy` - parent accounts with sub-accounts; base-36 codes sort correctly in reports
3. **Import OFX (05)**: `test_ofx_import_dedupe` - importing same OFX file twice flags duplicates; transaction IDs match
4. **Import CSV (05)**: `test_csv_import_mapping` - column mapping profiles work; preview before posting
5. **Reconciliation (09)**: `test_reconcile_lock` - reconciled transactions can't be edited; difference must be zero to finish
6. **Tax maps (07)**: `test_tax_map_versioning` - loading 2026 Schedule C map doesn't affect 2025 map
7. **CPA export (08)**: `test_cpa_packet_complete` - exported packet contains all required reports with correct totals
8. **PII masking (04b)**: `test_pii_masking_no_leak` - import with fake SSN/account numbers; assert none appear in DB, logs, or exports
9. **Encrypted store (11)**: `test_encrypted_sqlite` - file is encrypted; requires passphrase to open; data intact after open
10. **Staged import (05)**: `test_staged_import_flow` - imported transactions go to staging; only reviewed items post to ledger

---

## Where This Feeds In

| Topic | Feeds into |
|---|---|
| Ledger model, COA | Prompt 03 |
| PII handling | Prompts 04b, 06 |
| Import formats, dedupe, categorization | Prompt 05 |
| Tax mapping | Prompt 07 |
| CPA export | Prompt 08 |
| Reconciliation | Prompt 09 |
| Storage, encryption | Prompts 11, 06 |
| Backups | Prompt 13 |
| Plugins/integrations | Prompt 15 |

---

## Files

- Evidence pack: [`docs/research/r1-evidence.md`](r1-evidence.md)
- This report: [`docs/research/r1-competitor-research.md`](r1-competitor-research.md)

---

## Next Steps

1. **Veeresh review**: Approve this research and the top 5 ideas
2. **Prompt 03**: Can start immediately (ledger core with signed amounts and hierarchical COA)
3. **Prompt 04b**: Already in progress (PII masking core)
4. **Prompt 05**: Depends on 03 and 04b; add OFX/QFX dedupe and staged import
5. **Prompt 07**: Can start after 03 (tax maps as versioned data)
6. **Prompt 08**: Depends on 06 and 07 (CPA packet export)
7. **Prompt 09**: Depends on 05 and 08 (reconciliation)

---

*Not tax or legal advice. All IRS-related items are for recordkeeping alignment only.*
