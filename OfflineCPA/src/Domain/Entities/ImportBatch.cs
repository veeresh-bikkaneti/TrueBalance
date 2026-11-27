using Domain.Enums;
using Domain.Ledger;

namespace Domain.Entities;

/// <summary>
/// One stored statement waiting to be parsed. Queued -> Staged or Failed; discarding takes a Staged
/// batch back to Queued. No file name or path.
/// </summary>
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
    public BalanceCheck BalanceCheck { get; private set; }
    /// <summary>A fixed sentence about a failed balance check. Never a value from the file.</summary>
    public string? BalanceReason { get; private set; }

    public int ResetCount { get; private set; }
    public DateTime? LastResetAtUtc { get; private set; }

    public static ImportBatch Queue(Guid documentId, string extension, DateTime nowUtc)
        => new() { DocumentId = documentId, Extension = extension.ToLowerInvariant(), CreatedAtUtc = nowUtc };

    /// <summary>A Failed batch may be tried again, for example after fixing a column mapping.</summary>
    public void EnsureImportable()
    {
        if (Status == ImportBatchStatus.Staged)
            throw new LedgerRuleException("This batch is already imported.");
    }

    /// <param name="used">The resolved meaning the check ran with.</param>
    /// <param name="otherMeaningPasses">Same arithmetic as the opposite signs fitting.</param>
    public void RecordBalanceCheck(BalanceCheck check, BalanceMeaning used, bool otherMeaningPasses)
    {
        BalanceCheck = check;
        BalanceReason = check != BalanceCheck.Failed ? null
            : "The running balance does not match these amounts. "
              + (!otherMeaningPasses ? string.Empty
                  : "The balance column reads as "
                    + (used == BalanceMeaning.Available ? "an amount owed" : "money available")
                    + ", or the signs are reversed. Check this bank's sign layout and balance meaning. ")
              + "Review each row before posting.";
    }

    public void MarkStaged()
    {
        EnsureImportable();
        Status = ImportBatchStatus.Staged;
        FailureReason = null;
    }

    /// <summary>Back to Queued, so the stored document can be imported again.</summary>
    public void Reset(DateTime nowUtc)
    {
        if (Status != ImportBatchStatus.Staged)
            throw new LedgerRuleException("Only an imported batch can be discarded.");
        Status = ImportBatchStatus.Queued;
        BalanceCheck = BalanceCheck.NotAvailable;
        BalanceReason = null;
        FailureReason = null;
        ResetCount++;
        LastResetAtUtc = nowUtc;
    }

    /// <param name="reason">Must not quote anything read from the file.</param>
    public void MarkFailed(string reason)
    {
        EnsureImportable();
        Status = ImportBatchStatus.Failed;
        FailureReason = reason;
    }
}
