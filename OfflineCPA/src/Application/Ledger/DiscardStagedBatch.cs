using Application.Interfaces;
using Domain.Enums;
using Domain.Ledger;

namespace Application.Ledger;

/// <summary>
/// Throws away a wrongly staged batch so its stored document can be imported again.
/// Lives with the ledger because it has to ask whether posted entries still stand.
/// </summary>
public sealed class DiscardStagedBatch(IImportStore store, ILedgerStore ledger, IClock clock)
{
    /// <returns>The number of rows discarded.</returns>
    public async Task<int> HandleAsync(Guid batchId, CancellationToken ct = default)
    {
        var batch = await store.FindBatchAsync(batchId, ct)
            ?? throw new LedgerRuleException("The import batch does not exist.");
        var rows = await store.GetRowsAsync(batchId, ct);

        var standing = 0;
        foreach (var row in rows.Where(r => r.Status == StagedRowStatus.Posted))
        {
            var entry = row.JournalEntryId is { } id ? await ledger.FindEntryAsync(id, ct) : null;
            if (entry?.Status != JournalEntryStatus.Reversed) standing++;
        }
        if (standing > 0)
            throw new LedgerRuleException(
                $"Reverse the posted entries from this batch first. Rows still posted: {standing}.");

        batch.Reset(clock.UtcNow);
        foreach (var row in rows) row.Discard();
        await store.SaveChangesAsync(ct);
        return rows.Count;
    }
}
