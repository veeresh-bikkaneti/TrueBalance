using Domain.Entities;

namespace Application.Interfaces;

public interface ILedgerStore
{
    Task<JournalEntry?> FindEntryAsync(Guid id, CancellationToken ct = default);
    Task AddEntryAsync(JournalEntry entry, CancellationToken ct = default);
    Task<Period?> FindPeriodAsync(Guid id, CancellationToken ct = default);
    Task<Period?> FindPeriodForDateAsync(DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<AccountBalance>> GetAccountBalancesAsync(CancellationToken ct = default);
    Task AppendAuditAsync(LedgerAuditRecord record, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public sealed record AccountBalance(Guid AccountId, string Code, string Name, decimal Balance);
