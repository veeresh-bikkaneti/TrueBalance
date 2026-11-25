using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Ledger;

namespace Application.Ledger;

/// <summary>
/// Writes one audit row per event. Called in the same use case as SaveChanges,
/// so a rule failure before Dispatch leaves no row.
/// </summary>
public sealed class DomainEventDispatcher
{
    private readonly ILedgerStore _store;

    public DomainEventDispatcher(ILedgerStore store) => _store = store;

    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> events, string localUserId, DateTime occurredAtUtc, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
            await _store.AppendAuditAsync(Map(domainEvent, localUserId, occurredAtUtc), ct);
    }

    private static LedgerAuditRecord Map(IDomainEvent domainEvent, string localUserId, DateTime occurredAtUtc) => domainEvent switch
    {
        EntryDrafted e => Row(LedgerAction.DraftCreated, LedgerEventType.Drafted, localUserId, e.OccurredAtUtc, e.EntryId, e.PeriodId, "draft created"),
        EntryPosted e => Row(LedgerAction.Posted, LedgerEventType.Posted, localUserId, e.OccurredAtUtc, e.EntryId, e.PeriodId, "entry posted"),
        EntryReversed e => Row(LedgerAction.Reversed, LedgerEventType.Reversed, localUserId, e.OccurredAtUtc, e.ReversalEntryId, e.PeriodId, "entry reversed"),
        PeriodLocked e => Row(LedgerAction.PeriodLocked, LedgerEventType.PeriodLocked, localUserId, occurredAtUtc, null, e.PeriodId, "period locked"),
        _ => throw new LedgerRuleException($"Unknown ledger event {domainEvent.GetType().Name}.")
    };

    private static LedgerAuditRecord Row(LedgerAction action, string eventType, string localUserId, DateTime when, Guid? entryId, Guid? periodId, string summary)
        => LedgerAuditRecord.For(action, eventType, localUserId, when, entryId, periodId, summary);
}
