# Modules

ADR 0011 (Accepted) names the modules: Ledger, Import, PII/Masking, Vault, TaxMaps, Reporting, Settings. One process. Domain events stay in memory. No broker.

## Ledger namespaces

Production types that touch `JournalEntry`, `Posting`, `Period`, `LedgerAuditRecord`, or `IDomainEvent` must live in one of these namespaces. `LedgerArchitectureTests` enforces that.

| Namespace | What lives here |
| --- | --- |
| `Domain.Entities` | The ledger types, plus `Account`, which owns postings |
| `Domain.Enums` | `LedgerAction`, `JournalEntryStatus` |
| `Domain.Ledger` | Events, money, rule exception |
| `Application.Ledger` | Use cases and `DomainEventDispatcher` |
| `Application.Interfaces` | `ILedgerStore`, `IStatementImporter`, `IDocumentVault`, `IReportExporter`, `IClock`, `IChartOfAccountsSeeder` |
| `Infrastructure.Data` | EF store and `OfflineCPADbContext` |
| `Infrastructure.Migrations` | Migrations and the model snapshot |

`TrueBalance.Tests.Ledger` is the test double. It is not a production module.

## Networking namespace

Only `Application.Networking` (the `INetworkGate` port family) and `Infrastructure.Networking` (the gate adapter) may reference `System.Net.Http` or `System.Net.Sockets`. `LedgerArchitectureTests` fails on any other type that does. The gate denies every request until the user's settings enable the integration, hands out a `HttpClient` locked to its one approved host, and writes one `LedgerAuditRecords` row per attempt.

| Namespace | What lives here |
| --- | --- |
| `Application.Networking` | `INetworkGate`, `IIntegrationSettings`, `INetworkAttemptLog`, the attempt records |
| `Application.Ledger` | `LedgerNetworkAttemptLog`, the only writer of gate rows |
| `Infrastructure.Networking` | `LocalNetworkGate`, `DisabledIntegrationSettings`, `NetworkingModule` |

Adapters register by convention: one `IAdapterModule` class per adapter folder under `Infrastructure`, found by `AddInfrastructureAdapterModules`. The module classes live in `Infrastructure.Data`, `Infrastructure.Services`, `Infrastructure.Networking`, and `Infrastructure.Reporting`.

## Dependency rules

The same test class checks four rules. Library: NetArchTest.Rules 1.3.2, MIT, [license file](https://github.com/BenMorris/NetArchTest/blob/master/LICENSE). [Confidence: High]

ArchUnitNET was the other candidate. Its license is Apache-2.0 ([license file](https://github.com/TNG/ArchUnitNET/blob/main/LICENSE)). [Confidence: High] NetArchTest was picked because the license is MIT and the fluent dependency check is enough for these four rules.

1. Domain does not reference Infrastructure, `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore`, `System.Net.Http`, or `System.Net.Sockets`.
2. Application does not reference the Infrastructure assembly, `Microsoft.EntityFrameworkCore`, or `Microsoft.AspNetCore`. Infrastructure already references Application, so a project reference the other way does not compile. The test checks the compiled assembly list.
3. A production type that depends on the ledger types above must live in a namespace in the table.
4. No type outside `Application.Networking` and `Infrastructure.Networking` references `System.Net.Http` or `System.Net.Sockets`.

## Event contracts

The event log is the table `LedgerAuditRecords` (name kept; the insert-only trigger already guards it). `EventType` names the event. `Details` is a fixed summary. A row stores a local user id, the time, the event type, one entity id, and that summary. It does not store a memo, a name, an account number, a posting count, or a second id.

The aggregate raises the event only after the rule passes. `DomainEventDispatcher` appends one row. `SaveChanges` commits the entity and the row in the same unit of work. A thrown rule writes neither.

| Event | EventType | Action | Entity id column | Masked summary | Not stored |
| --- | --- | --- | --- | --- | --- |
| `EntryDrafted` | `EntryDrafted` | `DraftCreated` | `JournalEntryId` | `draft created` | memo, line count |
| `EntryPosted` | `EntryPosted` | `Posted` | `JournalEntryId` | `entry posted` | memo, amounts, line count |
| `EntryReversed` | `EntryReversed` | `Reversed` | `JournalEntryId` of the reversal | `entry reversed` | original id, memo |
| `PeriodLocked` | `PeriodLocked` | `PeriodLocked` | `PeriodId` (`JournalEntryId` is null) | `period locked` | period name |

`EntryDrafted` is the create row. ADR 0011 names posted, reversed, and locked. Create still needs one row so a draft is in the same log. A reversing entry is born `Posted` and does not raise `EntryPosted`; the original raises one `EntryReversed`.

The network gate writes two more event types to the same append-only table. They are not aggregate events; the gate writes them on every outbound attempt, allowed or denied.

| Trigger | EventType | Action | Entity id column | Masked summary | Not stored |
| --- | --- | --- | --- | --- | --- |
| Adapter asks a disabled integration for a host | `NetworkGateDenied` | `NetworkGateDenied` | both null | `network denied: <adapter> <host>` | paths, queries, user ids, raw values |
| Adapter asks an enabled integration for a host | `NetworkGateGranted` | `NetworkGateGranted` | both null | `network granted: <adapter> <host>` | paths, queries, user ids, raw values |

`LedgerNetworkAttemptLog` keeps only lowercase letters, digits, dots, dashes, and colons in the two name fields, and caps their length. `IAiAssist` (in `Application.Ai`) is the AI port; no implementation is registered, and any future one receives tokenized text from the PII layer (prompt 06), never raw values.
