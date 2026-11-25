---
trigger: always_on
---

# TrueBalance Agent Rules

## 1. Rule Priority
`P0: /AGENTS.md` > `P1: .agent/rules/TRUEBALANCE.md` > `P2: .agent/agents/*.md` > `P3: .agent/skills/*/SKILL.md`

## 2. Always-On Behavior
- Follow `.agent/skills/i-have-adhd/SKILL.md` until Veeresh says "stop adhd mode" or "normal mode".
- Follow `.agent/skills/ponytail/SKILL.md` (default: `full`) and `.agent/skills/karpathy-guidelines/SKILL.md` on every code change.
- Follow `.agent/skills/llm-wiki/SKILL.md` for all `raw/` + `wiki/` operations (immutable `raw/`, mandatory `(source: raw/...)` citations).

## 3. Token Economy & Seating
- Pick S / M / L tier from `/AGENTS.md` before starting; warn at 80%, stop at 100%.
- Main session plans, integrates, and verifies only (<20% of budget).
- Seating is the "Council of experts and roles" section in `/AGENTS.md`: cheaper models for the orchestrator and seats, a mid-tier model for the architect reviewer, a frontier model for the advisor.

## 4. TrueBalance Invariants
- Zero outbound network calls outside `INetworkGate`.
- Zero raw PII in logs, errors, fixtures, exports, or AI prompts (ADR 0010).
- Stack [Tested]: C# .NET 8 backend (`net8.0` in `OfflineCPA/src/Backend/Backend.csproj`), React + TypeScript frontend (`react` and `typescript` in `OfflineCPA/frontend/package.json`). Python is not in `OfflineCPA/`. `find` shows it under `.agent/scripts/`, `.agent/skills/` and `.agent/.shared/` (3, 11 and 3 files). Command: `grep -n TargetFramework OfflineCPA/src/Backend/Backend.csproj`; `find . -name '*.py'`.
