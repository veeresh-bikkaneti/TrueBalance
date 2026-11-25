using Domain.Enums;
using Domain.Ledger;

namespace Domain.Entities;

/// <summary>Append-only record of a ledger action. Holds a local user id only, never a name or email.</summary>
public class LedgerAuditRecord
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; private set; }
    public string LocalUserId { get; private set; } = string.Empty;
    public LedgerAction Action { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public Guid? PeriodId { get; private set; }
    public string Details { get; private set; } = string.Empty;

    private LedgerAuditRecord() { }

    /// <summary>Creates an audit record. Invariants: a local user id is required; details are at most 500 characters.</summary>
    /// <exception cref="LedgerRuleException">Invariant violated.</exception>
    public static LedgerAuditRecord For(LedgerAction action, string localUserId, DateTime occurredAtUtc,
        Guid? journalEntryId = null, Guid? periodId = null, string details = "")
    {
        if (string.IsNullOrWhiteSpace(localUserId))
            throw new LedgerRuleException("An audit record requires a local user id.");
        if (localUserId.Length > 100)
            throw new LedgerRuleException("Local user id cannot exceed 100 characters.");
        details ??= string.Empty;
        if (details.Length > 500)
            throw new LedgerRuleException("Audit details cannot exceed 500 characters.");
        return new LedgerAuditRecord
        {
            Action = action,
            LocalUserId = localUserId,
            OccurredAtUtc = occurredAtUtc,
            JournalEntryId = journalEntryId,
            PeriodId = periodId,
            Details = details
        };
    }
}
