using System.Reflection;
using Application.Interfaces;
using Application.Ledger;
using Domain.Entities;
using Domain.Enums;
using Domain.Ledger;
using static TrueBalance.Tests.Ledger.LedgerTestData;

namespace TrueBalance.Tests.Ledger;

public class LedgerTests
{
    // Contract item 1. Invariant: an unbalanced entry never enters the ledger.
    [Fact]
    public void Unbalanced_entry_is_rejected_with_a_clear_error()
    {
        var period = YearPeriod();
        var entry = JournalEntry.CreateDraft(Day, "Fake Vendor 1 purchase", period,
            Lines((Expense(), 100m), (Checking(), -60m)), Now);

        var ex = Assert.Throws<LedgerRuleException>(() => entry.Post(period, Now));

        Assert.Contains("unbalanced", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(JournalEntryStatus.Draft, entry.Status);
    }

    // Contract item 2. Invariant: sum(Amount) == 0 is sufficient to post.
    [Fact]
    public void Balanced_entry_posts()
    {
        var period = YearPeriod();
        var entry = BalancedDraft(period);

        entry.Post(period, Now);

        Assert.Equal(JournalEntryStatus.Posted, entry.Status);
        Assert.Equal(Now, entry.PostedAtUtc);
    }

    // Contract item 3. Invariant: a posted entry has at least two postings.
    [Fact]
    public void Entry_with_one_line_cannot_post()
    {
        var period = YearPeriod();
        var entry = JournalEntry.CreateDraft(Day, "Fake Vendor 1 purchase", period,
            Lines((Expense(), 100m)), Now);

        Assert.Throws<LedgerRuleException>(() => entry.Post(period, Now));
        Assert.Equal(JournalEntryStatus.Draft, entry.Status);
    }

    // Contract item 4. Invariant: Post is only valid from Draft.
    [Fact]
    public void Posted_entry_cannot_be_posted_twice()
    {
        var period = YearPeriod();
        var entry = PostedEntry(period);

        Assert.Throws<LedgerRuleException>(() => entry.Post(period, Now.AddHours(1)));
        Assert.Equal(Now, entry.PostedAtUtc);
    }

    // Contract item 5a. Invariant: state changes only through domain mutators (no public setters).
    [Fact]
    public void Posted_entry_rejects_mutation_no_public_setters()
    {
        var publicSetters = typeof(JournalEntry)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true })
            .Select(p => p.Name)
            .ToList();

        Assert.Empty(publicSetters);
    }

    // Contract item 5b. Invariant: a posted entry's state cannot be re-driven through Post.
    [Fact]
    public void Posted_entry_rejects_mutation_post_on_posted_throws()
    {
        var period = YearPeriod();
        var entry = PostedEntry(period);

        Assert.Throws<LedgerRuleException>(() => entry.Post(period, Now));
        Assert.Equal(JournalEntryStatus.Posted, entry.Status);
    }

    // Contract item 6. Invariant: a reversal nets every account to zero; Reversed entries still count.
    [Fact]
    public async Task Reversal_nets_balances_to_zero()
    {
        var period = YearPeriod();
        var store = StoreWith(period);
        var clock = new FakeClock();
        var original = PostedEntry(period, 250.75m);
        await store.AddEntryAsync(original);

        var reverseId = await new ReverseEntry(store, clock)
            .HandleAsync(new ReverseEntryCommand(original.Id, Day, "Fake reversal", "fake-user"));
        var reversal = await store.FindEntryAsync(reverseId);
        var balances = await new GetAccountBalances(store).HandleAsync();

        Assert.NotNull(reversal);
        Assert.Equal(2, balances.Count);
        Assert.All(balances, b => Assert.Equal(0m, b.Balance));
        Assert.Equal(0m, balances.Sum(b => b.Balance));
        Assert.Equal(JournalEntryStatus.Reversed, original.Status);
        Assert.Equal(JournalEntryStatus.Posted, reversal!.Status);
        Assert.Equal(reversal.Id, original.ReversedByEntryId);
        Assert.Equal(original.Id, reversal.ReversesEntryId);
    }

    // Contract item 7. Invariant: an entry can be reversed at most once.
    [Fact]
    public void Posted_entry_cannot_be_reversed_twice()
    {
        var period = YearPeriod();
        var entry = PostedEntry(period);
        entry.Reverse(period, Day, "Fake reversal", Now);

        Assert.Throws<LedgerRuleException>(() => entry.Reverse(period, Day, "Fake reversal again", Now));
    }

    // Contract item 8a. Invariant: no draft can be created in a locked period.
    [Fact]
    public void Posting_into_a_locked_period_fails_on_create_draft()
    {
        var period = YearPeriod();
        period.Lock();

        Assert.Throws<LedgerRuleException>(() => BalancedDraft(period));
    }

    // Contract item 8b. Invariant: a draft cannot be posted once its period is locked.
    [Fact]
    public void Posting_into_a_locked_period_fails_on_post()
    {
        var period = YearPeriod();
        var entry = BalancedDraft(period);
        period.Lock();

        Assert.Throws<LedgerRuleException>(() => entry.Post(period, Now));
        Assert.Equal(JournalEntryStatus.Draft, entry.Status);
    }

    // Contract item 9. Invariant: a reversal cannot land in a locked period, and the original is untouched.
    [Fact]
    public void Reversal_into_a_locked_period_fails()
    {
        var open = YearPeriod();
        var locked = NextYearPeriod();
        locked.Lock();
        var entry = PostedEntry(open);

        Assert.Throws<LedgerRuleException>(() =>
            entry.Reverse(locked, new DateOnly(2027, 1, 10), "Fake reversal", Now));

        Assert.Equal(JournalEntryStatus.Posted, entry.Status);
        Assert.Null(entry.ReversedByEntryId);
    }

    // Contract item 10. Invariant: money has at most 2 decimal places; rejected, never rounded.
    [Theory]
    [InlineData("1.234")]
    [InlineData("0.001")]
    [InlineData("-5.555")]
    public void Amount_with_more_than_two_decimals_is_rejected(string amount)
    {
        Assert.Throws<LedgerRuleException>(() => Posting.Create(Checking().Id, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    // Contract item 11. Invariant: a zero-amount posting carries no information and is rejected.
    [Fact]
    public void Zero_amount_posting_is_rejected()
    {
        Assert.Throws<LedgerRuleException>(() => Posting.Create(Checking().Id, 0m));
    }

    // Contract item 12. Invariant: across the whole ledger, sum of all balances is exactly zero.
    [Fact]
    public async Task Sum_of_all_account_balances_is_zero()
    {
        var accounts = FiveAccounts();
        var period = YearPeriod();
        var store = StoreWith(period, accounts);
        var clock = new FakeClock();
        var create = new CreateDraftEntry(store, clock);
        var post = new PostEntry(store, clock);
        var rng = new Random(20261125);
        var expected = accounts.ToDictionary(a => a.Id, _ => 0m);

        for (var i = 0; i < 200; i++)
        {
            var lineCount = rng.Next(2, 5); // 2..4 lines
            List<(Guid account, decimal amount)> lines;
            do
            {
                lines = new List<(Guid, decimal)>();
                for (var l = 0; l < lineCount - 1; l++)
                {
                    var cents = rng.Next(1, 500_000);
                    var sign = rng.Next(2) == 0 ? 1 : -1;
                    lines.Add((accounts[rng.Next(accounts.Count)].Id, sign * cents / 100m));
                }
                var last = -lines.Sum(x => x.amount);
                lines.Add((accounts[rng.Next(accounts.Count)].Id, last));
            } while (lines[^1].amount == 0m);

            var id = await create.HandleAsync(new CreateDraftEntryCommand(
                Day, $"Fake Vendor {i % 7 + 1} entry {i}", period.Id,
                lines.Select(x => new DraftPostingLine(x.account, x.amount, null)).ToList(),
                "fake-user"));
            await post.HandleAsync(new PostEntryCommand(id, "fake-user"));

            foreach (var (account, amount) in lines) expected[account] += amount;
        }

        var balances = await new GetAccountBalances(store).HandleAsync();

        Assert.Equal(0m, balances.Sum(b => b.Balance));
        foreach (var b in balances)
            Assert.Equal(expected[b.AccountId], b.Balance);
        // Every account with a nonzero hand-computed total must be reported.
        Assert.Equal(expected.Count(kv => kv.Value != 0m), balances.Count(b => b.Balance != 0m));
    }

    // Contract item 13a. Invariant: every use case writes exactly one audit record, in order, with the local user id.
    [Fact]
    public async Task Audit_log_records_create_post_and_reverse()
    {
        var period = YearPeriod();
        var store = StoreWith(period);
        var clock = new FakeClock();
        const string user = "fake-local-user-1";
        var lines = new List<DraftPostingLine>
        {
            new(Expense().Id, 40m, null),
            new(Checking().Id, -40m, null),
        };

        var id = await new CreateDraftEntry(store, clock)
            .HandleAsync(new CreateDraftEntryCommand(Day, "Fake Vendor 1 purchase", period.Id, lines, user));
        clock.Advance(TimeSpan.FromMinutes(1));
        await new PostEntry(store, clock).HandleAsync(new PostEntryCommand(id, user));
        clock.Advance(TimeSpan.FromMinutes(1));
        var reverseId = await new ReverseEntry(store, clock)
            .HandleAsync(new ReverseEntryCommand(id, Day, "Fake reversal", user));

        var audit = store.AuditRecords;
        Assert.Equal(3, audit.Count);
        Assert.Equal(
            new[] { LedgerAction.DraftCreated, LedgerAction.Posted, LedgerAction.Reversed },
            audit.Select(a => a.Action).ToArray());
        Assert.Equal(
            new[] { LedgerEventType.Drafted, LedgerEventType.Posted, LedgerEventType.Reversed },
            audit.Select(a => a.EventType).ToArray());
        Assert.Equal(
            new[] { "draft created", "entry posted", "entry reversed" },
            audit.Select(a => a.Details).ToArray());
        Assert.All(audit, a => Assert.Equal(user, a.LocalUserId));
        Assert.DoesNotContain(audit, a => a.Details.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase));
        Assert.Equal(id, audit[0].JournalEntryId);
        Assert.Equal(id, audit[1].JournalEntryId);
        Assert.Equal(reverseId, audit[2].JournalEntryId);
        Assert.True(audit[0].OccurredAtUtc <= audit[1].OccurredAtUtc);
        Assert.True(audit[1].OccurredAtUtc <= audit[2].OccurredAtUtc);
    }

    // A failed post must not leave an EntryPosted row. The draft row from create stays.
    [Fact]
    public async Task Failed_post_writes_no_event_row()
    {
        var period = YearPeriod();
        var store = StoreWith(period);
        var clock = new FakeClock();
        var lines = new List<DraftPostingLine>
        {
            new(Expense().Id, 10m, null),
            new(Checking().Id, -4m, null),
        };

        var id = await new CreateDraftEntry(store, clock)
            .HandleAsync(new CreateDraftEntryCommand(Day, "Fake Vendor 1 purchase", period.Id, lines, "fake-user"));

        await Assert.ThrowsAsync<LedgerRuleException>(() =>
            new PostEntry(store, clock).HandleAsync(new PostEntryCommand(id, "fake-user")));

        var audit = store.AuditRecords;
        Assert.Single(audit);
        Assert.Equal(LedgerEventType.Drafted, audit[0].EventType);
        Assert.Equal("draft created", audit[0].Details);
    }

    // Lock writes one PeriodLocked row and nothing else.
    [Fact]
    public async Task Locking_a_period_writes_one_period_locked_event()
    {
        var period = YearPeriod();
        var store = StoreWith(period);
        var clock = new FakeClock();

        await new LockPeriod(store, clock).HandleAsync(new LockPeriodCommand(period.Id, "fake-user"));

        var row = Assert.Single(store.AuditRecords);
        Assert.Equal(LedgerAction.PeriodLocked, row.Action);
        Assert.Equal(LedgerEventType.PeriodLocked, row.EventType);
        Assert.Equal(period.Id, row.PeriodId);
        Assert.Null(row.JournalEntryId);
        Assert.Equal("period locked", row.Details);
    }

    // The in-memory store hides a row until SaveChanges, same as one database transaction.
    [Fact]
    public async Task Audit_row_is_not_visible_until_save()
    {
        var store = new InMemoryLedgerStore();
        await store.AppendAuditAsync(LedgerAuditRecord.For(
            LedgerAction.Posted, LedgerEventType.Posted, "fake-user", Now, details: "entry posted"));

        Assert.Empty(store.AuditRecords);

        await store.SaveChangesAsync();

        var row = Assert.Single(store.AuditRecords);
        Assert.Equal(LedgerEventType.Posted, row.EventType);
    }

    // Contract item 13b. Invariant: audit log is append-only; the store offers no remove or mutate path.
    [Fact]
    public void Audit_log_store_exposes_no_remove_or_mutate()
    {
        string[] forbidden = { "Remove", "Delete", "Clear", "Update", "Edit", "Replace", "Set" };
        var offenders = new[] { typeof(InMemoryLedgerStore), typeof(ILedgerStore) }
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => m.DeclaringType != typeof(object))
            .Where(m => forbidden.Any(f => m.Name.Contains(f, StringComparison.Ordinal)
                                          && m.Name.Contains("Audit", StringComparison.Ordinal)))
            .Select(m => m.Name)
            .ToList();
        var audit = typeof(InMemoryLedgerStore).GetProperty(nameof(InMemoryLedgerStore.AuditRecords))!;

        Assert.Empty(offenders);
        Assert.Null(audit.SetMethod);
        Assert.Equal(typeof(IReadOnlyList<LedgerAuditRecord>), audit.PropertyType);
    }
}
