using Domain.Enums;

namespace Domain.Ledger;

/// <summary>
/// Available: previous balance plus amount equals this balance. Owed: previous minus amount.
/// Either file order counts.
/// </summary>
public static class RunningBalance
{
    public static BalanceMeaning Resolve(BalanceMeaning meaning, AccountType? accountType)
        => meaning != BalanceMeaning.FromAccountType ? meaning
            : accountType == AccountType.Liability ? BalanceMeaning.Owed : BalanceMeaning.Available;

    /// <param name="meaning">Already resolved: Available or Owed.</param>
    public static BalanceCheck Check(IReadOnlyList<(decimal Amount, decimal? Balance)> rows, BalanceMeaning meaning)
    {
        if (rows.Count < 2 || rows.Any(r => r.Balance is null)) return BalanceCheck.NotAvailable;
        var direction = meaning switch
        {
            BalanceMeaning.Available => 1,
            BalanceMeaning.Owed => -1,
            _ => throw new ArgumentOutOfRangeException(nameof(meaning))
        };
        // step 1: the row above is older. step -1: the row below is older.
        bool Holds(int step, int sign) => Enumerable.Range(step > 0 ? 1 : 0, rows.Count - 1)
            .All(i => rows[i - step].Balance + direction * sign * rows[i].Amount == rows[i].Balance);
        if (!Holds(1, 1) && !Holds(-1, 1)) return BalanceCheck.Failed;
        // If the opposite signs fit too (two equal fees, say), the balance proves nothing.
        return Holds(1, -1) || Holds(-1, -1) ? BalanceCheck.NotAvailable : BalanceCheck.Passed;
    }
}
