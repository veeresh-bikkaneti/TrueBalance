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

    public void EnsureQueued()
    {
        if (Status != ImportBatchStatus.Queued)
            throw new LedgerRuleException($"Only a Queued batch can be imported; this batch is {Status}.");
    }

    public void MarkStaged()
    {
        EnsureQueued();
        Status = ImportBatchStatus.Staged;
    }

    /// <param name="reason">Must not quote anything read from the file.</param>
    public void MarkFailed(string reason)
    {
        EnsureQueued();
        Status = ImportBatchStatus.Failed;
        FailureReason = reason;
    }
}
