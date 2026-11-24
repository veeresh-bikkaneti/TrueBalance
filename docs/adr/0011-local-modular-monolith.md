# ADR 0011: One local app — modular monolith, not microservices

- Status: Accepted
- Date: 2026-11-24
- Decided by: project owner, after an architecture review that raised microservices, event-driven architecture and microfrontends

## Context

An architecture review asked whether TrueBalance should be a distributed web platform: microservices, a message broker, and microfrontends, with Docker as the preferred deployment. The attracted ADRs pull the other way: offline by default with a single network gate (ADR 0004), one process on the user's machine, clean architecture with ports and adapters inside one deployable (ADR 0003), an encrypted single-file store (ADR 0006). The project is built and maintained by a solo owner with agent help, and end users are non-technical. Brokers, service registries and multi-repo frontends add failure modes a laptop cannot absorb and an operations surface one maintainer cannot carry.

## Decision

TrueBalance ships as one installable local app: a single deployable backend and one React frontend.

- The backend is a modular monolith. Module boundaries (Ledger, Import, PII/Masking, Vault, TaxMaps, Reporting, Settings) are enforced by project and namespace rules, not process boundaries. SOLID and Clean Architecture apply inside modules; ADR 0003 stands unchanged.
- Event-driven behavior is in-process: domain events dispatched in memory inside one transaction, plus an append-only audit/event log table in the encrypted store. No broker, no queue process, no network hops. This keeps EDA's benefits (auditability, decoupled projections for reports) without its runtime cost.
- The frontend is one React app composed as a shell with feature components (per ADR 0003 and prompt 10). No microfrontends, no separate frontend registries.
- Docker is the developer and contributor path only (compose on loopback for development). End users install and run with one command and no Docker, per TB-11; the database is the encrypted single-file store, per ADR 0006.
- If a hosted multi-user platform is ever proposed, that is a new decision requiring a superseding ADR. The module and event-contract discipline above are what make such a split possible later without rework.

## Consequences

- One thing to build, test, sign and ship per OS. CI stays one pipeline; no service mesh, no container orchestration, no deploy registry to maintain.
- Module discipline must be enforced by tests (architecture dependency checks in the test project), because nothing structural stops cross-module reaches in a monolith. The ledger stays the only writer of posted entries.
- The event log is user-facing data: it lives in the encrypted store, emits masked events only, and the Settings screen can show it as the import/posting audit trail.
- Microservice or broker proposals in future prompts are out of scope by default; an agent that wants one states the case in a superseding ADR instead of in code.
