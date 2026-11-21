# ADR 0009: Statement import is staged: parse, preview, review, post

- Status: Accepted
- Date: 2026-11-21

## Context
Today PDF text is extracted and thrown away, CSV is ignored, and a folder is polled every 30 seconds.

## Decision
- Importers (`IStatementImporter`: CSV, OFX, PDF text, later receipt OCR) produce staged rows, never ledger entries directly.
- The user previews and categorizes rows; duplicates are flagged; only reviewed rows post as balanced entries through the ledger use case.
- Folder watching becomes an optional adapter, not the main flow.

## Consequences
- Raw statement content passes through the importer first, so PII handling at this step matters (ADR 0010).
- Specs: `docs/specs/prompts/05-statement-import.md`, `14-local-receipt-ocr.md`.
