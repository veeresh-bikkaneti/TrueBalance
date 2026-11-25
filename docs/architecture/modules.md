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
| `Application.Interfaces` | `ILedgerStore` |
| `Infrastructure.Data` | EF store and `OfflineCPADbContext` |
| `Infrastructure.Migrations` | Migrations and the model snapshot |

`TrueBalance.Tests.Ledger` is the test double. It is not a production module.

## Dependency rules

The same test class checks three rules. Library: NetArchTest.Rules 1.3.2, MIT, [license file](https://github.com/BenMorris/NetArchTest/blob/master/LICENSE). [Confidence: High]

ArchUnitNET was the other candidate. Its license is Apache-2.0 ([license file](https://github.com/TNG/ArchUnitNET/blob/main/LICENSE)). [Confidence: High] NetArchTest was picked because the license is MIT and the fluent dependency check is enough for these three rules.

1. Domain does not reference Infrastructure, `Microsoft.EntityFrameworkCore`, or `Microsoft.AspNetCore`.
2. Application does not reference the Infrastructure assembly. Infrastructure already references Application, so a project reference the other way does not compile. The test checks the compiled assembly list.
3. A production type that depends on the ledger types above must live in a namespace in the table.

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
