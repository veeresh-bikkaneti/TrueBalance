using Domain.Enums;
using Domain.Ledger;

namespace Domain.Entities;

/// <summary>Append-only event-log row. Holds a local user id and a fixed summary, never a name, memo, or account number.</summary>
public class LedgerAuditRecord
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; private set; }
    public string LocalUserId { get; private set; } = string.Empty;
    public LedgerAction Action { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid? JournalEntryId { get; private set; }
    public Guid? PeriodId { get; private set; }
    public string Details { get; private set; } = string.Empty;

    private LedgerAuditRecord() { }

    /// <summary>Creates an event-log row. Invariants: local user id, event type, and a summary of at most 500 characters.</summary>
    /// <exception cref="LedgerRuleException">Invariant violated.</exception>
    public static LedgerAuditRecord For(LedgerAction action, string eventType, string localUserId, DateTime occurredAtUtc,
        Guid? journalEntryId = null, Guid? periodId = null, string details = "")
    {
        if (string.IsNullOrWhiteSpace(localUserId))
            throw new LedgerRuleException("An audit record requires a local user id.");
        if (localUserId.Length > 100)
            throw new LedgerRuleException("Local user id cannot exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(eventType))
            throw new LedgerRuleException("An audit record requires an event type.");
        if (eventType.Length > 40)
            throw new LedgerRuleException("Event type cannot exceed 40 characters.");
        details ??= string.Empty;
        if (details.Length > 500)
            throw new LedgerRuleException("Audit details cannot exceed 500 characters.");
        return new LedgerAuditRecord
        {
            Action = action,
            EventType = eventType,
            LocalUserId = localUserId,
            OccurredAtUtc = occurredAtUtc,
            JournalEntryId = journalEntryId,
            PeriodId = periodId,
            Details = details
        };
    }
}
