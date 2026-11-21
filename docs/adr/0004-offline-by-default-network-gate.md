# ADR 0004: Offline by default through a single network gate

- Status: Accepted
- Date: 2026-11-21

## Context
The owner requires that the app never connects to the internet unless a mechanism is deliberately provided. The current code makes no runtime network calls (verified 2026-11-19), but nothing prevents a future change from adding one.

## Decision
- All outbound traffic must pass through `INetworkGate`. The default implementation denies everything.
- An integration is enabled only by the user, per integration, and every attempt (allowed or denied) is written to a Network log the user can see.
- A test fails the build if any code path other than the gate opens a socket.
- Ports bind to 127.0.0.1 only; the database is never exposed.

## Consequences
- Integrations (bank feeds, tax software export, AI helpers) are adapters behind the gate and ship disabled.
- Package installs and updates happen at build time, never at runtime.
- Specs: `docs/specs/prompts/02-security-hardening.md`, `04-ports-network-gate.md`, `15-integration-ai-adapters.md`.
