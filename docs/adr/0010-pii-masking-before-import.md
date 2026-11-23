# ADR 0010: Run a PII masking core before statement import

- Status: Accepted
- Date: 2026-11-21
- Decided by: project owner, after a 3-seat advisor review (privacy, architecture, CPA), 3 of 3 for Option A

## Context
In the old plan, statement import (prompt 05) shipped before the PII layer (prompt 06). Between those PRs, real account numbers and tax IDs could be stored in the database and logs, which breaks ADR 0005. The project is open source, so contributors may run early builds on real statements.

## Decision
Option A. Add prompt 04b, a PII masking core, between 04 and 05. Prompt 06 extends it with encryption, the vault, the remaining detectors and the full no-leak suite.

## Conditions
Privacy:
- Mask before any database or log write on the import path.
- 05 cannot merge until the 04b no-leak test passes. The test covers database text columns, logs and error bodies, with synthetic fixtures only.
- Document detector coverage and known gaps. Contributors must not run real statements before 06 merges.

Architecture:
- Masking is a domain policy behind a narrow port (`IPiiMasker`); the importer only calls it.
- Detectors are pluggable; 06 extends the same detectors and fixtures instead of forking them.
- 04b is masking only: no encryption or vault. Re-measure the 05 and 06 estimates after 04b.

CPA (bookkeeping must not lose information):
- Masking is non-destructive: keep the last 4 digits as the display and matching key, plus a keyed fingerprint (HMAC) so matching still works.
- The original statement is retained and stored encrypted in the vault (06).
- Vendor TINs needed for 1099s are captured encrypted in 06 or 07, never masked away.
- Before 09 (reconciliation), test that matching on last 4 plus reference works.

## Consequences
- One more PR on the critical path before import: P0 grows by about 60k tokens and 06 shrinks by about 30k (net about +30k, planning estimates).
- Plan totals: 17 prompts, caps 1,900k, estimates 1,250k.
- Supporting fact: NIST SP 800-122 lists SSNs and financial account numbers as PII and advises minimizing collection and retention [Moderate | 3/3] (https://csrc.nist.gov/pubs/sp/800/122/final).
