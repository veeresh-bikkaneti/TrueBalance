# ADR 0003: Clean architecture with ports and adapters

- Status: Accepted
- Date: 2026-11-21

## Context
The owner wants the code modular, reusable, extendable and scalable for future integrations. Today all logic lives in `Program.cs` and the Application project is an empty `Class1.cs`.

## Decision
- Domain: entities and rules only, no I/O, no framework references.
- Application: use cases (`ImportStatement`, `CategorizeTransaction`, `PostEntry`, `Reconcile`, `BuildReport`), depending only on interfaces.
- Ports (interfaces): `ILedgerStore`, `IStatementImporter`, `IDocumentVault`, `IReportExporter`, `INetworkGate`, `IAiAssist`.
- Infrastructure: adapters that implement ports (CSV/OFX/PDF importers, encrypted store, vault, CSV/XLSX/CPA packet exporters).
- Backend endpoints are thin and call use cases.
- UI: React app shell -> feature components (Setup wizard, Transactions, Reconcile, Reports, Settings / Network log).
- Follow SOLID: a new bank format or integration is a new adapter class; the ledger core does not change.

## Consequences
- Architecture tests should fail the build if Domain references Infrastructure or a framework.
- Spec: `docs/specs/prompts/04-ports-network-gate.md`.
