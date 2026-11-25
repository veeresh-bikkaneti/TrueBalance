using Domain.Ledger;

namespace Domain.Entities;

/// <summary>One line of a journal entry. Amount is signed: positive = debit, negative = credit.</summary>
public class Posting
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid JournalEntryId { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Memo { get; private set; }
    public Account? Account { get; private set; }

    private Posting() { }

    /// <summary>
    /// Creates a posting. Invariants: an account is required, the amount is not zero
    /// (a zero line carries no information), and the amount has at most 2 decimal places.
    /// </summary>
    /// <exception cref="LedgerRuleException">Any invariant above is violated.</exception>
    public static Posting Create(Guid accountId, decimal amount, string? memo = null)
    {
        if (accountId == Guid.Empty)
            throw new LedgerRuleException("A posting requires an account id.");
        Money.Validated(amount, nameof(amount));
        if (amount == 0m)
            throw new LedgerRuleException("A posting amount cannot be zero.");
        if (memo is { Length: > 500 })
            throw new LedgerRuleException("A posting memo cannot exceed 500 characters.");
        return new Posting { AccountId = accountId, Amount = amount, Memo = memo };
    }
}
