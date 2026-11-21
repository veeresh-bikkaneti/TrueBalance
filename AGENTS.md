# AGENTS.md: start here

You are taking over TrueBalance end to end. Read this file, then `docs/README.md`, then the ADRs. Everything you need is in this repo; you do not need any outside chat or document.

## What TrueBalance is
A free, open-source, offline bookkeeping app for small businesses and households that can't afford paid software. It must keep balanced books, protect PII by design, install with one command, and produce reports a CPA can use to prepare tax returns. It gives recordkeeping support, not legal or tax advice.

## Where things stand (2026-11-21)
- Code: one commit (`406eec1`), in `OfflineCPA/`. It builds, but a fresh install fails (no migrations) and it is not yet a ledger. Findings: `docs/research/review.md`.
- Plan: 17 PRs in 3 phases, specified in `docs/specs/prompts/` (start with `INDEX.md`). Visual map: open `docs/roadmap/build-map.html` in a browser. Text roadmap: `docs/roadmap/roadmap.md`.
- Backlog with acceptance criteria and Definition of Ready/Done: `docs/backlog/product-backlog.md`.
- ADR 0010 is accepted: prompt 04b (PII masking core) runs before 05. Do not run real statements before prompt 06 merges.
- Next PR to build: prompt 00 (`docs/specs/prompts/00-baseline.md`).

## How to work (owner's rules, non-negotiable)
1. **One spec, one draft PR.** Build prompts in `INDEX.md` order, one per branch named in the spec. Open every PR as a draft. Never merge, approve or enable auto-merge: the owner merges.
2. **Git:** Conventional Commits. No `Co-Authored-By` or other AI trailer in commits, and no "Generated with" line in PR descriptions. No rebase, no force push, no `git merge`.
3. **Token economy:** state the cap and your estimate at the start of each PR. Stop and ask the owner before passing the cap. Report tokens used at the end.
4. **Orchestration:** cheaper models do the heavy lifting as subagents; a stronger reviewer model checks and accepts the work.
5. **pstack per PR:** sharpen the ask, draw the statechart, swarm options, architect, arena-test competing choices, write how-to notes, TDD with a narrow regression to clear the gates, then unslop the prose.
6. **Engineering:** show a working mockup before any UI code (`docs/mockups/`). SOLID, clean code, loose coupling, reuse, React shell to feature components. Follow the ADRs; change one only with a superseding ADR.
7. **Facts:** verify every factual claim, statistic or reference against sources and vote (see `docs/research/verified-facts.md` for the scale). Tag claims inline: `[Confidence: High | Vote: 3/3]` with a source link. No source means delete the claim.
8. **Offline and PII:** no new outbound network path outside `INetworkGate`. No raw PII in logs, exports, errors, fixtures committed with real data, or anything sent to an AI model.
9. **Placeholders:** never pre-fill real vendor or business names in templates or sample data; use generic placeholders.
10. **Reports to the owner:** answer first, then 3 to 5 short bullets, then at most one question.

## Decisions only the owner makes
- Merging each PR.
- Library picks in prompts 06 (encryption), 11 (encrypted store, packaging) and 14 (OCR).
- Any screen that differs from the approved setup-wizard mockup.
- Going past a token cap.
