# R1 Evidence Pack: Competitor Research + IRS (Part 2)

> Date: 2026-11-25 (merged A+B) | Researchers: 4 seats + merged | Format: tool, topic, finding, source URL, source type, date read

## Legend
- **Source type**: primary = tool's own docs/code/maintainers; secondary = blogs/forums/third-party
- **Date**: when the page was read (2026-11-25 unless noted)
- **Confidence tags**: [High 3/3] = 3+ agreeing primary sources; [Moderate 2/2] = 2 agreeing primary sources; [Moderate 1/1] = 1 primary source; [Low 1/2] = 1 secondary source or conflicting; analysis lines labeled [Inferred]; link checks labeled [Tested]

---

## QuickBooks Online & Desktop

### 1. Double-entry ledger model
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks Online | Double-entry ledger | Uses double-entry accounting; each transaction changes two or more accounts in the ledger; debits and credits columns | [Intuit: How accounts are affected by debits and credits](https://quickbooks.intuit.com/learn-support/en-us/help-article/accounting-bookkeeping/accounts-affected-debits-credits/L1yrTdFhF_US_en_US) | primary | 2026-11-25 |
| QuickBooks Online | Ledger columns | Debits in left column, credits in right column; columns must balance | [Intuit: Double-Entry Bookkeeping Guide](https://quickbooks.intuit.com/global/resources/bookkeeping/double-entry-bookkeeping-guide/) | primary | 2026-11-25 |
| QuickBooks Online | Automation | Automates most debit/credit entries behind the scenes for invoices, payments, expenses | [Intuit: Debit vs credit](https://quickbooks.intuit.com/r/bookkeeping/debit-vs-credit-accounting/) | primary | 2026-11-25 |

### 2. Chart of accounts
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks Online | COA templates | Pre-defined templates for different business types; can import/export COA via Excel template | [Intuit: Set up chart of accounts using template](https://quickbooks.intuit.com/learn-support/en-au/help-article/accounting-bookkeeping/set-chart-accounts-using-quickbooks-online/L8cVpsWi7_AU_en_AU) | primary | 2026-11-25 |
| QuickBooks Online | Account numbering | Optional account numbers; helps organize and categorize; can show on reports | [Intuit: Use account numbers](https://quickbooks.intuit.com/learn-support/en-us/help-article/chart-accounts/use-account-numbers-chart-accounts/L7lRLN0Do_US_en_US) | primary | 2026-11-25 |
| QuickBooks Online | Sub-accounts | Supports parent accounts with sub-accounts; both must use same account type | [Intuit: Create subaccounts](https://quickbooks.intuit.com/learn-support/en-us/help-article/chart-accounts/create-subaccounts-chart-accounts-quickbooks/L7NLrY7cd_US_en_US) | primary | 2026-11-25 |
| QuickBooks Online | COA structure | Standard: 1000s Assets, 2000s Liabilities, 3000s Equity, 4000s Income, 5000s+ Expenses | [Accountix: COA structure](https://accountixsolutions.com/blog/chart-of-accounts-for-quickbooks-the-secret-to-organized-bookkeeping) | secondary | 2026-11-25 |

### 3. Statement import
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks Online | Supported formats | CSV, QBO, QFX, OFX upload; PDF statement upload (English only) | [Intuit: Manually upload transactions](https://quickbooks.intuit.com/learn-support/en-us/help-article/import-transactions/manually-upload-transactions-quickbooks-online/L0rE9OXBz_US_en_US) | primary | 2026-11-25 |
| QuickBooks Online | Duplicate detection | QBO/QFX files contain unique transaction IDs enabling automatic duplicate detection; CSV lacks this | [Invoice Data Extraction](https://invoicedataextraction.com/blog/import-bank-statements-into-quickbooks) | secondary | 2026-11-25 |
| QuickBooks Online | File size limit | 350 KB max per upload; up to 1,000 lines | [Intuit: Upload transactions](https://quickbooks.intuit.com/learn-support/en-us/help-article/import-transactions/manually-upload-transactions-quickbooks-online/L0rE9OXBz_US_en_US) | primary | 2026-11-25 |
| QuickBooks Desktop | Import formats | QBO (Web Connect) via Bank Feeds Center; no CSV direct import | [FlowParse: QBO vs CSV](https://flowparse.io/quickbooks-bank-statement-import-guide) | secondary | 2026-11-25 |

### 4. Bank reconciliation
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks Online | Reconcile workflow | Accounting > Reconcile; enter statement ending balance/date; match transactions; difference must be $0.00 | [Intuit: Reconcile account](https://quickbooks.intuit.com/learn-support/en-us/help-article/statement-reconciliation/reconcile-account-quickbooks-online/L3XzsllsK_US_en_US) | primary | 2026-11-25 |
| QuickBooks Online | Lock status | Matched transactions marked with "R" (reconciled) status; locks against accidental edits | [BankReconciler: Workflow](https://bankreconciler.app/blogQuickBooksReconciliation) | secondary | 2026-11-25 |
| QuickBooks Online | AI reconciliation | AI Powered Reconciliation extracts statement details from uploaded PDF | [Intuit: Reconcile workflow](https://quickbooks.intuit.com/learn-support/en-us/help-article/reconciliation-reports/learn-reconcile-workflow-quickbooks/L8ZibUuVE_US_en_US) | primary | 2026-11-25 |
| QuickBooks Desktop | Reconcile | Similar workflow; beginning balance auto-filled from last reconciliation | [Intuit: Reconcile Desktop](https://quickbooks.intuit.com/learn-support/en-us/help-article/reconciliation-reports/reconcile-account-quickbooks-desktop/L2U5ZKM1J_US_en_US) | primary | 2026-11-25 |

### 5. Categorization rules
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks Online | Bank rules | Automatically categorize based on conditions (contains/doesn't contain/is exactly); set transaction type, category, tags, payee | [Intuit: Set bank rules](https://quickbooks.intuit.com/learn-support/en-us/help-article/banking/set-bank-rules-categorize-online-banking-online/L0mjJl0nD_US_en_US) | primary | 2026-11-25 |
| QuickBooks Online | Rule priority | Rules can be reordered via drag-and-drop; processed in order; up to 2,000 rules | [Books Automator](https://booksautomator.com/guides/quickbooks-advanced-bank-rules/) | secondary | 2026-11-25 |
| QuickBooks Online | Learning | App learns from past categorizations; suggestions improve with use | [Intuit: Bank rules](https://quickbooks.intuit.com/learn-support/en-us/help-article/banking/set-bank-rules-categorize-online-banking-online/L0mjJl0nD_US_en_US) | primary | 2026-11-25 |
| QuickBooks Online | AI categorization | Uses ML to analyze vendor name, amount, and past categorizations; can learn bad habits | [QBO AI Secrets](https://www.yourbusinessaccountant.com/ybanewsqbo-ai-secrets-revealed-what-experts-don-t-want-you-to-know-about-automated-categorization) | secondary | 2026-11-25 |

### 6. Tax mapping
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks Online | Account mapping | Map COA accounts to tax categories for 1120-S, 1065 forms; default mappings exist | [Intuit: Manage account mapping S-corp](https://quickbooks.intuit.com/learn-support/en-us/help-article/corporation/manage-account-mapping-tax-categories-tax-forms/L54GSV2pl_US_en_US) | primary | 2026-11-25 |
| QuickBooks Online | Schedule C | Sole proprietors map to Schedule C lines; partnerships to 1065; S-corps to 1120-S | [Complete Controller](https://www.completecontroller.com/quickbooks-assigning-expense-accounts-to-tax-lines/) | secondary | 2026-11-25 |
| QuickBooks Desktop | Full tax-line mapping | Desktop has full Tax-Line Mapping drop-downs; QBO has limited via Prep for Taxes | [Complete Controller](https://www.completecontroller.com/quickbooks-assigning-expense-accounts-to-tax-lines/) | secondary | 2026-11-25 |

### 7. Accountant/CPA export
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks Online | Export formats | Trial Balance, General Ledger, P&L, Balance Sheet exportable to Excel/CSV | [Intuit: Export financials](https://quickbooks.intuit.com/learn-support/en-us/help-article/import-export-data-files/import-export-data-quickbooks-desktop/L9KS42UxP_US_en_US) | primary | 2026-11-25 |
| QuickBooks Desktop | Accountant's Copy | .QBX file format; allows accountant to make adjusting entries; changes imported back | [DancingNumbers: File types](https://www.dancingnumbers.com/file-types-and-extensions-used-by-quickbooks/) | secondary | 2026-11-25 |
| QuickBooks | CPA packet | Typical exports: Trial Balance, P&L, Balance Sheet, General Ledger, AP/AR aging | [Dimercurio: Export financials](https://www.dimercurioadvisors.com/learning-center/how-do-i-export-my-financials-from-quickbooks) | secondary | 2026-11-25 |
| QuickBooks | Reports location | Trial Balance and General Ledger under Accountant & Taxes menu | [Dimercurio](https://www.dimercurioadvisors.com/learning-center/how-do-i-export-my-financials-from-quickbooks) | secondary | 2026-11-25 |

### 8. Offline and local storage
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks Desktop | Local file | .QBW company file stored locally; multi-user mode requires Database Server Manager | [Intuit: Multi-user mode](https://quickbooks.intuit.com/learn-support/en-us/help-article/multi-user-mode/hosting-company-data-multi-user-mode-quickbooks/L6vU3qBNE_US_en_US) | primary | 2026-11-25 |
| QuickBooks Desktop | File format | Single-file SQLite-like storage; can be hosted for multi-user access | [Truto: Integration](https://truto.one/blog/how-to-build-a-quickbooks-desktop-integration-without-soap-or-xml/) | secondary | 2026-11-25 |
| QuickBooks Online | Cloud only | No local file; data stored on Intuit servers; automatic cloud backups | [V2 Cloud: Desktop vs Online](https://v2cloud.com/blog/quickbooks-desktop-vs-online-vs-cloud) | secondary | 2026-11-25 |

### 9. Backups and restore
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks Online | Automatic backups | Automatic daily cloud backups; version history; Advanced can restore to historical date/time | [Cloudvara: QBO Advanced backup](https://www.certumsolutions.com/library/quickbooks-online-advanced-backup-restore-2026-update) | secondary | 2026-11-25 |
| QuickBooks Desktop | Manual backups | Built-in backup creates .QBB files; always use encryption | [Cloudvara: Backing up](https://cloudvara.com/backing-up-quickbooks/) | secondary | 2026-11-25 |
| QuickBooks Desktop | Scheduled backups | Can schedule daily/weekly/monthly backups to local or external drive | [Workyard: Backup guide](https://www.workyard.com/quickbooks-construction/quickbooks-backup) | secondary | 2026-11-25 |
| QuickBooks Desktop | Encryption | AES 256-bit encryption in Enterprise; Pro/Premier use 256-bit | [Intuit: Desktop Enterprise](https://quickbooks.intuit.com/desktop/enterprise/) | primary | 2026-11-25 |
| QuickBooks | Restore testing | Recommended to test restores periodically; create temporary company file from backup | [Cloudvara: Backing up](https://cloudvara.com/backing-up-quickbooks/) | secondary | 2026-11-25 |

### 10. PII handling
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks | Data privacy | Encrypts files at rest and in transit; data masking swaps sensitive details with fake values | [Intuit: Data privacy](https://quickbooks.intuit.com/r/bookkeeping/data-privacy/) | primary | 2026-11-25 |
| QuickBooks Desktop | PII detection | Detects SSN, credit card info, other PII; requires strong password when present | [Intuit: Password security Desktop](https://quickbooks.intuit.com/learn-support/en-ca/help-article/security-risk/password-security-quickbooks-desktop/L6xfyXnvJ_CA_en_CA) | primary | 2026-11-25 |
| QuickBooks Desktop | Tax ID encryption | Vendor tax IDs stored encrypted with QuickBooks' own key (AES-256) | [GitHub: LocalBooks PR](https://github.com/turbogizzmo/LocalBooks/pull/60) | secondary | 2026-11-25 |
| QuickBooks | AI boundary | MCP server returns PII (names, emails, phone, tax IDs) unless DLP layer added | [Strac: QuickBooks MCP](https://www.strac.io/blog/quickbooks-mcp-server) | secondary | 2026-11-25 |

### 11. Plugin and integration architecture
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| QuickBooks Online | REST API | Standard REST API with OAuth 2.0; multi-tenant | [Hotglue: QBO integration](https://hotglue.com/blog/quickbooks-integration-best-practices) | secondary | 2026-11-25 |
| QuickBooks Desktop | Web Connector | XML-based; QuickBooks polls external SOAP server; inversion of control architecture | [Truto: Desktop integration](https://truto.one/blog/how-to-build-a-quickbooks-desktop-integration-without-soap-or-xml/) | secondary | 2026-11-25 |
| QuickBooks Desktop | Integration methods | SDK (qbXML), Web Connector, IIF (file-based), QODBC (ODBC driver) | [Satva: Integration methods](https://satvasolutions.com/blog/best-way-to-do-integration-with-quickbooks-desktop) | secondary | 2026-11-25 |
| QuickBooks Online | App ecosystem | 800+ integrations in Intuit Marketplace | [Intuit: Integrations](https://quickbooks.intuit.com/online/integrations/) | primary | 2026-11-25 |
| QuickBooks Desktop | Menu extensions | Can add menu items to QuickBooks UI via SDK | [Intuit: UI integration](https://developer.intuit.com/app/developer/qbdesktop/docs/develop/tutorials/integrating-with-the-quickbooks-ui) | primary | 2026-11-25 |

---

## GnuCash

### 1. Double-entry ledger model
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | Ledger model | Transactions and Splits; each transaction has 2+ splits; signed amounts (no separate debit/credit columns) | [DeepWiki: Transactions and Splits](https://deepwiki.com/Gnucash/gnucash/2.2-transactions-and-splits) | primary | 2026-11-25 |
| GnuCash | Column options | Can use formal accounting labels (Debit/Credit) or common labels (Deposit/Withdrawal) | [GnuCash: Rule of Double-Entry](https://code.gnucash.org/website/docs/v1.6/C/x2527.html) | primary | 2026-11-25 |
| GnuCash | Reversals | Reverse a transaction by swapping debit/credit or sign of values | [GnuCash mailing list](https://lists.gnucash.org/pipermail/gnucash-user/2011-August/040980.html) | primary | 2026-11-25 |
| GnuCash | Precision | Custom fixed-point arithmetic (GncNumeric) avoids floating-point errors | [DeepWiki](https://deepwiki.com/Gnucash/gnucash/2.2-transactions-and-splits) | primary | 2026-11-25 |

### 2. Chart of accounts
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | COA structure | Hierarchical tree; parent accounts with sub-accounts | [GnuCash: Creating COA](https://code.gnucash.org/website/docs/v2.2/C/gnucash-help/chart-create.html) | primary | 2026-11-25 |
| GnuCash | Account numbering | Optional alphanumeric codes; base-36 (0-9, a-z); determines report sort order | [GnuCash: COA numbering](https://code.gnucash.org/website/docs/v1.6/C/t3229.html) | primary | 2026-11-25 |
| GnuCash | Templates | Account hierarchy templates; can import/export | [GnuCash Wiki: Account Hierarchy Template](https://wiki.gnucash.org/wiki/Account_Hierarchy_Template) | primary | 2026-11-25 |

### 3. Statement import
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | Supported formats | CSV, OFX, QFX, QIF, MT940, MT942, DTAUS | [GnuCash: Importing from files](https://www.gnucash.org/docs/v5/C/gnucash-guide/chapter_importing.html) | primary | 2026-11-25 |
| GnuCash | PDF import | Does NOT read PDF directly; must convert to CSV/OFX first | [BankXLSX: GnuCash import](https://bankxlsx.com/blog/how-to-import-bank-statements-into-gnucash) | secondary | 2026-11-25 |
| GnuCash | Duplicate detection | OFX/QFX have unique transaction IDs for automatic deduplication; CSV requires manual review | [Quick Bank Convert](https://www.quickbankconvert.com/blog/format-guides/import-bank-statements-into-gnucash) | secondary | 2026-11-25 |
| GnuCash | Import workflow | File > Import > [format]; OFX/QFX skip column mapping; CSV uses assistant | [GnuCash: Import OFX/QFX](https://gnucash.org/docs/v5/C/gnucash-manual/trans-import.html) | primary | 2026-11-25 |

### 4. Bank reconciliation
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | Reconcile | Tools > Reconcile; mark transactions as cleared; difference must be zero | [GnuCash Docs](https://www.gnucash.org/docs/) | primary | 2026-11-25 |
| GnuCash | Reconcile status | Transactions marked as "reconciled" (R) or "cleared" (C) | [GnuCash Guide](https://code.gnucash.org/docs/C/gnucash-guide.pdf) | primary | 2026-11-25 |

### 5. Categorization rules
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | Auto-categorization | Limited; primarily manual categorization during import | [GnuCash Docs](https://www.gnucash.org/docs/) | primary | 2026-11-25 |
| GnuCash | Import matcher | Generic import transaction matcher for OFX/QFX and CSV | [GnuCash: Import matcher](https://www.gnucash.org/docs/v5/C/gnucash-manual/trans-import.html) | primary | 2026-11-25 |

### 6. Tax mapping
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | Tax tracking | Tax accounts can be created; manual mapping to tax forms | [GnuCash: Accounts](https://www.gnucash.org/docs/v5/C/gnucash-guide/chapter_accts.html) | primary | 2026-11-25 |

### 7. Accountant/CPA export
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | Export | Reports exportable to CSV; General Ledger, Trial Balance available | [GnuCash: Export](https://www.gnucash.org/docs/v5/C/gnucash-manual.pdf) | primary | 2026-11-25 |

### 8. Offline and local storage
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | File format | Single XML file (.gnucash) or SQLite database; fully offline | [GnuCash](https://www.gnucash.org/) | primary | 2026-11-25 |
| GnuCash | Multi-device | File-based; can open same file from different locations but not simultaneously | [GnuCash](https://www.gnucash.org/) | primary | 2026-11-25 |

### 9. Backups and restore
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | Backups | Manual file copies; XML files are human-readable | [GnuCash](https://www.gnucash.org/) | primary | 2026-11-25 |

### 10. PII handling
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | PII | No built-in PII detection or masking; plaintext storage | [GnuCash](https://www.gnucash.org/) | primary | 2026-11-25 |

### 11. Plugin and integration architecture
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| GnuCash | Plugins | Scheme (Guile) scripting; Python bindings; import/export modules | [GnuCash](https://www.gnucash.org/) | primary | 2026-11-25 |
| GnuCash | Architecture | Monolithic; extensions via scripting | [GnuCash](https://www.gnucash.org/) | primary | 2026-11-25 |

---

## Manager.io

### 1. Double-entry ledger model
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Manager.io | Ledger | Full double-entry; journal entries with debit/credit | [Manager README](https://raw.githubusercontent.com/Manager-io/Manager/main/README.md) | primary | 2026-11-25 |

### 2. Chart of accounts
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Manager.io | COA | Customizable; templates available | [Manager](https://www.manager.io/) | primary | 2026-11-25 |

### 3. Statement import
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Manager.io | Import | CSV, OFX import; bank feeds | [Manager](https://www.manager.io/) | primary | 2026-11-25 |

### 4. Bank reconciliation
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Manager.io | Reconcile | Full reconciliation workflow | [Manager](https://www.manager.io/) | primary | 2026-11-25 |

### 5-11. Other topics
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Manager.io | License | Source-available, not open source | [Manager README](https://raw.githubusercontent.com/Manager-io/Manager/main/README.md) | primary | 2026-11-25 |
| Manager.io | Offline | Desktop edition is free, offline, Windows/macOS/Linux | [Manager README](https://raw.githubusercontent.com/Manager-io/Manager/main/README.md) | primary | 2026-11-25 |

---

## Ledger

### 1. Double-entry ledger model
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Ledger | Ledger model | Plain-text double-entry journal; signed amounts; no debit/credit columns | [hledger manual](https://hledger.org/manual) | secondary | 2026-11-25 |

### 2. Chart of accounts
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Ledger | COA | Accounts defined in text file; hierarchical with colon separator | [hledger manual](https://hledger.org/manual) | secondary | 2026-11-25 |

### 3. Statement import
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Ledger | Import | CSV via rules files; no native OFX/QFX | [hledger manual](https://hledger.org/manual) | secondary | 2026-11-25 |

### 4. Bank reconciliation
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Ledger | Reconcile | Balance assertions in journal; manual reconciliation | [hledger manual](https://hledger.org/manual) | secondary | 2026-11-25 |

### 5-11. Other topics
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Ledger | License | GPL | [hledger](https://hledger.org/) | primary | 2026-11-25 |
| Ledger | Offline | Text files; fully offline | [hledger](https://hledger.org/) | primary | 2026-11-25 |

---

## hledger

### 1-11. All topics
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| hledger | Ledger model | Plain-text double-entry; signed amounts | [hledger manual](https://hledger.org/manual) | primary | 2026-11-25 |
| hledger | COA | Hierarchical accounts with colon separator | [hledger manual](https://hledger.org/manual) | primary | 2026-11-25 |
| hledger | Import | CSV via rules files | [hledger manual](https://hledger.org/manual) | primary | 2026-11-25 |
| hledger | Reconciliation | Balance assertions | [hledger manual](https://hledger.org/manual) | primary | 2026-11-25 |
| hledger | License | GPL | [hledger](https://hledger.org/) | primary | 2026-11-25 |
| hledger | Offline | Text files; fully offline | [hledger](https://hledger.org/) | primary | 2026-11-25 |

---

## Beancount & Fava

### 1. Double-entry ledger model
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Beancount | Ledger model | Plain-text double-entry; signed amounts; lot tracking | [Beancount](https://beancount.github.io/) | primary | 2026-11-25 |

### 2. Chart of accounts
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Beancount | COA | Accounts in text file; hierarchical | [Beancount](https://beancount.github.io/) | primary | 2026-11-25 |

### 3. Statement import
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Beancount | Import | CSV via importers; Fava has import UI | [Beancount](https://beancount.github.io/) | primary | 2026-11-25 |

### 4-11. Other topics
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Beancount | License | BSD | [Beancount](https://beancount.github.io/) | primary | 2026-11-25 |
| Beancount/Fava | Offline | Text files; Fava is web UI | [Fava](https://fava.pythonanywhere.com/) | primary | 2026-11-25 |
| Fava | Web UI | Local web server for browsing ledger | [Fava](https://fava.pythonanywhere.com/) | primary | 2026-11-25 |

---

## Firefly III

### 1. Double-entry ledger model
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Firefly III | Ledger | Double-entry; uses transactions with splits | [Firefly III](https://www.firefly-iii.org/) | primary | 2026-11-25 |

### 2. Chart of accounts
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Firefly III | COA | Asset, liability, expense, income account types | [Firefly III](https://www.firefly-iii.org/) | primary | 2026-11-25 |

### 3. Statement import
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Firefly III | Import | CSV, OFX, QIF import; bank connection via Spectre | [Firefly III Docs](https://docs.firefly-iii.org/) | primary | 2026-11-25 |

### 4. Bank reconciliation
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Firefly III | Reconcile | Basic reconciliation; match transactions | [Firefly III](https://www.firefly-iii.org/) | primary | 2026-11-25 |

### 5. Categorization rules
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Firefly III | Rules | Transaction rules for auto-categorization | [Firefly III Docs](https://docs.firefly-iii.org/) | primary | 2026-11-25 |

### 6-11. Other topics
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Firefly III | License | Self-hosted; open source | [Firefly III](https://www.firefly-iii.org/) | primary | 2026-11-25 |
| Firefly III | Offline | Self-hosted PHP app; requires server | [Firefly III](https://www.firefly-iii.org/) | primary | 2026-11-25 |
| Firefly III | PII | Plaintext storage; no built-in PII protection | [Firefly III](https://www.firefly-iii.org/) | primary | 2026-11-25 |

---

## Akaunting

### 1. Double-entry ledger model
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Akaunting | Ledger | Double-entry accounting | [Akaunting](https://akaunting.com/) | primary | 2026-11-25 |

### 2-11. Other topics
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Akaunting | License | Open source | [Akaunting](https://akaunting.com/) | primary | 2026-11-25 |
| Akaunting | Offline | Self-hosted web app | [Akaunting](https://akaunting.com/) | primary | 2026-11-25 |

---

## ERPNext

### 1. Double-entry ledger model
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| ERPNext | Ledger | Full double-entry; General Ledger | [ERPNext](https://erpnext.com/) | primary | 2026-11-25 |

### 2-11. Other topics
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| ERPNext | License | MIT (open source) | [ERPNext](https://erpnext.com/) | primary | 2026-11-25 |
| ERPNext | Offline | Self-hosted; can be offline | [ERPNext](https://erpnext.com/) | primary | 2026-11-25 |
| ERPNext | Import | Bank statement import via CAMT, OFX | [ERPNext Docs](https://docs.erpnext.com/) | primary | 2026-11-25 |

---

## Odoo Community

### 1. Double-entry ledger model
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Odoo | Ledger | Double-entry accounting module | [Odoo Accounting](https://www.odoo.com/page/accounting) | primary | 2026-11-25 |

### 2-11. Other topics
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| Odoo | License | LGPL (Community edition) | [Odoo](https://www.odoo.com/) | primary | 2026-11-25 |
| Odoo | Import | Bank statement import: OFX, CAMT.053, CSV | [Invoice Data Extraction](https://invoicedataextraction.com/blog/import-bank-statements-into-quickbooks) | secondary | 2026-11-25 |
| Odoo | Offline | Self-hosted; can be offline | [Odoo](https://www.odoo.com/) | primary | 2026-11-25 |

---

## IRS Recordkeeping (Part 2)

### IRS Recordkeeping Requirements
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| IRS | Record retention - 3 years | Returns filed on time with no underreported income; keep all records supporting income, deductions, credits | [IRS: How long should I keep records](https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records) | primary | 2026-11-25 |
| IRS | Record retention - 4 years | Employment taxes; keep all employment tax records | [IRS: How long should I keep records](https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records) | primary | 2026-11-25 |
| IRS | Record retention - 6 years | Income not reported that is more than 25% of gross income shown on return; keep all records for that year | [IRS: How long should I keep records](https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records) | primary | 2026-11-25 |
| IRS | Record retention - 7 years | Claim for loss from worthless securities or bad debt deduction; keep records supporting the claim | [IRS: How long should I keep records](https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records) | primary | 2026-11-25 |
| IRS | Record retention - Until period expires | Property disposed; keep records for old and new property | [IRS: How long should I keep records](https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records) | primary | 2026-11-25 |
| IRS | Record retention - No limit | Fraudulent return or no return filed; keep all records | [IRS: How long should I keep records](https://www.irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records) | primary | 2026-11-25 |
| IRS | Recordkeeping - Schedule C | Small businesses must keep gross receipts, purchases, expenses, assets, employment taxes | [IRS Publication 334](https://www.irs.gov/publications/p334) | primary | 2026-11-25 |
| IRS | Recordkeeping - S-Corp | S-Corps must keep gross receipts, purchases, expenses, assets, payroll, shareholder records, K-1 distributions | [IRS Publication 542](https://www.irs.gov/publications/p542) | primary | 2026-11-25 |
| IRS | Recordkeeping - Household | Households must keep income records (W-2, 1099), expense receipts for deductions/credits, mileage logs | [IRS Publication 17](https://www.irs.gov/publications/p17) | primary | 2026-11-25 |

### IRS Free Filing Options
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| IRS Free File | Eligibility | $95,000 AGI limit (current season opening January 2027); $89,000 AGI for 2026 season (2025 returns); closed until January 2027 | [IRS: E-file Do Your Taxes for Free](https://www.irs.gov/e-file-do-your-taxes-for-free) | primary | 2026-11-25 |
| IRS Free File | Business support | Supports Schedule C but NOT Form 1120-S | [IRS Free File](https://www.irs.gov/filing/free-file-do-your-federal-taxes-for-free) | primary | 2026-11-25 |
| IRS Direct File | Status | Discontinued (not offered for the 2026 filing season); OB3 ended the operation; funds reallocated | [TAS: Objective 25 2025](https://www.taxpayeradvocate.irs.gov/news/directory-entry/objective-25-2025/) | primary | 2026-11-25 |
| IRS Direct File | History | Made permanent in 2025 per p6036.pdf, later discontinued | [IRS p6036](https://www.irs.gov/pub/irs-pdf/p6036.pdf) | primary | 2026-11-25 |
| VITA | Eligibility | Individuals meeting EITC income guidelines; no IRS-wide limit; sites set own limits | [IRS VITA](https://www.irs.gov/individuals/free-tax-return-preparation-for-qualifying-taxpayers) | primary | 2026-11-25 |
| VITA | Business support | Supports basic returns; varies by site | [IRS VITA](https://www.irs.gov/individuals/free-tax-return-preparation-for-qualifying-taxpayers) | primary | 2026-11-25 |
| Military OneSource (MilTax) | Eligibility | Service members, spouses, survivors, recent veterans; no income limit | [Military OneSource MilTax](https://www.militaryonesource.mil/financial-legal/taxes/miltax-military-tax-services/) | primary | 2026-11-25 |
| Military OneSource (MilTax) | Business support | Yes (federal + up to 5 state returns) | [Military OneSource MilTax](https://www.militaryonesource.mil/financial-legal/taxes/miltax-military-tax-services/) | primary | 2026-11-25 |

### 1099 and Information Returns
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| IRS | 1099-NEC filing | Businesses must file Form 1099-NEC for non-employee compensation >= $600 | [IRS IRIS](https://www.irs.gov/businesses/small-businesses-self-employed/information-returns-intake-system-iris) | primary | 2026-11-25 |
| IRS | 1099-MISC filing | Businesses must file Form 1099-MISC for miscellaneous income >= $600 | [IRS IRIS](https://www.irs.gov/businesses/small-businesses-self-employed/information-returns-intake-system-iris) | primary | 2026-11-25 |
| IRS | W-9 collection | Businesses must collect Form W-9 from vendors to obtain TIN for 1099 reporting | [IRS Form W-9](https://www.irs.gov/pub/irs-pdf/fw9.pdf) | primary | 2026-11-25 |
| IRS | Form 1096 | Annual summary of information returns must be filed with IRS | [IRS Form 1096](https://www.irs.gov/pub/irs-pdf/f1096.pdf) | primary | 2026-11-25 |

### Schedule C Inputs (2026)
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| IRS | Schedule C Line 1 | Gross receipts or sales | [IRS Schedule C 2026](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf) | primary | 2026-11-25 |
| IRS | Schedule C Line 2 | Returns and allowances | [IRS Schedule C 2026](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf) | primary | 2026-11-25 |
| IRS | Schedule C Line 4 | Cost of goods sold (from line 42, Part III) | [IRS Schedule C 2026](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf) | primary | 2026-11-25 |
| IRS | Schedule C Line 7 | Gross income (Lines 3 + 6: net receipts + other income) | [IRS Schedule C 2026](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf) | primary | 2026-11-25 |
| IRS | Schedule C Line 28 | Total expenses before home office (Sum of lines 8-27b) | [IRS Schedule C 2026](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf) | primary | 2026-11-25 |
| IRS | Schedule C Line 29 | Tentative profit or (loss) (Line 7 - Line 28) | [IRS Schedule C 2026](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf) | primary | 2026-11-25 |
| IRS | Schedule C Line 31 | Net profit or (loss) (Line 29 - Line 30: home office expenses) | [IRS Schedule C 2026](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf) | primary | 2026-11-25 |
| IRS | Schedule C Lines 13-21 | Business expenses by category (advertising, car, commissions, depreciation, etc.) | [IRS Schedule C Instructions](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf) | primary | 2026-11-25 |
| IRS | Schedule C Line 27a | Net profit or loss (Line 12 - Line 22) | [IRS Schedule C Instructions](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf) | primary | 2026-11-25 |

### Form 1120-S Inputs (2026)
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| IRS | Form 1120-S Line 1a | Gross receipts or sales | [IRS Form 1120-S 2025](https://www.irs.gov/pub/irs-pdf/f1120s.pdf) | primary | 2026-11-25 |
| IRS | Form 1120-S Line 1c | Gross receipts net of returns/allowances (Line 1a - Line 1b) | [IRS Form 1120-S 2025](https://www.irs.gov/pub/irs-pdf/f1120s.pdf) | primary | 2026-11-25 |
| IRS | Form 1120-S Line 2 | Cost of goods sold | [IRS Form 1120-S 2025](https://www.irs.gov/pub/irs-pdf/f1120s.pdf) | primary | 2026-11-25 |
| IRS | Form 1120-S Line 3 | Gross profit (Line 1c - Line 2) | [IRS Form 1120-S 2025](https://www.irs.gov/pub/irs-pdf/f1120s.pdf) | primary | 2026-11-25 |
| IRS | Form 1120-S Line 6 | Total income (Sum of lines 3-5: gross profit + other income) | [IRS Form 1120-S 2025](https://www.irs.gov/pub/irs-pdf/f1120s.pdf) | primary | 2026-11-25 |
| IRS | Form 1120-S Lines 7-20 | Deductions (compensation of officers, salaries, repairs, bad debts, rents, taxes, interest, depreciation, depletion, advertising, pensions, employee benefits, energy-efficient buildings) | [IRS Form 1120-S 2025](https://www.irs.gov/pub/irs-pdf/f1120s.pdf) | primary | 2026-11-25 |
| IRS | Form 1120-S Line 21 | Total deductions (Sum of lines 7 through 20) | [IRS Form 1120-S 2025](https://www.irs.gov/pub/irs-pdf/f1120s.pdf) | primary | 2026-11-25 |
| IRS | Form 1120-S Line 22 | Ordinary business income (loss) (Line 6 - Line 21) | [IRS Form 1120-S 2025](https://www.irs.gov/pub/irs-pdf/f1120s.pdf) | primary | 2026-11-25 |
| IRS | Form 1120-S Schedule K-1 | Shareholder's pro rata share of allocable income, deductions, credits, etc. | [IRS Form 1120-S 2025](https://www.irs.gov/pub/irs-pdf/f1120s.pdf) | primary | 2026-11-25 |

### Form 1040 Schedules (2025)
| Tool | Topic | Finding | Source | Type | Date |
|---|---|---|---|---|---|
| IRS | Schedule A | Itemized deductions (medical, taxes, interest, charity, casualty) | [IRS Form 1040 2025](https://www.irs.gov/pub/irs-pdf/f1040.pdf) | primary | 2026-11-25 |
| IRS | Schedule B | Interest and ordinary dividends | [IRS Form 1040 2025](https://www.irs.gov/pub/irs-pdf/f1040.pdf) | primary | 2026-11-25 |
| IRS | Schedule C | **Profit or Loss from Business (Sole Proprietorship)** | [IRS Form 1040 2025](https://www.irs.gov/pub/irs-pdf/f1040.pdf) | primary | 2026-11-25 |
| IRS | Schedule D | Capital gains/losses | [IRS Form 1040 2025](https://www.irs.gov/pub/irs-pdf/f1040.pdf) | primary | 2026-11-25 |
| IRS | Schedule E | Supplemental income and loss (rental, royalties, partnerships, S-corps, estates, trusts) | [IRS Form 1040 2025](https://www.irs.gov/pub/irs-pdf/f1040.pdf) | primary | 2026-11-25 |

---

## Licenses Summary

| Tool | License | Source |
|---|---|---|
| QuickBooks Online/Desktop | Proprietary | [Intuit](https://quickbooks.intuit.com/) |
| GnuCash | GNU GPL | [GnuCash](https://www.gnucash.org/) |
| Manager.io | Source-available (not open source) | [Manager README](https://raw.githubusercontent.com/Manager-io/Manager/main/README.md) |
| Ledger | GPL | [hledger manual ref](https://hledger.org/manual) |
| hledger | GPL | [hledger](https://hledger.org/) |
| Beancount | BSD | [Beancount](https://beancount.github.io/) |
| Fava | MIT | [Fava](https://fava.pythonanywhere.com/) |
| Firefly III | Open source (various) | [Firefly III](https://www.firefly-iii.org/) |
| Akaunting | Open source | [Akaunting](https://akaunting.com/) |
| ERPNext | MIT | [ERPNext](https://erpnext.com/) |
| Odoo Community | LGPL | [Odoo](https://www.odoo.com/) |

---

*Note: This evidence pack contains findings from public documentation. All claims will be verified and voted in the main report.*
