using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class EfLedgerStore : ILedgerStore
{
    private readonly OfflineCPADbContext _db;

    public EfLedgerStore(OfflineCPADbContext db) => _db = db;

    public Task<JournalEntry?> FindEntryAsync(Guid id, CancellationToken ct = default)
        => _db.JournalEntries.Include(e => e.Postings).FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddEntryAsync(JournalEntry entry, CancellationToken ct = default)
        => await _db.JournalEntries.AddAsync(entry, ct);

    public Task<Period?> FindPeriodAsync(Guid id, CancellationToken ct = default)
        => _db.Periods.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Period?> FindPeriodForDateAsync(DateOnly date, CancellationToken ct = default)
        => _db.Periods.FirstOrDefaultAsync(p => p.StartDate <= date && date <= p.EndDate, ct);

    public async Task<IReadOnlyList<AccountBalance>> GetAccountBalancesAsync(CancellationToken ct = default)
    {
        // Posted and Reversed entries both count: a Reversed entry keeps its postings
        // and its reversing entry holds the negation (see docs/ledger.md).
        var rows = await (
                from p in _db.Postings
                join e in _db.JournalEntries on p.JournalEntryId equals e.Id
                join a in _db.Accounts on p.AccountId equals a.Id
                where e.Status == JournalEntryStatus.Posted || e.Status == JournalEntryStatus.Reversed
                group p by new { a.Id, a.Code, a.Name } into g
                orderby g.Key.Code
                select new { g.Key.Id, g.Key.Code, g.Key.Name, Balance = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        return rows.Select(r => new AccountBalance(r.Id, r.Code, r.Name, r.Balance)).ToList();
    }

    public async Task AppendAuditAsync(LedgerAuditRecord record, CancellationToken ct = default)
        => await _db.LedgerAuditRecords.AddAsync(record, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);
}
