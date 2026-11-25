namespace Domain.Ledger;

public static class LedgerEventType
{
    public const string Drafted = "EntryDrafted";
    public const string Posted = "EntryPosted";
    public const string Reversed = "EntryReversed";
    public const string PeriodLocked = "PeriodLocked";
}

public sealed record EntryDrafted(Guid EntryId, Guid PeriodId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record EntryPosted(Guid EntryId, Guid PeriodId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record EntryReversed(Guid ReversalEntryId, Guid OriginalEntryId, Guid PeriodId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record PeriodLocked(Guid PeriodId) : IDomainEvent;
