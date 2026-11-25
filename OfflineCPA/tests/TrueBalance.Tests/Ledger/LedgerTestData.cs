using Domain.Entities;

namespace TrueBalance.Tests.Ledger;

/// <summary>Obviously fake accounts, periods and entries. No real names, vendors or account numbers.</summary>
public static class LedgerTestData
{
    public static readonly DateTime Now = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly Day = new(2026, 6, 15);

    public static Account MakeAccount(int n, string code, string name)
        => new() { Id = new Guid(n, 0, 0, new byte[8]), Code = code, Name = name };

    public static Account Checking() => MakeAccount(1, "1000", "Fake Checking");
    public static Account Expense() => MakeAccount(2, "5000", "Fake Expense");
    public static Account Revenue() => MakeAccount(3, "4000", "Fake Revenue");
    public static Account Payable() => MakeAccount(4, "2000", "Fake Payable");
    public static Account Equity() => MakeAccount(5, "3000", "Fake Equity");

    public static IReadOnlyList<Account> FiveAccounts()
        => new[] { Checking(), Revenue(), Payable(), Equity(), Expense() };

    public static Period YearPeriod(string name = "Fake Period 2026")
        => Period.Create(name, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

    public static Period NextYearPeriod()
        => Period.Create("Fake Period 2027", new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31));

    public static List<Posting> Lines(params (Account account, decimal amount)[] lines)
        => lines.Select(l => Posting.Create(l.account.Id, l.amount)).ToList();

    /// <summary>Draft debiting Fake Expense and crediting Fake Checking by <paramref name="amount"/>.</summary>
    public static JournalEntry BalancedDraft(Period period, decimal amount = 100m)
        => JournalEntry.CreateDraft(Day, "Fake Vendor 1 purchase", period,
            Lines((Expense(), amount), (Checking(), -amount)), Now);

    public static JournalEntry PostedEntry(Period period, decimal amount = 100m)
    {
        var entry = BalancedDraft(period, amount);
        entry.Post(period, Now);
        return entry;
    }

    public static InMemoryLedgerStore StoreWith(Period period, IEnumerable<Account>? accounts = null)
    {
        var store = new InMemoryLedgerStore();
        foreach (var a in accounts ?? FiveAccounts()) store.AddAccount(a);
        store.AddPeriod(period);
        return store;
    }
}
