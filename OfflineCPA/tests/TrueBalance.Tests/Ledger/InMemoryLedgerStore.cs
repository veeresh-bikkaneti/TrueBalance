using Application.Interfaces;
using Domain.Entities;

namespace TrueBalance.Tests.Ledger;

/// <summary>
/// Plain-collection ILedgerStore for tests. Audit rows stay pending until
/// SaveChangesAsync, matching one unit of work. No member removes or mutates a row.
/// </summary>
public sealed class InMemoryLedgerStore : ILedgerStore
{
    private readonly Dictionary<Guid, JournalEntry> _entries = new();
    private readonly List<Period> _periods = new();
    private readonly Dictionary<Guid, Account> _accounts = new();
    private readonly List<LedgerAuditRecord> _audit = new();
    private readonly List<LedgerAuditRecord> _pendingAudit = new();

    public int SaveCount { get; private set; }

    // Committed rows only. Callers cannot touch the internal list.
    public IReadOnlyList<LedgerAuditRecord> AuditRecords => _audit.ToArray();

    // Test seeding helpers (not part of ILedgerStore).
    public void AddAccount(Account account) => _accounts[account.Id] = account;
    public void AddPeriod(Period period) => _periods.Add(period);

    public Task<JournalEntry?> FindEntryAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_entries.TryGetValue(id, out var e) ? e : null);

    public Task AddEntryAsync(JournalEntry entry, CancellationToken ct = default)
    {
        _entries[entry.Id] = entry;
        return Task.CompletedTask;
    }

    public Task<Period?> FindPeriodAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_periods.FirstOrDefault(p => p.Id == id));

    public Task<Period?> FindPeriodForDateAsync(DateOnly date, CancellationToken ct = default)
        => Task.FromResult(_periods.FirstOrDefault(p => p.Contains(date)));

    public Task<IReadOnlyList<AccountBalance>> GetAccountBalancesAsync(CancellationToken ct = default)
    {
        // Mirrors the ledger rule: entries that are Posted OR Reversed count.
        IReadOnlyList<AccountBalance> result = _entries.Values
            .Where(e => e.IsInLedger)
            .SelectMany(e => e.Postings)
            .GroupBy(p => p.AccountId)
            .Select(g =>
            {
                var acct = _accounts[g.Key];
                return new AccountBalance(g.Key, acct.Code, acct.Name, g.Sum(p => p.Amount));
            })
            .OrderBy(b => b.Code, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AppendAuditAsync(LedgerAuditRecord record, CancellationToken ct = default)
    {
        _pendingAudit.Add(record);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        _audit.AddRange(_pendingAudit);
        _pendingAudit.Clear();
        SaveCount++;
        return Task.CompletedTask;
    }
}
