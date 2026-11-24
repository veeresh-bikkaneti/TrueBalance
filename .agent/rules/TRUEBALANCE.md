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
- Send building, writing, and review seats to **cheaper models** via the Council Panel seating loop (1 evidence pack, parallel clean-context seats, scoped re-votes).

## 4. TrueBalance Invariants
- Zero outbound network calls outside `INetworkGate`.
- Zero raw PII in logs, errors, fixtures, exports, or AI prompts (ADR 0010).
- Stack scope: `OfflineCPA/` (Python, C#, TypeScript/React).
