using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public sealed class EfImportStore(OfflineCPADbContext db) : IImportStore
{
    public async Task AddBatchAsync(ImportBatch batch, CancellationToken ct = default)
        => await db.ImportBatches.AddAsync(batch, ct);

    public Task<ImportBatch?> FindBatchAsync(Guid id, CancellationToken ct = default)
        => db.ImportBatches.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<ImportBatch>> GetBatchesAsync(CancellationToken ct = default)
        => await db.ImportBatches.OrderByDescending(b => b.CreatedAtUtc).ToListAsync(ct);

    public Task AddRowsAsync(IEnumerable<StagedRow> rows, CancellationToken ct = default)
        => db.StagedRows.AddRangeAsync(rows, ct);

    public Task<StagedRow?> FindRowAsync(Guid id, CancellationToken ct = default)
        => db.StagedRows.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<StagedRow>> GetRowsAsync(Guid batchId, CancellationToken ct = default,
        bool includeDiscarded = false)
        => await db.StagedRows
            .Where(r => r.BatchId == batchId && (includeDiscarded || r.Status != StagedRowStatus.Discarded))
            .OrderBy(r => r.RowNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StagedRow>> GetAccountRowsAsync(
        Guid bankAccountId, DateOnly from, DateOnly to, CancellationToken ct = default)
        => await db.StagedRows
            .Where(r => r.BankAccountId == bankAccountId && r.Date >= from && r.Date <= to
                && r.Status != StagedRowStatus.Discarded)
            .ToListAsync(ct);

    public Task<Guid?> LastPostedCategoryAsync(string matchKey, CancellationToken ct = default)
        => db.StagedRows
            .Where(r => r.MatchKey == matchKey && r.Status == StagedRowStatus.Posted)
            .OrderByDescending(r => r.PostedAtUtc)
            .Select(r => r.CategoryAccountId)
            .FirstOrDefaultAsync(ct);

    public Task<bool> AccountExistsAsync(Guid accountId, CancellationToken ct = default)
        => db.Accounts.AnyAsync(a => a.Id == accountId, ct);

    public Task<AccountType?> FindAccountTypeAsync(Guid accountId, CancellationToken ct = default)
        => db.Accounts.Where(a => a.Id == accountId).Select(a => (AccountType?)a.Type).FirstOrDefaultAsync(ct);

    public Task<CsvMappingProfile?> FindProfileAsync(string bankName, CancellationToken ct = default)
        => db.CsvMappingProfiles.FirstOrDefaultAsync(p => p.BankName == bankName, ct);

    public async Task AddProfileAsync(CsvMappingProfile profile, CancellationToken ct = default)
        => await db.CsvMappingProfiles.AddAsync(profile, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
