using Domain.Ledger;

namespace Domain.Entities;

/// <summary>An accounting period. Once locked, nothing may be posted into it.</summary>
public class Period
{
    private readonly List<IDomainEvent> _events = new();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsLocked { get; private set; }
    public IReadOnlyList<IDomainEvent> DomainEvents => _events;

    /// <summary>Copies raised events and clears them so a dispatcher writes each one once.</summary>
    public IReadOnlyList<IDomainEvent> PullEvents()
    {
        var copy = _events.ToArray();
        _events.Clear();
        return copy;
    }

    private Period() { }

    /// <summary>Creates an open period. Invariants: name is required and EndDate is not before StartDate.</summary>
    /// <exception cref="LedgerRuleException">Invariant violated.</exception>
    public static Period Create(string name, DateOnly startDate, DateOnly endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new LedgerRuleException("A period requires a name.");
        if (endDate < startDate)
            throw new LedgerRuleException($"Period end date {endDate:yyyy-MM-dd} is before start date {startDate:yyyy-MM-dd}.");
        return new Period { Name = name.Trim(), StartDate = startDate, EndDate = endDate };
    }

    /// <summary>True when the date falls within the period, bounds inclusive.</summary>
    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    /// <summary>Locks the period. Invariant: a period can be locked only once; there is no unlock.</summary>
    /// <exception cref="LedgerRuleException">The period is already locked.</exception>
    public void Lock()
    {
        if (IsLocked)
            throw new LedgerRuleException($"Period '{Name}' is already locked.");
        IsLocked = true;
        _events.Add(new PeriodLocked(Id));
    }
}
