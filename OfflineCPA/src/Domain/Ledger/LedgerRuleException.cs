namespace Domain.Ledger;

/// <summary>Thrown for every ledger invariant violation. Messages contain no personal data.</summary>
public sealed class LedgerRuleException : Exception
{
    public LedgerRuleException(string message) : base(message) { }
}
