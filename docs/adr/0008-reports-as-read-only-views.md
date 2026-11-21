# ADR 0008: Reports and exports are read-only views over the ledger

- Status: Accepted
- Date: 2026-11-21

## Context
The owner called the current export "a huge data dump from which we generate a CSV". A CPA needs trial balance, P&L, balance sheet, general ledger detail, a tax-line summary, a 1099 vendor summary and a receipts index.

## Decision
- Every report is computed from posted ledger entries at request time; reports never store their own copy of the numbers.
- Exports (`IReportExporter`): CSV, XLSX, and one zipped "CPA packet" per tax year.
- Exports are masked by default (ADR 0005).

## Consequences
- A report is always consistent with the ledger; fixing an entry fixes every report.
- Spec: `docs/specs/prompts/08-reports-cpa-packet.md`.
