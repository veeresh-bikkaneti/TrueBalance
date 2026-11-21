# Verified facts and sources

Facts the plan relies on, verified 2026-11-19. Voting rule: 3+ agreeing primary sources = High; 2 agreeing or 1 primary = Moderate; 1 secondary or conflicting = Low; no verifiable source = the claim is deleted. Recheck anything marked Low before quoting it.

## IRS recordkeeping
- Retention: generally 3 years; 6 years if unreported income exceeds 25%; 7 years for worthless securities or bad debt; employment tax records 4 years; indefinitely if no return was filed ([IRS](https://irs.gov/businesses/small-businesses-self-employed/how-long-should-i-keep-records)) [Confidence: Moderate | Vote: 1/1 primary]
- Any recordkeeping system is acceptable if it clearly shows income and expenses ([IRS](https://www.irs.gov/businesses/small-businesses-self-employed/what-kind-of-records-should-i-keep)) [Confidence: Moderate | Vote: 1/1 primary]
- Businesses may choose single- or double-entry; double-entry uses journals and ledgers ([IRS Pub 583](https://irs.gov/publications/p583)) [Confidence: Moderate | Vote: 1/1 primary]
- Meals are generally 50% deductible; travel records show time, place and business purpose ([IRS Pub 463](https://irs.gov/pub/irs-pdf/p463.pdf)) [Confidence: Moderate | Vote: 1/1 primary]
- Form 1120-S: line 19 is Energy Efficient Commercial Buildings Deduction, line 20 is Other Deductions ([IRS i1120s](https://irs.gov/instructions/i1120s)) [Confidence: Moderate | Vote: 1/1 primary]
- Schedule C 2026 Part II runs from line 8 (Advertising) to 27b (Other expenses), including 24a Travel, 24b Deductible meals, 26 Wages ([IRS Schedule C](https://www.irs.gov/pub/irs-pdf/f1040sc.pdf)) [Confidence: Moderate | Vote: 1/1 primary]
- 1099-NEC/MISC threshold reported as rising from $600 to $2,000 for payments in 2026, inflation-adjusted after 2026 ([CPA Practice Advisor](https://www.cpapracticeadvisor.com/2026/09/01/what-the-new-1099-reporting-rules-mean/189451/), [Barnes Dennig](https://www.barnesdennig.com/obbba-brings-relief-form-1099-reporting/)) [Confidence: Moderate | Vote: 2/2 secondary]

## PII
- SSNs and financial account numbers are PII; minimize collection, use and retention; de-identify where full records are not needed ([NIST SP 800-122](https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-122.pdf)) [Confidence: Moderate | Vote: 1/1 primary]
- IRS Pub 4557 (safeguarding taxpayer data) was not verified; do not cite it until it is.

## Competitors
- QuickBooks Desktop Pro Plus, Premier Plus, Mac Plus and Enhanced Payroll: no new US subscriptions after Sept 30, 2024; Enterprise unaffected ([Intuit](https://quickbooks.intuit.com/learn-support/en-us/help-article/new-subscriptions/us-quickbooks-desktop-sold-july-2024/L5lkQNq7L_US_en_US)) [Confidence: Moderate | Vote: 1/1 primary]
- QuickBooks Online monthly prices: Simple Start $38, Essentials $85, Plus $140, Advanced $340 ([NerdWallet, Oct 2026](https://www.nerdwallet.com/article/small-business/business-software/how-much-does-quickbooks-cost)) [Confidence: Low | Vote: 1/1 secondary]
- GnuCash: double-entry, reconciliation, standard reports, QIF/OFX import, SQL storage (described as experimental), free ([gnucash.org](https://www.gnucash.org/features)) [Confidence: Moderate | Vote: 1/1 primary]
- Manager desktop: free, offline, Windows/macOS/Linux; source-available, not open source ([Manager README](https://raw.githubusercontent.com/Manager-io/Manager/main/README.md)) [Confidence: Moderate | Vote: 1/1 primary]
- hledger: plain-text double-entry journal, balance assertions, CSV import through rules files ([hledger manual](https://hledger.org/manual)) [Confidence: Moderate | Vote: 1/1 primary]

## Verified by running the code (commit 406eec1, 2026-11-19)
- `dotnet build OfflineCPA.sln`: 0 errors, 0 warnings. No backend tests exist.
- No EF migrations; `/api/transactions` and `/api/export` return HTTP 500 on a fresh database.
- `/api/upload` path traversal confirmed: a crafted filename wrote outside the upload folder.
- PDF text is extracted then discarded; CSV is saved but not parsed; no journal entries are created; the XLSX export has only Date, Description and Document Path.
- Frontend hardcodes `http://localhost:5030/api`; backend listens on 5159 (launchSettings) or 8080 (Docker).
- Frontend: build passes; eslint 3 errors; npm audit (prod): axios high, form-data high, follow-redirects moderate.
- No outbound network calls found in app code. Compose publishes Postgres 5432, API 8080 and UI 3000 on all interfaces, hardcodes the DB password and runs in Development mode.
