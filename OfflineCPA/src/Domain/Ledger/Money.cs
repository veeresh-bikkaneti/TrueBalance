namespace Domain.Ledger;

public static class Money
{
    public const int Scale = 2;

    /// <summary>
    /// Returns <paramref name="value"/> unchanged if it has at most 2 decimal places.
    /// Never rounds: a value with more precision is rejected so no cent is silently lost.
    /// </summary>
    /// <exception cref="LedgerRuleException">The value has more than 2 decimal places.</exception>
    public static decimal Validated(decimal value, string paramName)
    {
        if (value != decimal.Round(value, Scale))
        {
            throw new LedgerRuleException(
                $"{paramName} has more than {Scale} decimal places ({value}); amounts are not rounded automatically.");
        }
        return value;
    }
}
