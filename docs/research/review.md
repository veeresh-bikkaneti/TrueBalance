# TrueBalance review

Nov 19, 2026 · Review of github.com/veeresh-bikkaneti/TrueBalance at commit 406eec1 (the only commit). Review and plan only; nothing was pushed.

> Not legal or tax advice. The IRS items below are recordkeeping alignment, so a CPA has what they need.

## Verdict

TrueBalance is a promising skeleton, not a bookkeeping app yet. Your hunch is right: today it stores uploaded files and exports a list of file names. It has no ledger activity, no amounts and no reports a CPA can use. The good parts are a clean project split (.NET 8 Domain / Application / Infrastructure / API), no internet calls, and a domain model (Account, Transaction, JournalEntry) that points the right way.

**Top 3 P0 fixes**

1. Make it run: add EF migrations and seed a chart of accounts. A fresh install fails today.
2. Close the file-write hole and lock the containers to this machine.
3. Build the real ledger: a posting service that only saves balanced, immutable entries, with reports and exports generated from it.

## 1. Current state (verified by running it)

I tested it in a clean Linux sandbox with .NET SDK 8.0 and PostgreSQL 16.

| Check | Command | Result |
| --- | --- | --- |
| Backend build | `dotnet build OfflineCPA.sln` | Pass: 0 errors, 0 warnings |
| Backend tests | none exist | 0 tests |
| API on a fresh DB | `GET /api/transactions`, `GET /api/export` | **Fail: HTTP 500** "relation Transactions does not exist" |
| Upload | `POST /api/upload` (CSV) | 200, but the file is only saved; nothing is parsed |
| Frontend build | `npm ci && npm run build` | Pass (211 KB JS) |
| Frontend lint | `npm run lint` | **Fail: 3 errors** |
| Dependency audit | `npm audit --omit=dev` | axios (high), form-data (high), follow-redirects (moderate) |
| UI tests | Cypress, 2 smoke tests | Not run here (binary download blocked); they only check that labels exist |

**Why the bootstrap fails:** the repo has no `Migrations` folder, so `db.Database.Migrate()` at startup creates nothing. Every data endpoint returns 500 on first run.

**Frontend can't reach the backend:** `Dashboard.tsx` hardcodes `http://localhost:5030/api`, but the API listens on 5159 locally (launchSettings) or 8080 in Docker. The UI shows "Failed to load transactions" in both modes.

**Architecture today**

- All endpoints, file handling and Excel export live in `Program.cs`. The Application layer is an empty `Class1.cs` plus one interface, so there is no use-case layer.
- `LocalFileProcessorService` polls a folder every 30 seconds, extracts PDF text with PdfPig, then throws the text away and saves a Transaction with only a date, description and file path.
- Tax mapping is a single enum (`TaxLineItem`) tied to Form 1120-S, so households and sole proprietors have no fit.
- Docs overclaim: `walkthrough.md` says "production-ready", and `architecture_research.md` describes Tesseract OCR, PII masking middleware and an internal Docker network. None of these exist in the code.

**Ease of use for a non-technical person:** low. It needs Docker, a terminal, knowledge of ports, and a manual migration step that isn't documented. There is no setup wizard, no category choice (household, sole proprietor, S-corp) and no way to enter or categorize a transaction.

## 2. Offline and security

**Network:** no runtime internet calls. I found no HttpClient, telemetry, CDN, font or update-check code in the backend or frontend. axios is only used to call the local API. This matches your offline goal.

**Security findings, worst first**

1. **Arbitrary file write (path traversal), confirmed.** `/api/upload` builds the save path from the raw `file.FileName`. A filename of `../../<any path>/x.csv` wrote a file outside the upload folder in my test. Fix: never use the client filename. Save under a generated ID, keep the original name only as data, and validate type and size.
2. **No authentication on any endpoint.** Anyone who can reach the port can read every transaction and upload files.
3. **Ports open to the network.** docker-compose publishes Postgres 5432, the API 8080 and the UI 3000 on all interfaces, so other devices on the same Wi-Fi can reach them. Bind to 127.0.0.1 and keep Postgres internal.
4. **Hardcoded secrets.** The DB password is in docker-compose.yml and in a fallback in `Program.cs`.
5. **Stack traces leak.** Compose runs in Development mode, so errors return full stack traces and SQL details.
6. **Plain storage.** Uploads sit unencrypted in a Docker volume and in `data/input/processed`. Nothing is encrypted at rest, and there are no backups.
7. **Vulnerable packages:** see the audit row above.

## 3. Accounting correctness

| Area | Today | Needed |
| --- | --- | --- |
| Entry system | JournalEntry entity exists (signed amount), but nothing ever creates one | Double-entry posting service |
| Balance check | None | Each transaction's lines must sum to 0 before saving |
| Chart of accounts | Account table, but no seed data | Starter charts per category (household, Schedule C, 1120-S) |
| Money type | `decimal(18,2)` | Good. Keep decimal, never float; round only at display and posting |
| Reconciliation | None | Match the ledger to a bank statement ending balance |
| Audit trail | Rows can be edited or deleted (cascade delete on entries) | Posted entries are immutable; fix with reversing entries; log who did what and when |
| Periods | None | Fiscal years, period close and lock |
| Imports | PDF text discarded, CSV ignored | Staged import: parse, preview, categorize, then post |

**Tax line errors:** the enum labels Travel as 20 and Meals as 21, "sub-items of 19". The current Form 1120-S instructions show line 19 as Energy Efficient Commercial Buildings Deduction and line 20 as Other Deductions ([IRS i1120s](https://irs.gov/instructions/i1120s)) \[Confidence: Moderate | Vote: 1/1 primary\]. Travel and meals belong under Other Deductions.

**Your "data dump" question:** confirmed. The design today is upload a file, save a row, dump rows to Excel. The right model is a ledger as the source of truth, with every report and export generated from it (section 7).

## 4. Competitor gap table

Focused on what a tiny business or household needs.

| Feature | TrueBalance today | QuickBooks Online (paid) | GnuCash | Manager (desktop) | hledger |
| --- | --- | --- | --- | --- | --- |
| Cost | Free | Subscription | Free | Free | Free |
| Works offline | Yes | No (cloud) | Yes | Yes | Yes |
| Double-entry ledger | Model only | Yes | Yes | Yes | Yes |
| Bank statement import | No | Bank feeds | QIF/OFX | Yes | CSV via rules |
| Reconciliation | No | Yes | Yes | Yes | Balance assertions |
| P&L / balance sheet | No | Yes | Yes | Yes | Yes |
| Guided setup for non-experts | No | Yes | Partial | Partial | No (text files) |
| Tax form mapping | 1120-S only, partly wrong | Yes | Manual | Manual | Manual |
| PII protection by design | No | Vendor-managed | No | No | No |

Sources and votes:

- **QuickBooks Desktop:** Intuit stopped selling new US subscriptions to Desktop Pro Plus, Premier Plus, Mac Plus and Enhanced Payroll after September 30, 2024 ([Intuit help](https://quickbooks.intuit.com/learn-support/en-us/help-article/new-subscriptions/us-quickbooks-desktop-sold-july-2024/L5lkQNq7L_US_en_US)) \[Confidence: Moderate | Vote: 1/1 primary\]. So new US small businesses are pushed toward the cloud product.
- **QuickBooks Online prices:** one secondary source lists Simple Start $38, Essentials $85, Plus $140 and Advanced $340 per month ([NerdWallet, Oct 2026](https://www.nerdwallet.com/article/small-business/business-software/how-much-does-quickbooks-cost)) \[Confidence: Low | Vote: 1/1 secondary\]. Preliminary, so check Intuit's page before quoting it.
- **GnuCash:** its feature page lists double-entry, account reconciliation, standard reports (P&L, balance sheet), QIF/OFX import, and SQLite/MySQL/PostgreSQL storage, which it calls experimental ([gnucash.org](https://www.gnucash.org/features)) \[Confidence: Moderate | Vote: 1/1 primary\].
- **Manager:** the vendor says the desktop edition is free, runs offline on Windows, macOS and Linux, and is source-available, not open source ([Manager README](https://raw.githubusercontent.com/Manager-io/Manager/main/README.md)) \[Confidence: Moderate | Vote: 1/1 primary\].
- **hledger:** the manual describes a plain-text double-entry journal, balance assertions and CSV import through rules files ([hledger manual](https://hledger.org/manual)) \[Confidence: Moderate | Vote: 1/1 primary\].

**Where TrueBalance can win:** offline, free, open source, guided setup for non-experts, PII protection by design, and CPA-ready exports with tax-line mapping. No tool in the table combines all six.

## 5. IRS recordkeeping fit (for a CPA)

| IRS expectation | Source | TrueBalance today | Plan |
| --- | --- | --- | --- |
| Any system is acceptable if it clearly shows income and expenses | [IRS records page](https://www.irs.gov/businesses/small-businesses-self-employed/what-kind-of-records-should-i-keep) \[Confidence: Moderate | Vote: 1/1 primary\] | Doesn't show amounts |
| Choose single- or double-entry; double-entry uses journals and ledgers | [Pub 583](https://irs.gov/publications/p583) \[Confidence: Moderate | Vote: 1/1 primary\] | No entries |
| Keep records that support each income and deduction item until the limitations period ends | [Pub 583](https://irs.gov/publications/p583) \[Confidence: Moderate | Vote: 1/1 primary\] | Receipt paths only |
| Retention: 3 years generally; 6 if unreported income is over 25%; 7 for bad debt or worthless securities; employment tax 4 years; indefinitely if no return | [IRS retention page](https://irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records) \[Confidence: Moderate | Vote: 1/1 primary\] | None |
| Meals generally 50% deductible; travel records show time, place and business purpose | [Pub 463](https://irs.gov/pub/irs-pdf/p463.pdf) \[Confidence: Moderate | Vote: 1/1 primary\] | Mislabeled enum |
| 1099-NEC/MISC threshold rose from $600 to $2,000 for 2026 payments, inflation-adjusted after 2026 | [CPA Practice Advisor](https://www.cpapracticeadvisor.com/2026/09/01/what-the-new-1099-reporting-rules-mean/189451/), [Barnes Dennig](https://www.barnesdennig.com/obbba-brings-relief-form-1099-reporting/) \[Confidence: Moderate | Vote: 2/2 secondary\] | None |

**Schedule C mapping:** the 2026 form's Part II runs from line 8 (Advertising) to 27b (Other expenses), including 9 Car and truck, 11 Contract labor, 13 Depreciation, 18 Office expense, 24a Travel, 24b Deductible meals and 26 Wages ([Schedule C](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf)) \[Confidence: Moderate | Vote: 1/1 primary\]. Keep form line maps as versioned data files (one per form and tax year), not enums, so a new year is a data change.

**What a CPA needs exported:** trial balance, P&L, balance sheet, general ledger detail, a tax-line summary (Schedule C or 1120-S), a 1099 vendor summary and a receipts index. Formats: CSV and XLSX, plus one zipped "CPA packet" per year.

## 6. PII and trust

**What it takes in and keeps today**

| Where | What | Protection |
| --- | --- | --- |
| `/app/user-assets`, `data/input/processed` | Original statements, W-2s and W-9s as uploaded | None, plaintext, kept forever |
| Database | File names and paths, descriptions | None |
| Logs | Full file paths (`Processing file: …`) | None |
| Error pages | Stack traces with SQL | None (Development mode) |
| Excel export | File paths | None |

NIST counts SSNs and financial account numbers as PII and advises minimizing collection, use and retention, and de-identifying where full records aren't needed ([NIST SP 800-122](https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-122.pdf)) \[Confidence: Moderate | Vote: 1/1 primary\]. IRS Pub 4557 wasn't verified in this pass, so I don't cite it.

**PII layer design**

1. **Detect and classify** at import: SSN, EIN/TIN, bank account and routing numbers, card numbers, DOB and street addresses. Use local pattern rules with checksums where they exist (card numbers: Luhn; routing numbers: ABA check digit).
2. **Keep only what's needed.** Bookkeeping needs payee, date, amount and category, not the SSN. Store sensitive values only when a feature needs them (a vendor's TIN for 1099s), and encrypt those fields.
3. **Mask everywhere by default:** UI shows `***-**-1234`, logs never hold raw values (a log filter plus a test), and exports are masked unless the user ticks "include full TIN for my CPA".
4. **Encrypt at rest.** Use an encrypted SQLite file (SQLCipher) or per-field encryption, with a key derived from a user passphrase. Original documents go into an encrypted vault, not loose files.
5. **Secure handling of temp files:** parse in memory where possible; delete staging files after import; an explicit "purge originals" action that respects retention periods.
6. **AI boundary (hard rule):** no AI model, local or remote, receives raw PII. Any future AI feature (auto-categorize, receipt reading) is an opt-in adapter that only gets tokenized text, such as `payee: <VENDOR_7>, amount: 42.10`. It ships disabled and is listed in the network/AI log.
7. **Tests that prove it:** seed fixtures with fake SSNs and account numbers, run import → report → export, then assert none appear in logs, exports, error responses or outbound traffic (the test fails on any socket open).

**Draft README trust statement**

> TrueBalance keeps your books on your computer. Your data is stored in one encrypted file that only your passphrase opens. TrueBalance makes no internet connections unless you turn on an integration yourself, and every connection is shown in the Network log. Tax ID and account numbers are masked on screen, never written to logs, and left out of exports unless you choose to include them for your CPA. No AI model, on your machine or online, ever sees your raw personal data.

## 7. Target architecture

**Principle:** the ledger is the single source of truth. Imports feed it, and reports and exports are read-only views over it. Integrations plug in through adapters and never touch the core.

```
 UI shell (React)
  ├─ Setup wizard   ├─ Transactions   ├─ Reconcile   ├─ Reports   └─ Settings / Network log
        │ (local API on 127.0.0.1, session token)
 Application (use cases)
  ├─ ImportStatement   ├─ CategorizeTransaction   ├─ PostEntry   ├─ Reconcile   └─ BuildReport
        │ depends only on interfaces ↓
 Domain (no I/O)
  ├─ Ledger: Account, JournalEntry, Posting (sum = 0), Period, Reversal
  ├─ Tax maps (data): household, Schedule C 2026, 1120-S 2026
  └─ PII policy: classify, mask, tokenize
        │
 Ports (interfaces)                      Adapters (Infrastructure)
  ├─ ILedgerStore         ─────────────→  EncryptedSqliteStore (default), PostgresStore (optional)
  ├─ IStatementImporter   ─────────────→  CsvImporter, OfxImporter, PdfTextImporter
  ├─ IDocumentVault       ─────────────→  EncryptedFileVault
  ├─ IReportExporter      ─────────────→  CsvExporter, XlsxExporter, CpaPacketExporter
  ├─ IAiAssist (off)      ─────────────→  (none by default; gets tokenized text only)
  └─ INetworkGate (off)   ─────────────→  every outbound call must pass here; logged; default deny
```

**Why this fits your goals**

- **Modular and extendable:** a new bank format or integration is one new adapter class (open/closed principle). The core ledger never changes.
- **Loose coupling:** Domain has no dependencies; Application depends on interfaces only (dependency inversion).
- **Offline by default:** there is one `INetworkGate`, and its default implementation denies everything. An integration has to be switched on by the user and is logged. A test fails the build if any other code opens a socket.
- **One-command setup:** ship a single local app with an embedded encrypted SQLite file as the default store, so no Docker or Postgres is needed. Keep Postgres as an optional adapter for bigger users.
- **Scalable enough:** SQLite handles a small business's volume comfortably; the storage port lets heavier users switch to Postgres without touching the ledger code.

## 8. Ranked improvement plan

Effort: S = under a day, M = 2–4 days, L = about a week, for one developer with an AI agent.

**P0: make it correct and safe**

| # | Fix | Effort |
| --- | --- | --- |
| 1 | Add EF migrations and seed starter charts of accounts; fix the frontend API URL (read from config) | S |
| 2 | Fix path traversal: generated file names, type and size checks; bind all ports to 127.0.0.1; move secrets to a local env file; production mode by default | S |
| 3 | Posting service: balanced, immutable entries with reversals; period lock; unit tests for the balance rule | M |
| 4 | CSV import into a staging table → review → post (OFX next) | M |
| 5 | Update packages (axios etc.), fix lint, add backend test project with a CI gate | S |
| 6 | Correct the docs: remove claims the code doesn't meet | S |

**P1: what a CPA and a non-expert need**

| # | Fix | Effort |
| --- | --- | --- |
| 7 | Reports as views: trial balance, P&L, balance sheet, general ledger | M |
| 8 | Tax-line maps as versioned data (household, Schedule C, 1120-S) and the yearly CPA packet export | M |
| 9 | PII layer: detect, mask, encrypt fields, log filter, no-leak tests | L |
| 10 | Setup wizard (category → books → import → review), see the mockup | M |
| 11 | Bank reconciliation | M |
| 12 | 1099 vendor tracking, meals 50% flag, mileage log | M |

**P2: polish and growth**

| # | Fix | Effort |
| --- | --- | --- |
| 13 | Single-app packaging with encrypted SQLite (no Docker); one bootstrap command per OS | L |
| 14 | Encrypted backups and restore | M |
| 15 | Local receipt OCR behind `IStatementImporter` | L |
| 16 | Integration adapters behind `INetworkGate`, plus an opt-in AI adapter with tokenized input only | L |

## 9. Migration path from the current code

1. **Keep:** Domain entities (rename JournalEntry lines to Posting if wanted), the solution layout, the `ILocalFileParser` idea (becomes `IStatementImporter`), ClosedXML for XLSX.
2. **Move:** endpoint logic out of `Program.cs` into Application use cases; delete `Class1.cs`.
3. **Replace:** the `TaxLineItem` enum with data-driven tax maps; the folder-polling service with an explicit import flow (keep folder watch as an optional adapter).
4. **Add:** migrations, the storage port with an encrypted SQLite adapter, the network gate, the PII policy.
5. **Ship in small PRs** in the P0 order above, each with tests, so main always runs.

## Next decision

Approve a fix prompt for your local agent covering P0 items 1–6, built under your usual rules (mockup first, cheaper seats with review, token cap and estimate stated up front).
