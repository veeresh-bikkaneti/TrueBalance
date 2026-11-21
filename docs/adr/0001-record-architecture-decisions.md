# ADR 0001: Record architecture decisions

- Status: Accepted
- Date: 2026-11-21

## Context
TrueBalance is open source and will be built in small PRs by different people and AI agents. Without written decisions, each new contributor re-litigates the design.

## Decision
Keep one Markdown ADR per significant decision in `docs/adr/`, numbered, using `0000-template.md`. A decision changes only through a new ADR that supersedes the old one.

## Consequences
Agents read `docs/adr/` before coding. A PR that contradicts an Accepted ADR must add a superseding ADR or be rejected in review.
