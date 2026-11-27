using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// Port for staging data. An adapter shares its unit of work with
/// <see cref="ILedgerStore"/>, so a posted row and its entry save together.
/// </summary>
public interface IImportStore
{
    Task AddBatchAsync(ImportBatch batch, CancellationToken ct = default);
    Task<ImportBatch?> FindBatchAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ImportBatch>> GetBatchesAsync(CancellationToken ct = default);
    Task AddRowsAsync(IEnumerable<StagedRow> rows, CancellationToken ct = default);
    Task<StagedRow?> FindRowAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StagedRow>> GetRowsAsync(Guid batchId, CancellationToken ct = default, bool includeDiscarded = false);
    Task<IReadOnlyList<StagedRow>> GetAccountRowsAsync(Guid bankAccountId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Guid?> LastPostedCategoryAsync(string matchKey, CancellationToken ct = default);
    Task<bool> AccountExistsAsync(Guid accountId, CancellationToken ct = default);
    /// <returns>Null when the account does not exist.</returns>
    Task<AccountType?> FindAccountTypeAsync(Guid accountId, CancellationToken ct = default);
    Task<CsvMappingProfile?> FindProfileAsync(string bankName, CancellationToken ct = default);
    Task AddProfileAsync(CsvMappingProfile profile, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
