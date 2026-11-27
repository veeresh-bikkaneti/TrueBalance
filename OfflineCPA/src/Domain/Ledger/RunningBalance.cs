using Domain.Enums;

namespace Domain.Ledger;

/// <summary>Previous balance plus amount must equal this balance, in either file order.</summary>
public static class RunningBalance
{
    public static BalanceCheck Check(IReadOnlyList<(decimal Amount, decimal? Balance)> rows)
    {
        if (rows.Count < 2 || rows.Any(r => r.Balance is null)) return BalanceCheck.NotAvailable;
        // step 1: the row above is older. step -1: the row below is older.
        bool Holds(int step) => Enumerable.Range(step > 0 ? 1 : 0, rows.Count - 1)
            .All(i => rows[i - step].Balance + rows[i].Amount == rows[i].Balance);
        return Holds(1) || Holds(-1) ? BalanceCheck.Passed : BalanceCheck.Failed;
    }
}
