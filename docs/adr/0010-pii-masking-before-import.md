# ADR 0010: Run a PII masking core before statement import

- Status: Proposed (waiting on the project owner)
- Date: 2026-11-21

## Context
In the current plan, statement import (prompt 05) ships before the PII layer (prompt 06). Between those two PRs, real account numbers and tax IDs could be stored in the database and logs, which breaks ADR 0005.

## Options
- **A (recommended):** add prompt 04b, a small masking core (detect account, routing and tax ID numbers; mask them; keep them out of logs; one no-leak test) between 04 and 05. Prompt 06 then adds encryption and the vault. Cost: P0 gains about 60k tokens (estimate), 06 shrinks by about 30k, net about +30k.
- **B:** keep the current order. Cheaper now, but raw PII can be stored between 05 and 06.

## Decision
Pending. Do not change prompt files 05 or 06 until the owner picks. If A is picked: add `docs/specs/prompts/04b-pii-masking-core.md`, move the masking scope out of 06, update `INDEX.md`, the roadmap and the build map, and mark this ADR Accepted.

## Consequences
See options.
