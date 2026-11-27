using Domain.Enums;
using Domain.Ledger;

namespace Domain.Entities;

/// <summary>One stored statement waiting to be parsed. Queued -> Staged or Failed. No file name or path.</summary>
public class ImportBatch
{
    private ImportBatch()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DocumentId { get; private set; }
    public string Extension { get; private set; } = string.Empty;
    public ImportBatchStatus Status { get; private set; } = ImportBatchStatus.Queued;
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static ImportBatch Queue(Guid documentId, string extension, DateTime nowUtc)
        => new() { DocumentId = documentId, Extension = extension.ToLowerInvariant(), CreatedAtUtc = nowUtc };

    /// <summary>A Failed batch may be tried again, for example after fixing a column mapping.</summary>
    public void EnsureImportable()
    {
        if (Status == ImportBatchStatus.Staged)
            throw new LedgerRuleException("This batch is already imported.");
    }

    public void MarkStaged()
    {
        EnsureImportable();
        Status = ImportBatchStatus.Staged;
        FailureReason = null;
    }

    /// <param name="reason">Must not quote anything read from the file.</param>
    public void MarkFailed(string reason)
    {
        EnsureImportable();
        Status = ImportBatchStatus.Failed;
        FailureReason = reason;
    }
}
