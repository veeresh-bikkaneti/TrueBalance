# OfflineCPA roadmap: not built yet

Claims cut from earlier versions of `../walkthrough.md`, `../task.md` and `../architecture_research.md` during the honesty pass (prompt 00). Nothing here is implemented. The ordered plan that delivers these is `../../docs/specs/prompts/INDEX.md`; decisions live in `../../docs/adr/`.

- OCR for receipts and PDFs (Tesseract or another local engine) — prompt 14.
- PII detection, masking and encryption (middleware, vault, no-leak tests) — prompts 04b and 06.
- Internal Docker network with only the frontend port published — replacing Docker+Postgres entirely per ADR 0006 (encrypted single-file SQLite, prompt 11).
- Double-entry posting, locked periods, reconciliation — prompts 03 and 09.
- Staged import (parse, preview, review, post) for CSV/OFX — prompt 05.
- Large-ledger rendering (virtualized table) and local state caching in the UI — prompt 10.
- CPA packet reports (trial balance, P&L, balance sheet, tax-line summary) — prompt 08.
