using Domain.Enums;
using Domain.Ledger;

namespace Domain.Entities;

/// <summary>Journal entry header. Lines are <see cref="Posting"/>s with signed amounts (+ debit, - credit).</summary>
public class JournalEntry
{
    private readonly List<Posting> _postings = new();
    private readonly List<IDomainEvent> _events = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateOnly Date { get; private set; }
    public string Memo { get; private set; } = string.Empty;
    public JournalEntryStatus Status { get; private set; } = JournalEntryStatus.Draft;
    public Guid PeriodId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public Guid? ReversesEntryId { get; private set; }
    public Guid? ReversedByEntryId { get; private set; }
    public IReadOnlyList<Posting> Postings => _postings;
    public IReadOnlyList<IDomainEvent> DomainEvents => _events;

    /// <summary>Copies raised events and clears them so a dispatcher writes each one once.</summary>
    public IReadOnlyList<IDomainEvent> PullEvents()
    {
        var copy = _events.ToArray();
        _events.Clear();
        return copy;
    }

    /// <summary>Posted and Reversed entries both count in balances; the reversal is a separate Posted entry that negates the original.</summary>
    public bool IsInLedger => Status is JournalEntryStatus.Posted or JournalEntryStatus.Reversed;

    /// <summary>Sum of signed posting amounts. Zero means balanced.</summary>
    public decimal Imbalance => _postings.Sum(p => p.Amount);

    private JournalEntry() { }

    /// <summary>
    /// Creates a draft. Invariants: memo is required (max 500); the date lies in the period;
    /// the period is not locked; at least one posting. A draft may be unbalanced.
    /// </summary>
    /// <exception cref="LedgerRuleException">Invariant violated.</exception>
    public static JournalEntry CreateDraft(DateOnly date, string memo, Period period,
        IEnumerable<Posting> postings, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(postings);
        ValidateMemo(memo);
        if (!period.Contains(date))
            throw new LedgerRuleException($"Date {date:yyyy-MM-dd} is outside period '{period.Name}' ({period.StartDate:yyyy-MM-dd} to {period.EndDate:yyyy-MM-dd}).");
        if (period.IsLocked)
            throw new LedgerRuleException($"Period '{period.Name}' is locked; entries cannot be created in it.");
        var list = postings.ToList();
        if (list.Count == 0)
            throw new LedgerRuleException("An entry requires at least one posting.");

        var entry = new JournalEntry
        {
            Date = date,
            Memo = memo.Trim(),
            PeriodId = period.Id,
            CreatedAtUtc = nowUtc
        };
        entry._postings.AddRange(list);
        entry._events.Add(new EntryDrafted(entry.Id, period.Id, nowUtc));
        return entry;
    }

    /// <summary>
    /// Posts a draft. Invariants: status is Draft; at least 2 postings; postings sum to zero;
    /// the supplied period is the entry's period; that period is not locked.
    /// </summary>
    /// <exception cref="LedgerRuleException">Invariant violated.</exception>
    public void Post(Period period, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(period);
        if (Status != JournalEntryStatus.Draft)
            throw new LedgerRuleException($"Only a Draft entry can be posted; this entry is {Status}.");
        if (_postings.Count < 2)
            throw new LedgerRuleException($"An entry needs at least 2 postings to post; this entry has {_postings.Count}.");
        var imbalance = Imbalance;
        if (imbalance != 0m)
            throw new LedgerRuleException($"Entry is unbalanced by {imbalance:0.00}; debits and credits must sum to zero.");
        if (period.Id != PeriodId)
            throw new LedgerRuleException("The supplied period is not the period of this entry.");
        if (period.IsLocked)
            throw new LedgerRuleException($"Period '{period.Name}' is locked; entries cannot be posted into it.");

        Status = JournalEntryStatus.Posted;
        PostedAtUtc = nowUtc;
        _events.Add(new EntryPosted(Id, PeriodId, nowUtc));
    }

    /// <summary>
    /// Reverses a Posted entry by returning a new Posted entry with negated postings, linked both ways.
    /// Invariants: status is Posted; not already reversed; the reversal period is unlocked and contains the date.
    /// The original keeps its postings and becomes Reversed.
    /// </summary>
    /// <exception cref="LedgerRuleException">Invariant violated.</exception>
    public JournalEntry Reverse(Period reversalPeriod, DateOnly reversalDate, string memo, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(reversalPeriod);
        if (Status != JournalEntryStatus.Posted)
            throw new LedgerRuleException($"Only a Posted entry can be reversed; this entry is {Status}.");
        if (ReversedByEntryId is not null)
            throw new LedgerRuleException("This entry has already been reversed.");
        ValidateMemo(memo);
        if (reversalPeriod.IsLocked)
            throw new LedgerRuleException($"Period '{reversalPeriod.Name}' is locked; a reversal cannot be posted into it.");
        if (!reversalPeriod.Contains(reversalDate))
            throw new LedgerRuleException($"Reversal date {reversalDate:yyyy-MM-dd} is outside period '{reversalPeriod.Name}'.");

        var reversal = new JournalEntry
        {
            Date = reversalDate,
            Memo = memo.Trim(),
            PeriodId = reversalPeriod.Id,
            CreatedAtUtc = nowUtc,
            Status = JournalEntryStatus.Posted,
            PostedAtUtc = nowUtc,
            ReversesEntryId = Id
        };
        foreach (var p in _postings)
            reversal._postings.Add(Posting.Create(p.AccountId, -p.Amount, p.Memo));

        Status = JournalEntryStatus.Reversed;
        ReversedByEntryId = reversal.Id;
        _events.Add(new EntryReversed(reversal.Id, Id, reversalPeriod.Id, nowUtc));
        return reversal;
    }

    private static void ValidateMemo(string memo)
    {
        if (string.IsNullOrWhiteSpace(memo))
            throw new LedgerRuleException("An entry memo is required.");
        if (memo.Trim().Length > 500)
            throw new LedgerRuleException("An entry memo cannot exceed 500 characters.");
    }
}
