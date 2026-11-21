# ADR 0005: PII is minimized, masked and encrypted; AI never sees raw PII

- Status: Accepted
- Date: 2026-11-21

## Context
Users will upload bank statements, W-2s and W-9s. The owner's rule: everything is built on trust, and no AI model may sniff anyone's PII. NIST counts SSNs and financial account numbers as PII and advises minimizing collection, use and retention, and de-identifying where full records are not needed ([NIST SP 800-122](https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-122.pdf)) [Confidence: Moderate | Vote: 1/1 primary]. Today uploads, file paths and log lines are all stored in plain text.

## Decision
1. Detect at import: SSN, EIN/TIN, bank account and routing numbers (ABA check digit), card numbers (Luhn), dates of birth, street addresses. Local rules only.
2. Keep only what bookkeeping needs (payee, date, amount, category). Store a sensitive value only when a feature needs it (for example a vendor TIN for 1099s), in an encrypted field.
3. Mask everywhere by default: UI shows `***-**-1234`; logs never hold raw values (log filter plus test); exports are masked unless the user ticks an explicit "include full TIN for my CPA".
4. Encrypt at rest with a key derived from a user passphrase; original documents go into an encrypted vault, not loose files.
5. Parse in memory where possible, delete staging files after import, offer a "purge originals" action that respects IRS retention periods.
6. AI boundary (hard rule): no AI model, local or remote, receives raw PII. Any AI feature is an opt-in `IAiAssist` adapter that only receives tokenized text such as `payee: <VENDOR_7>, amount: 42.10`, ships disabled, and is listed in the Network log.
7. No-leak tests: fixtures with fake SSNs and account numbers run through import -> report -> export, then assert none appear in logs, exports, error responses or outbound traffic.

## Consequences
- Every new feature that touches documents must pass the no-leak suite.
- When masking starts relative to statement import is still open; see ADR 0010.
- Spec: `docs/specs/prompts/06-pii-layer.md`.
