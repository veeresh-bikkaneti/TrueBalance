using Domain.Enums;

namespace Domain.Ledger;

/// <summary>Previous balance plus amount must equal this balance, in either file order.</summary>
public static class RunningBalance
{
    public static BalanceCheck Check(IReadOnlyList<(decimal Amount, decimal? Balance)> rows)
    {
        if (rows.Count < 2 || rows.Any(r => r.Balance is null)) return BalanceCheck.NotAvailable;
        // step 1: the row above is older. step -1: the row below is older.
        bool Holds(int step, int sign) => Enumerable.Range(step > 0 ? 1 : 0, rows.Count - 1)
            .All(i => rows[i - step].Balance + sign * rows[i].Amount == rows[i].Balance);
        if (!Holds(1, 1) && !Holds(-1, 1)) return BalanceCheck.Failed;
        // If the opposite signs fit too (two equal fees, say), the balance proves nothing.
        return Holds(1, -1) || Holds(-1, -1) ? BalanceCheck.NotAvailable : BalanceCheck.Passed;
    }
}
