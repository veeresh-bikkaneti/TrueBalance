using Domain.Enums;
using Domain.Ledger;

namespace Domain.Entities;

/// <summary>One parsed statement line held outside the ledger. Pending or Duplicate -> Posted; any may be Discarded.</summary>
public class StagedRow
{
    public const int MaxDescription = 500;

    private StagedRow()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BatchId { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid BankAccountId { get; private set; }
    public int RowNumber { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string MatchKey { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public decimal? Balance { get; private set; }
    public bool IsExperimental { get; private set; }
    public bool BalanceCheckFailed { get; private set; }
    public StagedRowStatus Status { get; private set; }
    public Guid? CategoryAccountId { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }

    /// <param name="maskedDescription">Text that has already passed the PII masker.</param>
    public static StagedRow Stage(ImportBatch batch, Guid bankAccountId, int rowNumber, DateOnly date,
        string maskedDescription, decimal amount, decimal? balance, bool isExperimental)
    {
        var text = maskedDescription.Trim();
        if (text.Length > MaxDescription) text = text[..MaxDescription];
        return new StagedRow
        {
            BatchId = batch.Id,
            DocumentId = batch.DocumentId,
            BankAccountId = bankAccountId,
            RowNumber = rowNumber,
            Date = date,
            Description = text,
            MatchKey = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant(),
            Amount = Money.Validated(amount, "Amount"),
            Balance = balance,
            IsExperimental = isExperimental
        };
    }

    public void FlagDuplicate() => Status = StagedRowStatus.Duplicate;

    public void FlagBalanceFailed() => BalanceCheckFailed = true;

    /// <summary>The row stays as the record of what was imported.</summary>
    public void Discard() => Status = StagedRowStatus.Discarded;

    public void EnsureCanPost(bool allowDuplicate, bool reviewed)
    {
        if (Status == StagedRowStatus.Discarded)
            throw new LedgerRuleException("This row was discarded.");
        if (Status == StagedRowStatus.Posted)
            throw new LedgerRuleException("This row is already posted.");
        if (Status == StagedRowStatus.Duplicate && !allowDuplicate)
            throw new LedgerRuleException("This row looks like a duplicate; posting it needs an explicit override.");
        if (IsExperimental && !reviewed)
            throw new LedgerRuleException("This row came from an experimental import; confirm you reviewed it.");
        if (BalanceCheckFailed && !reviewed)
            throw new LedgerRuleException("The balance check failed for this import; confirm you reviewed this row.");
    }

    public void MarkPosted(Guid entryId, Guid categoryAccountId, DateTime nowUtc)
    {
        Status = StagedRowStatus.Posted;
        JournalEntryId = entryId;
        CategoryAccountId = categoryAccountId;
        PostedAtUtc = nowUtc;
    }
}
