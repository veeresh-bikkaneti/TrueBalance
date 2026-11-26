---
checked:
  docs/adr/0011-local-modular-monolith.md: 4792ef60fdc033c952b3dc6d252ba9138881bb26
  OfflineCPA/src/Domain/Ledger/IDomainEvent.cs: ba907ed5f4659c16c52c0e6250a48cf89af5390b
  OfflineCPA/src/Domain/Ledger/LedgerEvents.cs: ebbc7c41014baa10304b7cf80d2203ddcd43dad6
  OfflineCPA/src/Domain/Entities/JournalEntry.cs: 60a5b3d1e5b424ba8d60bd599e5a6072b3de7e87
  OfflineCPA/src/Domain/Entities/Period.cs: bcc4fe128753b85180c4567b93e875337cb7df6c
  OfflineCPA/src/Application/Ledger/DomainEventDispatcher.cs: 493387e8d82edf09427ea1a44e1f4883283fcb2c
  OfflineCPA/src/Domain/Entities/LedgerAuditRecord.cs: a364845a8971c0d605ea46a778e09261d8f282a4
  OfflineCPA/src/Application/Ledger/CreateDraftEntry.cs: c4828307290c306fcc5540383dea320b11c4f330
  OfflineCPA/src/Application/Ledger/PostEntry.cs: 1a33aa5abaf015344c6996da8ccd00adcbcca53e
  OfflineCPA/src/Application/Ledger/LockPeriod.cs: 5847bbfe4d6d13869463ab78e55d4d8751fff324
  OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs: e9a88657b2bea914a76921239982eba4df689c19
  docs/architecture/modules.md: 296ecfb6566a66c56773e762d5b03c3ae360f5dc
---
# Event log

Domain events stay in memory on the aggregate. There is no broker. ADR 0011 (accepted) says dispatch is in-process, inside one transaction, plus an append-only table (repo: docs/adr/0011-local-modular-monolith.md#L16) (repo: OfflineCPA/src/Domain/Ledger/IDomainEvent.cs#L4).

## Events

`EntryDrafted`, `EntryPosted`, `EntryReversed`, and `PeriodLocked` are the four events. Their `EventType` strings match those names (repo: OfflineCPA/src/Domain/Ledger/LedgerEvents.cs#L3-L17). The aggregate adds the event only after the rule passes: draft at create, posted at post, reversed at reverse, and period locked at lock (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L67) (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L93) (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L130) (repo: OfflineCPA/src/Domain/Entities/Period.cs#L48). A reversing entry is born Posted and does not raise `EntryPosted`. The original raises one `EntryReversed` (repo: OfflineCPA/src/Domain/Entities/JournalEntry.cs#L115-L130).

## Dispatcher and table

`DomainEventDispatcher` appends one `LedgerAuditRecords` row per event. The summaries are fixed: `draft created`, `entry posted`, `entry reversed`, `period locked` (repo: OfflineCPA/src/Application/Ledger/DomainEventDispatcher.cs#L12-L29). `PeriodLocked` stores a null journal-entry id and the period id (repo: OfflineCPA/src/Application/Ledger/DomainEventDispatcher.cs#L29). The row holds a local user id, a time, an action, an event type, one entity id, and that summary (repo: OfflineCPA/src/Domain/Entities/LedgerAuditRecord.cs#L6-L16).

The use case calls dispatch and then `SaveChanges`, so a thrown rule writes neither the entity nor the row (repo: OfflineCPA/src/Application/Ledger/CreateDraftEntry.cs#L32-L34) (repo: OfflineCPA/src/Application/Ledger/DomainEventDispatcher.cs#L8-L10). The same order is in post, reverse, and lock (repo: OfflineCPA/src/Application/Ledger/PostEntry.cs#L29-L30) (repo: OfflineCPA/src/Application/Ledger/LockPeriod.cs#L27-L28).

`TR_LedgerAuditRecords_AppendOnly` raises on every update and delete (repo: OfflineCPA/src/Infrastructure/Migrations/20261125000000_LedgerCore.cs#L193-L205).

## Change it safely

Add the event on the aggregate, a `LedgerEventType` constant, a dispatcher arm with a fixed summary, and a `LedgerTests` fact. Do not put a memo, a name, an account number, or a posting count in `Details` (repo: docs/architecture/modules.md#L31-L42). See [[ledger]] and [[double-entry-invariants]].
