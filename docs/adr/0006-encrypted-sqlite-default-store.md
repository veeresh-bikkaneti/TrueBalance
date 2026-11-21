# ADR 0006: Encrypted single-file SQLite is the default store

- Status: Accepted (library choice pending owner approval)
- Date: 2026-11-21

## Context
The owner wants setup to be one bootstrap command for non-technical users. Today it needs Docker, Postgres, a terminal and an undocumented migration step.

## Decision
- Default store: one encrypted SQLite file, opened with the user's passphrase. No Docker or Postgres needed.
- Postgres stays as an optional `ILedgerStore` adapter for larger users.
- The encryption library (for example SQLCipher or per-field encryption) is chosen in prompt 11's arena step and approved by the owner before merge.

## Consequences
- One-command bootstrap per OS becomes possible (`docs/specs/prompts/11-one-command-bootstrap.md`).
- Backups are a copy of one encrypted file plus the vault (`13-backups.md`).
- Losing the passphrase means losing the data; the wizard must say so clearly.
